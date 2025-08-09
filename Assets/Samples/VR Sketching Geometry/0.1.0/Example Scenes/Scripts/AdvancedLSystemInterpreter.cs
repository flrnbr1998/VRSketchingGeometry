using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Text;

using System;
using System.Globalization;

using VRSketchingGeometry;
using VRSketchingGeometry.Commands;
using VRSketchingGeometry.Commands.Line;
using VRSketchingGeometry.Meshing;
using VRSketchingGeometry.Serialization;
using VRSketchingGeometry.SketchObjectManagement;
using System.Threading.Tasks;

namespace VRSketchingGeometryPackage.Samples.ExampleScenes.Scripts
{
    /// <summary>
    /// Interpreter für ein parametrisches L-System mit den Operatoren
    /// <c>T(x,y,z)</c> (Translation), <c>K(x,y,z)</c> (Zwischensegment),
    /// <c>J(x,y,z)</c> (letztes Segment), Branching mittels <c>[</c> und <c>]</c>,
    /// sowie Platzhalter <c>X</c> und Aggregator <c>F</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Erwartete Eingaben:</b> Ein expandierter String des L-Systems in obigem Dialekt.
    /// <c>Generate</c> ruft intern <see cref="ExpandLSystem(string, System.Collections.Generic.Dictionary{char, string}, int)"/> auf
    /// und interpretiert die resultierende Zeichenkette räumlich in der Szene.
    /// </para>
    /// <para>
    /// <b>Interpretationskonzept:</b><br/>
    /// - <c>T(v)</c> verschiebt den Stift um <c>v</c> (optional unterdrückt, siehe <see cref="interpretAsTree"/>).<br/>
    /// - <c>K(v)</c> fügt ein Segment (Zwischensegment) in Richtung <c>v</c> an.<br/>
    /// - <c>J(v)</c> fügt das letzte Segment an und aktualisiert die lokale Orientierung anhand von <c>v</c>.<br/>
    /// - <c>[</c>/<c>]</c> sichern/wiederherstellen Position und Rotation (Branching).<br/>
    /// Alle Richtungsvektoren werden mit der aktuellen Orientierung (<see cref="Quaternion"/>) in Weltkoordinaten transformiert.
    /// </para>
    /// <para>
    /// <b>Bekannte Einschränkungen:</b><br/>
    /// - <see cref="inflateSystem(string, System.Collections.Generic.Dictionary{char, string})"/> ignoriert aktuell den
    /// in Kleinbuchstaben-Token geparsten Offset (<c>diffVec</c>) und verwendet nur Basisvektoren aus der Regeldefinition.<br/>
    /// - <see cref="compressSystem(string, System.Collections.Generic.Dictionary{char, string})"/> ist de facto ein No-Op,
    /// außer ein Regelwert entspricht exakt einem gefundenen Cluster; dann wird derselbe String zurückgegeben.<br/>
    /// - Beim Ersetzen von <c>X</c> wird pro Zeichen ein <c>Vector3.zero</c> angehängt, was zu Ausgaben wie <c>"F(0,0,0)"</c> etc. führt.
    /// Das Format hängt von <c>Vector3.ToString()</c> ab (Kultureinstellungen!).<br/>
    /// - Einige Felder (<see cref="lineSegmentPrefab"/>, <see cref="parentObject"/>, <see cref="length"/>, <see cref="lineWidth"/>)
    /// sind aktuell ungenutzt, werden aber beibehalten.
    /// </para>
    /// </remarks>
    public class AdvancedLSystemInterpreter : MonoBehaviour
    {
        
        [SerializeField] private Drawer drawer;
        
        /// <summary>
        /// Wenn <c>true</c>, werden <c>T</c>-Translationen beim Interpretieren auf <c>(0,0,0)</c> gesetzt,
        /// sodass die Struktur wie ein Baum an Ort und Stelle verzweigt (kein Versetzen ganzer Teilstrukturen).
        /// </summary>
        [SerializeField] private Boolean interpretAsTree = true;


        public GameObject lineSegmentPrefab;
        public Transform parentObject;
        public float length = 0.5f;
        public float lineWidth = 0.02f;

