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
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
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
    }
}
