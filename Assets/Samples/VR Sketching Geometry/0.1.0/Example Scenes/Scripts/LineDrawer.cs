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
    public class LineDrawer : MonoBehaviour
    {

        private LineBrush _brush;
        private LineSketchObject _currentLine;
        private Vector3 _lastPoint;

        [SerializeField] private DefaultReferences defaults;
        [SerializeField] private Material customMaterial;

        private SketchWorld _sketchWorld;
        private static readonly CommandInvoker Invoker = new CommandInvoker();

        // Start is called before the first frame update
        void Start()
        {
            var brushReference = Instantiate(defaults.LineSketchObjectPrefab).GetComponent<LineSketchObject>();
            brushReference.gameObject.SetActive(false);
            _sketchWorld = Instantiate(defaults.SketchWorldPrefab).GetComponent<SketchWorld>();
            _brush = CreateLineBrush(16, 0.02f, 12);
        }

        // Update is called once per frame
        void Update()
        {

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


        public void drawLine(List<Vector3> points)
        {
            Debug.Log(customMaterial.name);


            _currentLine = Instantiate(defaults.LineSketchObjectPrefab).GetComponent<LineSketchObject>();
            _currentLine.name = "DrawnLine";

            Vector3 drawPoint = new Vector3();
            for (int i = 0; i < points.Count; i++)
            {
                drawPoint = points[i];
                if (i == 0)
                {
                    Debug.Log(drawPoint.x + " " + drawPoint.y + " " + drawPoint.z);
                    Invoker.ExecuteCommand(new SetBrushCommand(_currentLine, _brush));
                    Invoker.ExecuteCommand(new AddObjectToSketchWorldRootCommand(_currentLine, _sketchWorld));
                    Invoker.ExecuteCommand(new AddControlPointCommand(_currentLine, drawPoint));
                }
                else
                {
                    Invoker.ExecuteCommand(new AddControlPointCommand(_currentLine, drawPoint));
                    _lastPoint = drawPoint;
                }
                _lastPoint = drawPoint;
            }
        }

    }
}
