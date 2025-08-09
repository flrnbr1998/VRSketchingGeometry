using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using VRSketchingGeometry;
using VRSketchingGeometry.Commands;
using VRSketchingGeometry.Commands.Line;
using VRSketchingGeometry.Meshing;
using VRSketchingGeometry.Serialization;
using VRSketchingGeometry.SketchObjectManagement;

using UnityEngine.InputSystem;

namespace VRSketchingGeometryPackage.Samples.ExampleScenes.Scripts
{
    /// <summary>
    /// Erzeugt aus mehreren 3D-Freihandlinien (LineSketchObject) ein parametrisches L-System.
    /// </summary>
    /// <remarks>
    /// <para><b>Dialect / Symbole:</b></para>
    /// <list type="bullet">
    /// <item><description><c>T(x,y,z)</c>: Translation relativ zum Start der ersten Linie.</description></item>
    /// <item><description><c>K(x,y,z)</c>: Zeichnet ein Segment mit Vektor <c>v</c> (Zwischensegment).</description></item>
    /// <item><description><c>J(x,y,z)</c>: Wie <c>K</c>, jedoch für das <b>letzte</b> Segment der Linie.</description></item>
    /// <item><description><c>[</c> / <c>]</c>: Push/Pop von Position/Rotation (Branching).</description></item>
    /// <item><description><c>X</c>: Platzhalter, der zu <c>F</c> expandiert.</description></item>
    /// <item><description><c>F</c>: Aggregator-Regel, die alle bisher definierten Liniensymbole verzweigt:
    /// <c>F → [aX][bX]…</c></description></item>
    /// </list>
    /// <para><b>Axiom:</b> "F"</para>
    /// <para><b>Ergebnis:</b> Für jede Eingabelinie wird ein neues Symbol ab <c>'a'</c> erzeugt, dessen Regel eine
    /// Translation vom globalen Referenzpunkt (Startpunkt der ersten Linie) plus die segmentweisen Deltas (<c>K</c>/<c>J</c>) enthält.</para>
    /// <para><b>Bekannte Einschränkungen:</b> 
    /// - Unity serialisiert <see cref="Dictionary{TKey,TValue}"/> nicht; das Feld erscheint nicht im Inspector.
    /// - Nach <c>'z'</c> laufen die char-Symbole weiter (ASCII), es gibt kein automatisches Umschalten auf Großbuchstaben o.ä.
    /// - Linien mit weniger als 2 Kontrollpunkten werden übersprungen (es wird dann keine Regel erzeugt).</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // lines: vom Drawer/Scene gesammelte LineSketchObject-Referenzen mit >= 2 Punkten
    /// var lsys = GetComponent&lt;LSystemGeneratorAdvanced&gt;()
    ///              .GenerateParaRulesFromMultipleLines(lines);
    /// // lsys kann anschließend von deinem L-System-Interpreter weiterverarbeitet werden.
    /// </code>
    /// </example>
    public class LSystemGeneratorAdvanced : MonoBehaviour
    {


        [SerializeField] private AdvancedLSystemInterpreter generator;
        [SerializeField] private Drawer drawer;
        [SerializeField] private Dictionary<char, string> rules;


        [Header("L-System Settings")]
        [SerializeField] private Transform lSystemParent;


        /// <summary>
        /// Erzeugt ein parametrisches L-System basierend auf einer Liste von Linien.
        /// </summary>
        /// <param name="lines">
        /// Liste von <see cref="LineSketchObject"/>. Jede verwertete Linie benötigt mindestens 2 Kontrollpunkte.
        /// Die erste Linie definiert den globalen Referenzstart (für die nachfolgenden <c>T</c>-Deltas).
        /// </param>
        /// <returns>
        /// Ein <see cref="LSystem"/> mit Axiom <c>"F"</c> und erzeugten Produktionsregeln,
        /// oder <c>null</c>, wenn <paramref name="lines"/> leer oder <c>null</c> ist.
        /// </returns>
        public LSystem GenerateParaRulesFromMultipleLines(List<LineSketchObject> lines)
        {
            rules = new Dictionary<char, string>();
            if (lines == null || lines.Count == 0)  return null;

            
            char symbol = 'F';
            char lineSymbol = 'a';
            rules[symbol] = "";
            rules['X'] = "F";
            string axiom = symbol.ToString();
            Vector3 prevEnd = new Vector3(0, 0, 0);
            
            // Zählt, wie viele Linien bereits verarbeitet wurden (relevant für die Translation).
            int cycleCount = 0;
            List<char> prevLineSymbols = new List<char>();

            // Build rule string pro Linie
            foreach (var line in lines)
            {
                prevLineSymbols.Add(lineSymbol);
                var pts = line.GetControlPoints();

                // Linien mit < 2 Punkten können keine Segmente bilden
                if (pts == null || pts.Count < 2)
                {
                    continue;
                }

                string rule = "";
                // Ausgangspunkt ist erster Punkt, Ausrichtung spielt keine Rolle mehr
                Vector3 basePoint = pts[0];

                Vector3 delta = prevEnd;
                if (cycleCount > 0)
                {
                    delta = pts[0] - lines[0].GetControlPoints()[0];
                }
                // Direkt Translation 
                rule += string.Format(CultureInfo.InvariantCulture, "T({0:0.###},{1:0.###},{2:0.###})", delta.x, delta.y, delta.z);



                for (int i = 0; i < pts.Count - 1; i++)
                {
                    // World-space Delta
                    Vector3 worldDelta = pts[i + 1] - pts[i];

                    if (i == pts.Count - 2)
                    {
                        rule += string.Format(CultureInfo.InvariantCulture, "J({0:0.###},{1:0.###},{2:0.###})", worldDelta.x, worldDelta.y, worldDelta.z);
                    }
                    else
                    {
                        rule += string.Format(CultureInfo.InvariantCulture, "K({0:0.###},{1:0.###},{2:0.###})", worldDelta.x, worldDelta.y, worldDelta.z);
                    }
                }

                rules[lineSymbol] = rule;

                int sc = 0;
                string curRule = "";


                foreach (char s in prevLineSymbols){
                    curRule += '[';
                    curRule += s;
                    curRule += 'X';
                    curRule += ']';

                }

                rules[symbol] = curRule;
                prevEnd = pts[0];
      

                lineSymbol++;
                cycleCount++;
            }

            // Debug-Ausgabe
            Debug.Log("AXIOM: " + axiom);
            foreach (var kv in rules)
                Debug.Log(kv.Key + " -> " + kv.Value);

            LSystem lSystem = new LSystem(axiom,rules);
            
            
            return lSystem;

        }

    }
}
