using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VRSketchingGeometry.SketchObjectManagement;
using VRSketchingGeometryPackage.Samples.ExampleScenes.Scripts;


public class SzeneController : MonoBehaviour
{
   
    private LineSketchObject _currentLine;
    private Vector3 _lastPoint;

    [SerializeField] private AdvancedLSystemInterpreter interpreter;
    [SerializeField] private LSystemGeneratorAdvanced generator;
    [SerializeField] private Drawer drawer;
    [SerializeField] private Dictionary<char, string> rules;
    [SerializeField] private TextMeshPro recodingLabel;

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
    private InputAction recolorLinesAction;
    [SerializeField] private Transform rightHandAnchor;


    [Header("L-System Settings")]
    [SerializeField] private Transform lSystemParent;

    [Header("UIFields")]
    [SerializeField] private TMP_InputField axiomInput;
    [SerializeField] private TMP_InputField keyboardInput;
    [SerializeField] private TMP_Text ruleDísplay;
    [SerializeField] private UIToggle UIController;
    [SerializeField] private GameObject keyboard;
  

    private float timeSinceLastPoint = 0f;

    private List<LineSketchObject> recordedLines = new List<LineSketchObject>();
    private List<LineSketchObject> lSystemRecording = new List<LineSketchObject>();

    private List<Vector3> _lines = new List<Vector3>();

    private List<LSystem> lSystems = new List<LSystem>(); 

    private LSystem _currentLSystem;

    private bool _recording = false;

    void Start()
    {
        recodingLabel.enabled = false;
        
    }
    void Update()
    {
        if (drawAction.triggered && !UIController.isActiveAndEnabled)
        {
            TryStartLine();
            timeSinceLastPoint = 0f;
        }

        if (addPointAction.IsPressed() && !UIController.isActiveAndEnabled)
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
            if (_recording)
            {
                lSystemRecording.Add(_currentLine);
            }
            _currentLine = null;
        }

        if (RecordLSystemAction.triggered)
        {
            if (!_recording)
            {
                Debug.Log("Start Recording");
                lSystemRecording = new List<LineSketchObject>();
                _recording = true;
                recodingLabel.enabled = true;
            }
            else {
                _currentLSystem = generator.GenerateParaRulesFromMultipleLines(lSystemRecording);
                lSystems.Add(_currentLSystem);
                Debug.Log("L-System recording abgschlossen und generiert");
                _recording = false;
                recodingLabel.enabled = false;
                axiomInput.text = _currentLSystem.GetRule('F');
                ruleDísplay.text = _currentLSystem.ToString();
            }
            
            _lines.Clear();
        }

        if (generateRulesAction.triggered)
        {
            if (_currentLSystem != null)
            {
                interpreter.Generate(_currentLSystem, iterations, GetControllerPositionInSpace(), rightHandAnchor.forward);
                
            }
        }

        if (deletePointAction.IsPressed())
        {
            deletePoints();
        }

        if (recolorLinesAction.IsPressed())
        {
            recolorLines();
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
        recolorLinesAction = map.FindAction("RecolorLines");
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
        recolorLinesAction.Enable();
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
        recolorLinesAction.Disable();
    }

    private void TryStartLine()
    {
        Vector3 drawPoint = GetControllerPositionInSpace();
        _lines.Add(drawPoint);

        _currentLine = drawer.startNewLine(drawPoint);
        _lastPoint = drawPoint;
    }

    private void deletePoints()
    {
        Vector3 drawPoint = GetControllerPositionInSpace();
        drawer.deletePoints(drawPoint);
    }

    private void recolorLines()
    {
        Vector3 drawPoint = GetControllerPositionInSpace();
        drawer.colorLines(drawPoint);
    }

    private bool TryAddPointFromMouse()
    {
        if (_currentLine != null)
        {
            Vector3 drawPoint = GetControllerPositionInSpace();
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



    private Vector3 GetControllerPositionInSpace()
    {
        if (useControllerPosition)
        {
            return rightHandAnchor.position;
        }

        Vector2 mousePos = pointerPositionAction.ReadValue<Vector2>();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        return ray.GetPoint(drawDistanceFromCamera);
    }

    public void activateKeyboard()
    {
        keyboardInput.text = axiomInput.text;
        
    }

    public void enteredNewRule()
    {
        string new_rule = keyboardInput.text;
        Debug.Log("New Rule: " + new_rule);
        _currentLSystem.Rules['F'] = new_rule;
        axiomInput.text = _currentLSystem.GetRule('F');
        ruleDísplay.text = _currentLSystem.ToString();
    }

}
