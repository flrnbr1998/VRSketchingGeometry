using System.Collections.Generic;
using UnityEngine;
using VRSketchingGeometry;
using VRSketchingGeometry.Commands;
using VRSketchingGeometry.Commands.Line;
using VRSketchingGeometry.Meshing;
using VRSketchingGeometry.Serialization;
using VRSketchingGeometry.SketchObjectManagement;

namespace VRSketchingGeometryPackage.Samples.ExampleScenes.Scripts
{
    public class LSystemGeneratorAdvanced : MonoBehaviour
    {
        private LineBrush _brush;
        private LineSketchObject _currentLine;
        private Vector3 _lastPoint;

        


        [SerializeField] private DefaultReferences defaults;
        [SerializeField] private Material customMaterial;
        [SerializeField] private AdvancedLSystemInterpreter generator;
        [SerializeField] private BrushExample drawer;

        [SerializeField] private int iterations = 2;
        [SerializeField] private float pointAddInterval = 0.05f;
        [SerializeField] private float minPointDistance = 0.01f;
        [SerializeField] private float drawDistanceFromCamera = 5f;
        [SerializeField] private Vector3 difference = new Vector3(0.0f, 20.0f, 0.0f);

        [Header("L-System Settings")]
        [SerializeField] private GameObject lineSegmentPrefab;
        [SerializeField] private Transform lSystemParent;

        private float timeSinceLastPoint = 0f;
        private SketchWorld _sketchWorld;
        private static readonly CommandInvoker Invoker = new CommandInvoker();

        private List<LineSketchObject> recordedLines = new List<LineSketchObject>();

        List<Vector3> line = new List<Vector3>();

        void Start()
        {
            var brushReference = Instantiate(defaults.LineSketchObjectPrefab).GetComponent<LineSketchObject>();
            brushReference.gameObject.SetActive(false);
            _sketchWorld = Instantiate(defaults.SketchWorldPrefab).GetComponent<SketchWorld>();
            _brush = CreateLineBrush(16, 0.02f, 12);
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                TryStartLineFromMouse();
                timeSinceLastPoint = 0f;
            }

            if (Input.GetMouseButton(1))
            {
                timeSinceLastPoint += Time.deltaTime;
                if (timeSinceLastPoint >= pointAddInterval && TryAddPointFromMouse())
                {
                    timeSinceLastPoint = 0f;
                }
            }

            if (Input.GetMouseButtonUp(1) && _currentLine != null)
            {
                recordedLines.Add(_currentLine);
                Debug.Log("Linie zum L-System hinzugefügt.");
                _currentLine = null;
            }

            if (Input.GetMouseButtonUp(1))
            {
                _currentLine = null;
                drawer.drawLineThroughPoints(line);
                line.Clear();
            }

            if (Input.GetKeyDown(KeyCode.K) && _currentLine != null)
            {
                recordedLines.Add(_currentLine);
                Debug.Log("Linie zum L-System hinzugefügt.");
                _currentLine = null;
            }

            if (Input.GetKeyDown(KeyCode.L) && recordedLines.Count > 0)
            {
                //GenerateRulesFromMultipleLines(recordedLines);
                GenerateParaRulesFromMultipleLines(recordedLines);
                //recordedLines.Clear();
            }
        }

        private void TryStartLineFromMouse()
        {
            Vector3 drawPoint = GetMousePointInSpace();
            _currentLine = Instantiate(defaults.LineSketchObjectPrefab).GetComponent<LineSketchObject>();
            _currentLine.name = "DrawnLine";

            line.Add(drawPoint);

            Invoker.ExecuteCommand(new SetBrushCommand(_currentLine, _brush));
            Invoker.ExecuteCommand(new AddObjectToSketchWorldRootCommand(_currentLine, _sketchWorld));
            Invoker.ExecuteCommand(new AddControlPointCommand(_currentLine, drawPoint));
            _lastPoint = drawPoint;
        }

        private bool TryAddPointFromMouse()
        {
            if (_currentLine != null)
            {
                Vector3 drawPoint = GetMousePointInSpace();
                if (Vector3.Distance(_lastPoint, drawPoint) >= minPointDistance)
                {
                    line.Add(drawPoint);
                    Invoker.ExecuteCommand(new AddControlPointCommand(_currentLine, drawPoint));
                    _lastPoint = drawPoint;
                    return true;
                }
            }
            return false;
        }

