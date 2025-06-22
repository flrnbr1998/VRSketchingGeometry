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
    public class MouseLineDrawer : MonoBehaviour
    {
        private LineBrush _brush;
        private LineSketchObject _currentLine;
        private Vector3 _lastPoint;

        [SerializeField] private DefaultReferences defaults;
        [SerializeField] private Material customMaterial;
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

            if (Input.GetMouseButtonUp(1))
            {
                _currentLine = null;
            }

            if (Input.GetKeyDown(KeyCode.L) && _currentLine != null)
            {
                Generate3DRuleFromLineAndExpand(_currentLine);
            }
        }

        private void TryStartLineFromMouse()
        {
            Vector3 drawPoint = GetMousePointInSpace();
            _currentLine = Instantiate(defaults.LineSketchObjectPrefab).GetComponent<LineSketchObject>();
            _currentLine.name = "DrawnLine";

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

        private void Generate3DRuleFromLineAndExpand(LineSketchObject lineObject)
        {
            List<Vector3> controlPoints = lineObject.GetControlPoints();
            if (controlPoints.Count < 3 || lineSegmentPrefab == null) return;

            string axiom = "F";
            string derivedRule = "F";

            // Anfangsrichtung bestimmen (erste Tangente)
            Vector3 prevDir = (controlPoints[1] - controlPoints[0]).normalized;
            Quaternion prevRot = Quaternion.LookRotation(prevDir, Vector3.up);

            for (int i = 1; i < controlPoints.Count - 1; i++)
            {
                Vector3 currentDir = (controlPoints[i + 1] - controlPoints[i]).normalized;
                Quaternion currentRot = Quaternion.LookRotation(currentDir, Vector3.up);

                // Rotationsdelta berechnen zwischen vorherigem und aktuellem Orientierungsframe
                Quaternion delta = Quaternion.Inverse(prevRot) * currentRot;

                delta.ToAngleAxis(out float angle, out Vector3 axis);

                // Kleine Rotationen ignorieren (numerisches Rauschen)
                if (angle > 1f)
                {
                    axis.Normalize();
                    string axisStr = $"({axis.x:0.###},{axis.y:0.###},{axis.z:0.###},{angle:0.#})";
                    derivedRule += $"R{axisStr}";
                }

                derivedRule += "F";
                prevRot = currentRot;
            }

            Dictionary<char, string> rules = new Dictionary<char, string> { { 'F', derivedRule } };

            GameObject generatorObj = new GameObject("GeneratedLSystem" + Random.Range(0, 9999));
            var generator = generatorObj.AddComponent<AdvancedLSystemInterpreter>();
            generator.lineSegmentPrefab = lineSegmentPrefab;
            generator.parentObject = lSystemParent;
            generator.lineWidth = 0.02f;
            generator.length = Vector3.Distance(controlPoints[0], controlPoints[1]);
            generator.defaultAngle = 25f;

            Debug.Log($"Generated rule: {derivedRule}");
            generator.Generate(axiom, rules, iterations);
        }
    }
}

    /*public class MouseLineDrawer : MonoBehaviour
    {
        private LineBrush _brush;
        private LineSketchObject _currentLine;
        private Vector3 _lastPoint;

        [SerializeField]
        private DefaultReferences defaults;

        [SerializeField]
        private Material customMaterial;

        // Abstand in Sekunden zwischen zwei Punkten
        [SerializeField]
        private float pointAddInterval = 0.05f;

        // Minimaler Abstand, um unnötige Punkte zu vermeiden
        [SerializeField]
        private float minPointDistance = 0.01f;

        // Abstand zur Kamera in Weltkoordinaten, wo gezeichnet wird
        [SerializeField]
        private float drawDistanceFromCamera = 5f;

        [SerializeField]
        private Vector3 difference = new Vector3(0.0f, 20.0f, 0.0f);

        private float timeSinceLastPoint = 0f;
        private SketchWorld _sketchWorld;
        private static readonly CommandInvoker Invoker = new CommandInvoker();

        void Start()
        {
            // Referenz-Objekt für Brush initialisieren
            var brushReference = Instantiate(defaults.LineSketchObjectPrefab).GetComponent<LineSketchObject>();
            brushReference.gameObject.SetActive(false);

            // SketchWorld erzeugen
            _sketchWorld = Instantiate(defaults.SketchWorldPrefab).GetComponent<SketchWorld>();

            // Brush konfigurieren
            _brush = CreateLineBrush(16, 0.02f, 12);
        }

        void Update()
        {
            // Rechte Maustaste gedrückt -> neue Linie beginnen
            if (Input.GetMouseButtonDown(1))
            {
                TryStartLineFromMouse();
                timeSinceLastPoint = 0f;
            }

            // Rechte Maustaste gehalten -> alle X Sekunden Punkt hinzufügen
            if (Input.GetMouseButton(1))
            {
                timeSinceLastPoint += Time.deltaTime;
                if (timeSinceLastPoint >= pointAddInterval)
                {
                    if (TryAddPointFromMouse())
                    {
                        timeSinceLastPoint = 0f;
                    }
                }
            }

            // Rechte Maustaste losgelassen -> Linie abschließen
            if (Input.GetMouseButtonUp(1))
            {
                _currentLine = null;
            }

            if (Input.GetKeyDown(KeyCode.L) && _currentLine != null)
            {
                GenerateLSystemFromDrawnLine(_currentLine);
            }
        }

        /// <summary>
        /// Startet eine neue Linie an der Mausposition im Raum (vor der Kamera)
        /// </summary>
        private void TryStartLineFromMouse()
        {
            Vector3 drawPoint = GetMousePointInSpace();
            _currentLine = Instantiate(defaults.LineSketchObjectPrefab).GetComponent<LineSketchObject>();
            _currentLine.name = "DrawnLine";

            Invoker.ExecuteCommand(new SetBrushCommand(_currentLine, _brush));
            Invoker.ExecuteCommand(new AddObjectToSketchWorldRootCommand(_currentLine, _sketchWorld));
            Invoker.ExecuteCommand(new AddControlPointCommand(_currentLine, drawPoint));
            _lastPoint = drawPoint;
        }

        /// <summary>
        /// Fügt mit Mindestabstand einen weiteren Punkt im Raum hinzu
        /// </summary>
        private bool TryAddPointFromMouse()
        {
            if (_currentLine != null)
            {
                Vector3 drawPoint = GetMousePointInSpace();
                if (Vector3.Distance(_lastPoint, drawPoint) >= minPointDistance)
                {
                    Invoker.ExecuteCommand(new AddControlPointCommand(_currentLine, drawPoint));
                    _lastPoint = drawPoint;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gibt einen Punkt im Raum zurück, der sich unter dem Mauszeiger befindet
        /// </summary>
        private Vector3 GetMousePointInSpace()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition + difference);
            return ray.GetPoint(drawDistanceFromCamera); // z.B. 5 Einheiten vor der Kamera
        }

        /// <summary>
        /// Erstellt einen Brush mit definierten Querschnittsparametern
        /// </summary>
        private LineBrush CreateLineBrush(int resolution, float scale, int interpolationSteps)
        {
            LineBrush brush = new LineBrush
            {
                CrossSectionVertices = CircularCrossSection.GenerateVertices(resolution, scale),
                CrossSectionNormals = CircularCrossSection.GenerateVertices(resolution),
                InterpolationSteps = interpolationSteps
            };
            return brush;
        }
    }*/
