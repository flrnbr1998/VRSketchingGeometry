using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VRSketchingGeometry.SketchObjectManagement;
using VRSketchingGeometryPackage.Samples.ExampleScenes.Scripts;

public class SzeneController : MonoBehaviour
{
   
    private LineSketchObject _currentLine;
    private Vector3 _lastPoint;

    [SerializeField] private AdvancedLSystemInterpreter interpreter;
    [SerializeField] private LSystemGeneratorAdvanced generator;
    [SerializeField] private BrushExample drawer;
    [SerializeField] private Dictionary<char, string> rules;

    [Header("Debugging Settings for Mouse Input")]
    [SerializeField] private int iterations = 2;
    [SerializeField] private float pointAddInterval = 0.05f;
    [SerializeField] private float minPointDistance = 0.01f;
    [SerializeField] private float drawDistanceFromCamera = 5f;

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

    private List<LineSketchObject> recordedLines = new List<LineSketchObject>();

    private List<Vector3> _lines = new List<Vector3>();

    void Start()
    {

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
            _lines.Clear();
        }

        if (generateRulesAction.triggered && recordedLines.Count > 0)
        {
            generator.GenerateParaRulesFromMultipleLines(recordedLines);
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
        _lines.Add(drawPoint);

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
                _lines.Add(drawPoint);
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
            return rightHandAnchor.position;
        }

        Vector2 mousePos = pointerPositionAction.ReadValue<Vector2>();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        return ray.GetPoint(drawDistanceFromCamera);
    }

}