        private Vector3 GetMousePointInSpace()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition + difference);
            return ray.GetPoint(drawDistanceFromCamera);
        }

        private LineBrush CreateLineBrush(int resolution, float scale, int interpolationSteps)
        {
            return new LineBrush
            {
                CrossSectionVertices = CircularCrossSection.GenerateVertices(resolution, scale),
                CrossSectionNormals = CircularCrossSection.GenerateVertices(resolution),
                InterpolationSteps = interpolationSteps
            };
        }

        private void GenerateParaRulesFromMultipleLines(List<LineSketchObject> lines)
        {
            if (lines == null || lines.Count == 0 || lineSegmentPrefab == null) return;

            Dictionary<char, string> rules = new Dictionary<char, string>();
            char symbol = 'F';
            rules[symbol] = "";
            string axiom = symbol.ToString();

            // Pushes für jeden Linien-Stamm
            foreach (var line in lines)
                rules[symbol] += "[";

            bool isFirstLine = true;

            // Build rule string pro Linie
            foreach (var line in lines)
            {
                var pts = line.GetControlPoints();
                if (pts.Count < 2) continue;

                string rule = "";
                // Ausgangspunkt ist erster Punkt, Ausrichtung spielt keine Rolle mehr
                Vector3 basePoint = pts[0];

                if (!isFirstLine)
                {
                    Vector3 delta = pts[0] - lines[0].GetControlPoints()[0]; ;
                    // Direkt Translation und parametrisches F mit Vektor
                    rule += string.Format("T({0:0.###},{1:0.###},{2:0.###})", delta.x, delta.y, delta.z);
                }


                for (int i = 0; i < pts.Count - 1; i++)
                {
                    // World-space Delta
                    Vector3 worldDelta = pts[i + 1] - pts[i];

                    if (i == pts.Count - 2 || i == 0)
                    {
                        rule += string.Format("F({0:0.###},{1:0.###},{2:0.###})", worldDelta.x, worldDelta.y, worldDelta.z);
                    }
                    else
                    {
                        rule += string.Format("K({0:0.###},{1:0.###},{2:0.###})", worldDelta.x, worldDelta.y, worldDelta.z);
                    }
                }

                rule += "]";
                rules[symbol] += rule;
                isFirstLine = false;
            }

            // Debug-Ausgabe
            Debug.Log("AXIOM: " + axiom);
            foreach (var kv in rules)
                Debug.Log(kv.Key + " -> " + kv.Value);

            // Interpreter starten
            generator.Generate(axiom, rules, iterations);
        
        }

        private void GenerateRulesFromMultipleLines(List<LineSketchObject> lines)
       {
            // 1) Vorbedingung: Keine Linien oder kein Prefab → Abbruch
            if (lines == null || lines.Count == 0 || lineSegmentPrefab == null) return;

            // 2) Setup für L-System: Axiom und Regelsatz
            Dictionary<char, string> rules = new Dictionary<char, string>();
            string axiom = "";
            char symbol = 'F';
            rules.Add(symbol, "");  // Ein Symbol F mit leerer Regel
            axiom += symbol;

            // Zustände der vorherigen Linie merken
            Quaternion? prevRotation = Quaternion.LookRotation(Vector3.up);
            Vector3 prevEndPoint = Vector3.zero;

            // 3) PROBLEM: Unbalancierte Pushes
            // Fügt für jede Linie nur ein '[' ein, aber Pop (']') später nur pro Linie im zweiten Loop.
            foreach (LineSketchObject line in lines)
                    {
                        rules[symbol] += '[';
                    }

            // 4) Übersetze jede Linie in eine Regel-Folge
            foreach (LineSketchObject line in lines)
            {
                List<Vector3> points = line.GetControlPoints();
                if (points.Count < 3) continue;  // zu kurze Linie ignorieren

                string rule = "";
                //rule += "[";
                // 4.1) Erste Segmentrichtung bestimmen
                Vector3 prevDir = (points[1] - points[0]).normalized;
                Quaternion currentRotation = Quaternion.LookRotation(prevDir, Vector3.up);

                // 4.2) Falls nicht erste Linie: Pop + Translation + Rotation
                if (prevRotation.HasValue)
                {
                    
                    // PROBLEM: offset wird hier falsch verwendet (Welt vs. Turtle)
                    Vector3 offset = points[0] - lines[0].GetControlPoints()[0] ;                      // ← PROBLEM: kein prevEndPoint-Abzug
                    rule += $"T({offset.x:0.###},{offset.y:0.###},{offset.z:0.###})";  // PROBLEM: ungefiltert im Welt-KS

                    // Rotation von letzte Ausrichtung zu neuer Ausrichtung
                    Quaternion delta = Quaternion.Inverse(prevRotation.Value) * currentRotation;
                    delta.ToAngleAxis(out float angle, out Vector3 axis);
                    if (angle > 1f)
                    {
                        axis.Normalize();
                        rule += $"R({axis.x:0.###},{axis.y:0.###},{axis.z:0.###},{angle:0.#})";
                    }
                }



                // 4.3) Immer ein F für das erste Segment

                // 4.4) Für jedes weitere Segment: Rotation + F
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Vector3 p0 = points[i];
                    Vector3 p1 = points[i + 1];

                    Vector3 dir = (points[i + 1] - points[i]).normalized;
                    Quaternion nextRotation = Quaternion.LookRotation(dir, Vector3.up);

                    Quaternion delta = Quaternion.Inverse(currentRotation) * nextRotation;
                    delta.ToAngleAxis(out float angle, out Vector3 axis);

                    if (angle > 1f)
                    {
                        axis.Normalize();
                        rule += $"R({axis.x:0.###},{axis.y:0.###},{axis.z:0.###},{angle:0.#})";
                    }

                    float segmentLength = Vector3.Distance(p0, p1);

                    if (i == points.Count - 2)
                    {
                        rule += $"F({segmentLength:0.###})";
                      
                    }
                    else
                    {
                        rule += $"K({segmentLength:0.###})";
                    }
                    currentRotation = nextRotation;
                }

                rule += "]";
                // 4.5) Regel anhängen
                rules[symbol] += rule;
                //axiom += symbol;   // bisher nicht genutzt
              
                // 4.6) aktuellen Endpunkt und Rotation merken
                prevRotation = Quaternion.LookRotation(Vector3.up);
                prevEndPoint = points[points.Count - 1];
            }



            // 6) Debug: Axiom und Regeln ausgeben
            Debug.Log("AXIOM: " + axiom);
            foreach (var kvp in rules)
            {
                Debug.Log($"{kvp.Key} → {kvp.Value}");
            }

            // 7) L-System generieren (Iterations-Anzahl beliebig)
            generator.Generate(axiom, rules, iterations);
        }
    }
}
