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
    public class LSystemGeneratorAdvanced : MonoBehaviour
    {
        private LineBrush _brush;
        private LineSketchObject _currentLine;
        private Vector3 _lastPoint;

        [SerializeField] private DefaultReferences defaults;
        [SerializeField] private Material customMaterial;
        [SerializeField] private AdvancedLSystemInterpreter generator;
        [SerializeField] private BrushExample drawer;
        [SerializeField] private Dictionary<char, string> rules;


        [SerializeField] private int iterations = 2;
        [SerializeField] private float pointAddInterval = 0.05f;
        [SerializeField] private float minPointDistance = 0.01f;
        [SerializeField] private float drawDistanceFromCamera = 5f;
        [SerializeField] private Vector3 difference = new Vector3(0.0f, 20.0f, 0.0f);


        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;

        private InputAction drawAction;
        private InputAction addPointAction;
        private InputAction RecordLSystemAction;
        private InputAction generateRulesAction;
        private InputAction pointerPositionAction;
        private InputAction controllerPositionAction;
        private InputAction deletePointAction;
        [SerializeField] private Transform rightHandAnchor;


        [Header("L-System Settings")]
        [SerializeField] private GameObject lineSegmentPrefab;
        [SerializeField] private Transform lSystemParent;

        private float timeSinceLastPoint = 0f;
        
        private static readonly CommandInvoker Invoker = new CommandInvoker();

        private List<LineSketchObject> recordedLines = new List<LineSketchObject>();

        List<Vector3> line = new List<Vector3>();

        void Start()
        {
            var brushReference = Instantiate(defaults.LineSketchObjectPrefab).GetComponent<LineSketchObject>();
            brushReference.gameObject.SetActive(false);
            _brush = CreateLineBrush(16, 0.02f, 12);
        }
        void Update()
        {
            if (drawAction.triggered)
            {
                TryStartLineFromMouse();
                timeSinceLastPoint = 0f;
            }

            if (addPointAction.IsPressed())
            {
                timeSinceLastPoint += Time.deltaTime;
                if (timeSinceLastPoint >= pointAddInterval && TryAddPointFromMouse())
                {
                    timeSinceLastPoint = 0f;
                }
            }

            if (addPointAction.WasReleasedThisFrame())
            {
                recordedLines.Add(_currentLine);
                Debug.Log("Linie zum L-System hinzugefügt.");
                _currentLine = null;
            }

            if (RecordLSystemAction.triggered)
            {
                recordedLines = new List<LineSketchObject>();
                Debug.Log("Linie zum L-System hinzugefügt.");

                //drawer.drawLineThroughPoints(line);
                line.Clear();
            }

            if (generateRulesAction.triggered && recordedLines.Count > 0)
            {
                GenerateParaRulesFromMultipleLines(recordedLines);
            }
            if (deletePointAction.IsPressed())
            {
                deletePoints();
            }
        }
        

        void Awake()
        {
            var map = inputActions.FindActionMap("Drawing");
            drawAction = map.FindAction("StartDrawing");
            addPointAction = map.FindAction("AddPoint");
            RecordLSystemAction = map.FindAction("FinishLine");
            generateRulesAction = map.FindAction("GenerateSystem");
            pointerPositionAction = map.FindAction("PointerPosition");
            controllerPositionAction = map.FindAction("ControllerPosition");
            deletePointAction = map.FindAction("DeletePoints");
        }

        void OnEnable()
        {
            drawAction.Enable();
            addPointAction.Enable();
            RecordLSystemAction.Enable();
            generateRulesAction.Enable();
            pointerPositionAction.Enable();
            controllerPositionAction.Enable();
            deletePointAction.Enable();
        }

        void OnDisable()
        {
            drawAction.Disable();
            addPointAction.Disable();
            RecordLSystemAction.Disable();
            generateRulesAction.Disable();
            pointerPositionAction.Disable();
            controllerPositionAction.Disable();
            deletePointAction.Disable();
        }

        private void TryStartLineFromMouse()
        {
            Vector3 drawPoint = GetMousePointInSpace();
            line.Add(drawPoint);


            _currentLine = drawer.startNewLine(drawPoint);
            _lastPoint = drawPoint;
        }

        private void deletePoints()
        {
            Vector3 drawPoint = GetMousePointInSpace();
            drawer.deletePoints(drawPoint);
        }

        private bool TryAddPointFromMouse()
        {
            if (_currentLine != null)
            {
                Vector3 drawPoint = GetMousePointInSpace();
                if (Vector3.Distance(_lastPoint, drawPoint) >= minPointDistance)
                {
                    line.Add(drawPoint);
                    drawer.addPointToCurrentLine(drawPoint);
                    _lastPoint = drawPoint;
                    return true;
                }
            }
            return false;
        }

        [SerializeField] private bool useControllerPosition = true;



        private Vector3 GetMousePointInSpace()
        {
            if (useControllerPosition)
            {
                //return controllerPositionAction.ReadValue<Vector3>();
                return rightHandAnchor.position;
            }

            Vector2 mousePos = pointerPositionAction.ReadValue<Vector2>();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
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

        public void GenerateParaRulesFromMultipleLines(List<LineSketchObject> lines)
        {
            rules = new Dictionary<char, string>();
            if (lines == null || lines.Count == 0 || lineSegmentPrefab == null) return;

            
            char symbol = 'F';
            char lineSymbol = 'a';
            rules[symbol] = "";
            rules['X'] = "F";
            string axiom = symbol.ToString();
            Vector3 prevEnd = new Vector3(0, 0, 0);
            int cycleCount = 0;
            List<char> prevLineSymbols = new List<char>();

            // Build rule string pro Linie
            foreach (var line in lines)
            {
                prevLineSymbols.Add(lineSymbol);
                var pts = line.GetControlPoints();
                if (pts.Count < 2) continue;

                string rule = "";
                // Ausgangspunkt ist erster Punkt, Ausrichtung spielt keine Rolle mehr
                Vector3 basePoint = pts[0];

                Vector3 delta = prevEnd;
                if (cycleCount > 0)
                {
                    delta = pts[0] - lines[0].GetControlPoints()[0];
                }
                // Direkt Translation und parametrisches F mit Vektor
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

            // Interpreter starten
            generator.Generate(axiom, rules, iterations);
        
        }

    }
}
