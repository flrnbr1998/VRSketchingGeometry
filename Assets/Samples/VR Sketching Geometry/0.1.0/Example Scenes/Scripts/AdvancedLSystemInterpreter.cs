using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Text;


using VRSketchingGeometry;
using VRSketchingGeometry.Commands;
using VRSketchingGeometry.Commands.Line;
using VRSketchingGeometry.Meshing;
using VRSketchingGeometry.Serialization;
using VRSketchingGeometry.SketchObjectManagement;

namespace VRSketchingGeometryPackage.Samples.ExampleScenes.Scripts
{

    public class AdvancedLSystemInterpreter : MonoBehaviour
    {

        //BrushSetup
        private LineBrush _brush;
        private LineSketchObject _currentLine;
        private Vector3 _lastPoint;

        [SerializeField] private BrushExample drawer;

        public DefaultReferences defaults;
        public Material customMaterial;

        private SketchWorld _sketchWorld;
        private static readonly CommandInvoker Invoker = new CommandInvoker();




        public float length = 0.5f;
        public float defaultAngle = 25f;
        public float lineWidth = 0.02f;

        public GameObject lineSegmentPrefab;
        public Transform parentObject;


        //Hilfsfunktion
        private bool TryParseVector(string token, out Vector3 vec)
        {
            vec = Vector3.zero;
            int start = token.IndexOf('(');
            int end = token.IndexOf(')');
            if (start < 0 || end < 0) return false;

            string paramString = token.Substring(start + 1, end - start - 1);
            string[] parts = paramString.Split(',');

            if (parts.Length != 3) return false;

            try
            {
                vec = new Vector3(
                    float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        //Hilfsfunktion
        private string TransformReplacement(string replacement, Quaternion rotation)
        {
            // Sucht alle Kommandos mit Vektor-Parametern, z.B. F(0,1,0)
            var vectorCommandPattern = new Regex(@"([A-Za-z])\(([^)]+)\)");

            return vectorCommandPattern.Replace(replacement, match =>
            {
                string cmd = match.Groups[1].Value;
                string[] parts = match.Groups[2].Value.Split(',');
                if (parts.Length != 3) return match.Value;

                try
                {
                    Vector3 original = new Vector3(
                        float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)
                    );

                    Vector3 rotated = rotation * original;

                    return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0}({1:0.###},{2:0.###},{3:0.###})",
                        cmd, rotated.x, rotated.y, rotated.z);
                }
                catch
                {
                    return match.Value; // Bei Fehler: unverändert lassen
                }
            });
        }


        private void drawLine(List<Vector3> points)
        {
            drawer.drawLineThroughPoints(points);
        }


        public void Generate(string axiom, Dictionary<char, string> rules, int iterations)
        {
            //string generated = ExpandLSystem(axiom, rules, iterations);


            string generatedPara = ExpandLSystemPara(axiom, rules, iterations);

            //InterpretLSystem(generated);


            InterpretLSystem(generatedPara);
        }


        private string ExpandLSystemPara(string axiom, Dictionary<char, string> rules, int iterations)
        {
            string current = axiom;
            var tokenPattern = new Regex(@"([A-Za-z])(\(([^)]*)\))?|(\[|\])", RegexOptions.Compiled);

            for (int iter = 0; iter < iterations; iter++)
            {
                var nextBuilder = new System.Text.StringBuilder();
                var matches = tokenPattern.Matches(current);

                foreach (Match m in matches)
                {
                    string token = m.Value;
                    char symbol = token[0];

                    if (rules.ContainsKey(symbol))
                    {
                        string replacement = rules[symbol];

                        // Versuche, Richtung aus ursprünglichem Symbol zu holen
                        Vector3 direction;
                        if (!TryParseVector(token, out direction))
                        {
                            // Standard-Richtung, wenn keine Parameter vorhanden (z. B. bei erstem F)
                            direction = Vector3.forward;
                        }

                        Quaternion rotation = Quaternion.LookRotation(direction.normalized);
                        string transformed = TransformReplacement(replacement, rotation);
                        nextBuilder.Append(transformed);
                    }
                    else
                    {
                        nextBuilder.Append(token);
                    }
                }

                current = nextBuilder.ToString();
            }

            Debug.Log("Expanded L-System PARA: " + current);
            return current;
        }


        // Jetzt mit Token-basierter Expansion: ganze F(...)-Token werden erkannt und ersetzt
        private string ExpandLSystem(string axiom, Dictionary<char, string> rules, int iterations)
        {
            string current = axiom;
            // Regex: Erfasst einzelne Befehle mit optionalen Parametern, z.B. F(1.5), R(0,1,0,25), [, ]
            var tokenPattern = new Regex(@"([A-Za-z])(\([^)]*\))?|
                                   (\[|\])", RegexOptions.Compiled);

            for (int iter = 0; iter < iterations; iter++)
            {
                var nextBuilder = new System.Text.StringBuilder();
                var matches = tokenPattern.Matches(current);
                foreach (Match m in matches)
                {
                    // Token ohne Klammern (z.B. "[") oder Befehl mit Parameter (z.B. "F(1.5)")
                    string token = m.Value;
                    // Symbol ist der erste Buchstabe bei parametrierten Befehlen oder der Token selbst bei [ ]
                    char symbol = token[0];

                    if (rules.ContainsKey(symbol))
                    {
                        // Ersetze den gesamten Token (z.B. "F(1.5)") durch die Regel für 'F'
                        nextBuilder.Append(rules[symbol]);
                    }
                    else
                    {
                        // Alle anderen Tokens (Klammerbefehle wie "[", "]", oder unbekannte Commands) bleiben erhalten
                        nextBuilder.Append(token);
                    }
                }
                current = nextBuilder.ToString();
            }
            return current;
        }



        private void InterpretLSystem(string lSystem)
        {
            Stack<TransformInfo> transformStack = new Stack<TransformInfo>();
            Vector3 position = new Vector3(0.0f, 0.0f, 0.0f);
            Quaternion rotation = Quaternion.LookRotation(Vector3.up);

            //Setup for Line drawing
            List<Vector3> drawPoints = new List<Vector3>();
            List<List<Vector3>> lines = new List<List<Vector3>>();

            int i = 0;
            while (i < lSystem.Length)
            {
                char command = lSystem[i];

                // R(x,y,z,angle): Rotation um beliebige Achse
                if (command == 'R' && i + 1 < lSystem.Length && lSystem[i + 1] == '(')
                {
                    int end = lSystem.IndexOf(')', i + 2);
                    if (end > i)
                    {
                        string content = lSystem.Substring(i + 2, end - (i + 2));
                        string[] parts = content.Split(',');
                        if (parts.Length == 4 &&
                            float.TryParse(parts[0], out float x) &&
                            float.TryParse(parts[1], out float y) &&
                            float.TryParse(parts[2], out float z) &&
                            float.TryParse(parts[3], out float angle))
                        {
                            Vector3 axis = new Vector3(x, y, z).normalized;
                            rotation *= Quaternion.AngleAxis(angle, axis);
                            i = end + 1;
                            continue;
                        }
                    }
                }

                // F(l): Vorwärts mit parametrisierter Länge
                if ((command == 'F' || command == 'K') && i + 1 < lSystem.Length && lSystem[i + 1] == '(')
                {
                    int end = lSystem.IndexOf(')', i + 2);
                    if (end > i)
                    {
                        string content = lSystem.Substring(i + 2, end - (i + 2));
                        string[] parts = content.Split(',');

                        // --- 1) Vektor‐Parameter: F(x,y,z) ---
                        if (parts.Length == 3
                            && float.TryParse(parts[0], out float vx)
                            && float.TryParse(parts[1], out float vy)
                            && float.TryParse(parts[2], out float vz))
                        {
                            Vector3 localVec = new Vector3(vx, vy, vz);
                            Vector3 worldOffset = localVec;

                            // Liniensegment zeichnen


                            // Turtle vorziehen und drehen
                            drawPoints.Add(position);
                            position += worldOffset;
                            //rotation = Quaternion.LookRotation(worldOffset.normalized, Vector3.up);

                            // zum nächsten Token springen
                            i = end + 1;
                            continue;
                        }

                        else if (float.TryParse(content, out float paramLen))
                        {
                            Vector3 nextPosition = position + (rotation * Vector3.forward * paramLen);
                            Vector3 dir = nextPosition - position;
                            float len = dir.magnitude;
                            Vector3 center = (position + nextPosition) / 2f;

                            GameObject segment = Instantiate(lineSegmentPrefab, center, Quaternion.LookRotation(dir));
                            segment.transform.localScale = new Vector3(lineWidth, lineWidth, len);
                            if (parentObject != null) segment.transform.SetParent(parentObject);

                            drawPoints.Add(position);
                            position = nextPosition;
                            i = end + 1;
                            continue;
                        }
                    }
                }

                // T(dx,dy,dz): Translation
                if (command == 'T' && i + 1 < lSystem.Length && lSystem[i + 1] == '(')
                {
                    lines.Add(drawPoints);
                    drawPoints = new List<Vector3>();

                    int end = lSystem.IndexOf(')', i + 2);
                    if (end > i)
                    {
                        string content = lSystem.Substring(i + 2, end - (i + 2));
                        string[] parts = content.Split(',');
                        if (parts.Length == 3 &&
                            float.TryParse(parts[0], out float dx) &&
                            float.TryParse(parts[1], out float dy) &&
                            float.TryParse(parts[2], out float dz))
                        {
                            Vector3 offset = new Vector3(dx, dy, dz);
                            position += offset; //Rotation entfernt
                            i = end + 1;
                            continue;
                        }
                    }

                }

                switch (command)
                {
                    case 'F':
                        Vector3 nextPosition = position + (rotation * Vector3.forward * length);
                        Vector3 dir = nextPosition - position;
                        float len = dir.magnitude;
                        Vector3 center = (position + nextPosition) / 2f;

                        GameObject segment = Instantiate(lineSegmentPrefab, center, Quaternion.LookRotation(dir));
                        segment.transform.localScale = new Vector3(lineWidth, lineWidth, len);
                        if (parentObject != null) segment.transform.SetParent(parentObject);

                        position = nextPosition;
                        break;

                    case '[':
                        transformStack.Push(new TransformInfo(position, rotation));
                        break;

                    case ']':
                        if (transformStack.Count > 0)
                        {
                            var t = transformStack.Pop();
                            position = t.Position;
                            rotation = t.Rotation;
                        }
                        break;
                }

                i++;
            }
            lines.Add(drawPoints);

            foreach (List<Vector3> points in lines)
            {
                drawLine(points);
            }
        }

        private struct TransformInfo
        {
            public Vector3 Position;
            public Quaternion Rotation;

            public TransformInfo(Vector3 pos, Quaternion rot)
            {
                Position = pos;
                Rotation = rot;
            }
        }
    }
}