        /// <summary>
        /// Cache für interpretierte L-System-Zwischenstände pro Iteration.
        /// Aktuell nicht verwendet, potenziell nützlich für Memoisierung.
        /// </summary>
        private Dictionary<LSystem, Dictionary<int, string>> interpretedLSystems = new Dictionary<LSystem, Dictionary<int, string>>();

        /// <summary>
        /// Hilfsfunktion zum Parsen eines Vektors aus einem Token wie <c>"T(1,2,3)"</c> oder <c>"K(0.1,-0.5,2)"</c>.
        /// </summary>
        /// <param name="token">Der vollständige Token-String inkl. Klammern.</param>
        /// <param name="vec">Ausgabewert: geparster <see cref="Vector3"/>.</param>
        /// <returns><c>true</c>, wenn erfolgreich geparst; sonst <c>false</c>.</returns>
        private bool TryParseVector(string token, out Vector3 vec)
        {
            vec = Vector3.zero;
            int start = token.IndexOf('(');
            int end = token.IndexOf(')');
            if (start < 0 || end < 0) return false;

            string paramString = token.Substring(start + 1, end - start - 1);
            string[] parts = paramString.Split(',');
            if (parts.Length != 3) return false;

            if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z))
            {
                vec = new Vector3(x, y, z);
                return true;
            }
            return false;
        }

        /// <summary>
        /// „Bläst“ ein kompaktes System auf, indem es Kleinbuchstaben-Token (z.B <c>a(x,y,z)</c>)
        /// anhand ihrer Regeldefinition in Sequenzen aus <c>T/K/J</c> umsetzt.
        /// </summary>
        /// <param name="baseString">Eingabestring nach dem vorherigen Ersetzungsschritt.</param>
        /// <param name="rules">Regelwerk (<c>char -> string</c>).</param>
        /// <returns>Aufgeblähter String mit konkreten <c>T/K/J</c>-Sequenzen.</returns>
        private string inflateSystem(string baseString, Dictionary<char, string> rules)
        {
            // Erfasst Cluster der Form: T(...)[K(...)]*J(...); die letzte J-Gruppe wird separat geklammert.
            var clusterRegex = new Regex(@"T\([^)]*\)(?:K\([^)]*\))*J\(([^)]*)\)");

            //Groß-/Kleinbuchstaben mit optionalen Parametern sowie Klammern.
            var tokenPattern = new Regex(@"([A-Za-z])(\(([^)]*)\))?|\[|\]", RegexOptions.Compiled);

            // Kleinbuchstaben mit Vektorparametern
            var lowerPattern = @"([a-z])\(\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*\)";
            // Großbuchstaben mit Vektorparametern.
            var upperPattern = @"([A-Z])\(\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*\)";
            
            var ruleMatcher = new Regex(lowerPattern, RegexOptions.Compiled);
            var operatorMatcher = new Regex(upperPattern, RegexOptions.Compiled);

            // Für jedes Kleinbuchstaben-Token in baseStrin
            foreach (Match m in ruleMatcher.Matches(baseString))
            {

                // Buchstabe aus Gruppe 1
                char ch = m.Groups[1].Value[0];
                // Floats aus den Gruppen 2–4 parsen
                float xdiff = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                float ydiff = float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                float zdiff = float.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);

                

                // Zuordnen ins Dictionary
                Vector3 diffVec = new Vector3(xdiff, ydiff, zdiff);

                string rule = rules[ch];
                Debug.Log(rule);
                string newString = "";

                // Für jeden Regel-Cluster: extrahiere Großbuchstaben-Operatoren samt Basiskoordinaten
                foreach (Match match1 in clusterRegex.Matches(rule))
                {
                    foreach (Match match2 in operatorMatcher.Matches(match1.Value))
                    {

                        // Buchstabe aus Gruppe 1
                        char op = match2.Groups[1].Value[0];

                        float xbase = float.Parse(match2.Groups[2].Value, CultureInfo.InvariantCulture);
                        float ybase = float.Parse(match2.Groups[3].Value, CultureInfo.InvariantCulture);
                        float zbase = float.Parse(match2.Groups[4].Value, CultureInfo.InvariantCulture);

                        // Zuordnen ins Dictionary
                        Vector3 baseVec = new Vector3(xbase, ybase, zbase);
                        float vecAbs = baseVec.magnitude;
                        

                        Vector3 targetVec = baseVec /*+ diffVec*/;
                        //targetVec = targetVec.normalized * vecAbs;

                        newString += op;
                        newString += targetVec.ToString();

                        Debug.Log("DEBUG new String:" +newString);
                    }
                    
                }

                // Ersetze genau das erste Vorkommen dieses Kleinbuchstaben-Tokens durch den neu aufgebauten String.
                var tokenMatcher = new Regex($@"([{ch}])\(\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*\)",
                                            RegexOptions.Compiled);

                int counter = -1;    // Läuft bei jedem Match hoch
                int target = 0; //Index, welches Match ersetzt wird 

                baseString = tokenMatcher.Replace(baseString, m =>
                {
                    // Erhöhe den Zähler _vor_ dem Vergleich
                    counter++;

                    if (counter == target)
                    {
                        //Debug.Log("DEBUG Replacing match #" + counter + ": " + m.Value);
                        return newString;
                    }
                    else
                    {
                        //Debug.Log("DEBUG Skipping match #" + counter + ": " + m.Value);
                        return m.Value;
                    }
                });


            }



            return baseString;

        }

        /// <summary>
        /// Expandiert Kollaps-Token <c>F(x,y,z)</c> zu <c>rules['F']</c>, wobei allen Kleinbuchstaben-Symbolen
        /// die Parameter <c>(x,y,z)</c> angehängt werden; ersetzt außerdem <c>X</c> gemäß <c>rules['X']</c>
        /// und hängt für jedes Zeichen <c>Vector3.zero</c> an.
        /// </summary>
        /// <param name="collapsedString">Eingabestring mit Kollaps-Token.</param>
        /// <param name="rules">Regelwerk (<c>char → string</c>).</param>
        /// <returns>Erweitertes System als String.</returns>
        private string expandCollapsedSystem(string collapsedString, Dictionary<char, string> rules)
        {
            var tokenMatcher = new Regex(
                @"F\(\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*\)",
                RegexOptions.Compiled
            );

            string output = tokenMatcher.Replace(collapsedString, match =>
            {
                string rule = rules['F'];
                var initialPattern = new Regex(@"[a-z]");

                float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                float z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

         

                string editedRule = initialPattern.Replace(rule, match2 => $"{match2.Groups[0].Value}(" + x + "," + y + "," + z + ")");

                
                return editedRule;
            });

            Regex xRegex = new Regex("X");
            string replacedX = xRegex.Replace(output, match => {

                string rule = rules['X'];
                string extendedRule = "";
                foreach(char c in rule)
                {
                    extendedRule += c;
                    extendedRule += Vector3.zero.ToString();
                }

                return extendedRule;
            });


            return replacedX;
        }


        /// <summary>
        /// !Yet to be fixed!
        /// Versucht, aufgeblähte Sequenzen von <c>T/K/J</c> zu „komprimieren“, indem exakte
        /// Übereinstimmungen mit Regelwerten gesucht werden. Aktuell bewirkt dies praktisch
        /// keine strukturelle Kompression (die Übereinstimmung führt zur Rückgabe desselben Strings).
        /// </summary>
        /// <param name="expandedSystem">Aufgeblähter String.</param>
        /// <param name="rules">Regelwerk.</param>
        /// <returns>String nach dem (nahezu no-op) Kompressionsversuch.</returns>
        private string compressSystem(string expandedSystem, Dictionary<char, string> rules)
        {
           

            var clusterRegex = new Regex(
                @"T\([^)]*\)(?:K\([^)]*\))*J\(([^)]*)\)",
                RegexOptions.Compiled
            );

            // Ersetze jeden kompletten Match durch "F(<Inhalt von Gruppe1>)"
            string output = clusterRegex.Replace(expandedSystem, m =>
            {
                foreach (var kvp in rules)
                {
                    if (kvp.Value == m.Value)
                    {
                        return kvp.Value;  // brich ab, sobald du den ersten Treffer hast
                    }
                }
                return m.Value;
            });

            return output;
            

        }

        /// <summary>
        /// Führt die L-System-Expansion über mehrere Iterationen aus:
        /// 1) Kollabierte Tokens expandieren (<see cref="expandCollapsedSystem"/>),
        /// 2) in konkrete <c>T/K/J</c>-Sequenzen „aufblasen“ (<see cref="inflateSystem"/>),
        /// 3) optional komprimieren (<see cref="compressSystem"/>; derzeit wirkungslos),
        /// und gibt schließlich den aufgeblähten String zurück.
        /// </summary>
        /// <param name="axiom">Axiom des L-Systems (z.&nbsp;B. "F").</param>
        /// <param name="rules">Regelwerk (<c>char → string</c>).</param>
        /// <param name="iterations">Anzahl der Iterationen.</param>
        /// <returns>Letzter aufgeblähter String nach <paramref name="iterations"/> Durchläufen.</returns>
        private string ExpandLSystem(string axiom, Dictionary<char, string> rules, int iterations)
        {
            string current = axiom; //F
            Vector3 dirDiff = Vector3.zero;

            //Initiale Ersetzung
            string baseString = "";
            foreach (char symbol in axiom)
            {
                baseString += axiom;
                baseString += "(" + dirDiff.x + ", " + dirDiff.y + ", " + dirDiff.z + ")";
            }

            var initialPattern = new Regex(@"[a-z]");
            
            Debug.Log("Base String before inflation: " + baseString);
            string inflatedSystem = "";

            for (int i = 0; i < iterations; i++)
            {
                baseString = expandCollapsedSystem(baseString, rules);
                Debug.Log($"Collapsed Expanded After {i} Iteration: " + baseString);
                inflatedSystem = inflateSystem(baseString, rules); //TKJs
                string compressedString = compressSystem(inflatedSystem, rules);  //collapsed Strings

                baseString = inflatedSystem;
                Debug.Log($"inflatedSystem After {i} Iteration: " + inflatedSystem);
                Debug.Log($"Compressed System After {i} Iteration: " + compressedString);
                
            }
            return inflatedSystem;

        }

        /// <summary>
        /// Expandiert das übergebene L-System und startet anschließend die Interpretation
        /// in der Szene relativ zu <paramref name="controllerPosition"/> mit Ausrichtung entlang
        /// <paramref name="playerDirection"/>.
        /// </summary>
        /// <param name="lSystem">L-System mit Axiom und Regeln.</param>
        /// <param name="iterations">Anzahl der Expansions-Iterationen.</param>
        /// <param name="controllerPosition">Weltposition, an der die Struktur beginnt.</param>
        /// <param name="controllerRotation">Gibt Orientierung des erzeugten Systems vor</param>
        public void Generate(LSystem lSystem, int iterations, Vector3 controllerPosition, Quaternion controllerRotation)
        {
            string expanded = ExpandLSystem(lSystem.Axiom, lSystem.Rules, iterations);

            StartCoroutine(InterpretLSystemWithControllerRot(expanded, controllerPosition, controllerRotation));
        }

        /// <summary>
        /// Interpretiert ein expandiertes L-System relativ zur Controllerpose und zeichnet die resultierenden Linien.
        /// Die erste Zeichnungsrichtung (erstes <c>K(...)</c> oder <c>J(...)</c> im String) wird dabei so ausgerichtet,
        /// dass sie exakt der <see cref="Vector3.forward"/>-Achse entspricht; anschließend wird das gesamte System
        /// mit der übergebenen <paramref name="controllerRotation"/> in die Welt gedreht.
        /// </summary>
        ///  /// <param name="lSystem">L-System mit Axiom und Regeln.</param>
        /// <param name="iterations">Anzahl der Expansions-Iterationen.</param>
        /// <param name="controllerPosition">Weltposition, an der die Struktur beginnt.</param>
        /// <param name="controllerRotation">Rotation des Controllers, die auf das erzeugte System übertragen wird</param>
        private IEnumerator InterpretLSystemWithControllerRot(string lSystem, Vector3 controllerPosition, Quaternion controllerRotation)
        {
            var transformStack = new Stack<(Vector3 pos, Quaternion rot)>();
            Vector3 position = controllerPosition;

            // 1) Erste Segmentrichtung aus dem String holen (erstes K(...) oder J(...))
            Vector3 firstDirLocal = Vector3.zero;
            {
                int scan = 0;
                while (scan < lSystem.Length - 1)
                {
                    char c = lSystem[scan];
                    if ((c == 'K' || c == 'J') && lSystem[scan + 1] == '(')
                    {
                        int end = lSystem.IndexOf(')', scan);
                        if (end > scan && TryParseVector(lSystem.Substring(scan, end - scan + 1), out var v) && v.sqrMagnitude > Mathf.Epsilon)
                        {
                            firstDirLocal = v;
                        }
                        break;
                    }
                    scan++;
                }
            }

            // 2) Drehung, die "erste Linie" → lokale Forward (Z) bringt
            Quaternion alignToForward = (firstDirLocal.sqrMagnitude > Mathf.Epsilon)
                ? Quaternion.FromToRotation(firstDirLocal.normalized, Vector3.forward)
                : Quaternion.identity;

            // 3) Weltorientierung = exakt Controller-Rotation (inkl. Roll)
            Quaternion currentRot = controllerRotation;

            // Parent nur zur Ordnung
            GameObject lSystemParent = new GameObject("LSystemParent");
            lSystemParent.transform.SetPositionAndRotation(controllerPosition, controllerRotation);

            var drawPoints = new List<Vector3>();
            var lines = new List<List<Vector3>>();

            int i = 0;
            while (i < lSystem.Length)
            {
                char command = lSystem[i];

                // ── Translation: T(x,y,z)
                if (command == 'T' && i + 1 < lSystem.Length && lSystem[i + 1] == '(')
                {
                    int end = lSystem.IndexOf(')', i);
                    if (end > i && TryParseVector(lSystem.Substring(i, end - i + 1), out Vector3 offset))
                    {
                        if (drawPoints.Count > 1) lines.Add(drawPoints);
                        drawPoints = new List<Vector3>();

                        if (interpretAsTree) offset = Vector3.zero;

                        // WICHTIG: erst ins "ausgerichtete" lokale System, dann mit Controller in die Welt
                        Vector3 offAligned = alignToForward * offset;
                        position += currentRot * offAligned;
                        drawPoints.Add(position);
                    }
                    i = end + 1;
                    continue;
                }

                // ── Bewegung: K(x,y,z) / J(x,y,z)
                if ((command == 'J' || command == 'K') && i + 1 < lSystem.Length && lSystem[i + 1] == '(')
                {
                    int end = lSystem.IndexOf(')', i);
                    if (end > i && TryParseVector(lSystem.Substring(i, end - i + 1), out Vector3 dirLocal))
                    {
                        // erst lokal ausrichten, dann in die Welt
                        Vector3 dirAligned = alignToForward * dirLocal;
                        position += currentRot * dirAligned;
                        drawPoints.Add(position);

                        if (command == 'J' && dirAligned.sqrMagnitude > Mathf.Epsilon)
                        {
                            // Vorwärtsachse (Z) an das letzte Segment im lokalen System anpassen
                            Quaternion localRot = Quaternion.FromToRotation(Vector3.forward, dirAligned.normalized);
                            currentRot = currentRot * localRot;
                        }
                    }
                    i = end + 1;
                    continue;
                }

                // ── Branch Push
                if (command == '[')
                {
                    transformStack.Push((position, currentRot));
                    i++;
                    continue;
                }

                // ── Branch Pop
                if (command == ']')
                {
                    if (transformStack.Count > 0)
                    {
                        if (drawPoints.Count > 1) lines.Add(drawPoints);
                        drawPoints = new List<Vector3>();
                        (position, currentRot) = transformStack.Pop();
                    }
                    i++;
                    continue;
                }

                i++;
            }

            if (drawPoints.Count > 1) lines.Add(drawPoints);

            foreach (var pts in lines)
            {
                var lineObj = drawer.drawLineThroughPoints(pts);
                yield return new WaitForSeconds(0.1f);
                lineObj.transform.parent = lSystemParent.transform;
            }
        }



        private struct TransformInfo
        {
            public Vector3 Position;
            public Vector3 Rotation;
            public TransformInfo(Vector3 p, Vector3 r) { Position = p; Rotation = r; }
        }
    }
}


