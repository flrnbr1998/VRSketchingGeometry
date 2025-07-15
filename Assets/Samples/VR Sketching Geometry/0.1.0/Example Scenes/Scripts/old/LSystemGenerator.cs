using System.Collections.Generic;
using UnityEngine;

public class LSystemGenerator : MonoBehaviour
{
    public float length = 0.5f;
    public float defaultAngle = 25f;
    public float lineWidth = 0.02f;

    public GameObject lineSegmentPrefab;
    public Transform parentObject;

    public void Generate(string axiom, Dictionary<char, string> rules, int iterations)
    {
        string generated = ExpandLSystem(axiom, rules, iterations);
        InterpretLSystem(generated);
    }

    private string ExpandLSystem(string axiom, Dictionary<char, string> rules, int iterations)
    {
        string current = axiom;
        for (int i = 0; i < iterations; i++)
        {
            string next = "";
            foreach (char c in current)
            {
                next += rules.ContainsKey(c) ? rules[c] : c.ToString();
            }
            current = next;
        }
        return current;
    }

    private void InterpretLSystem(string lSystem)
    {
        Stack<TransformInfo> transformStack = new Stack<TransformInfo>();
        Vector3 position = transform.position;
        Quaternion rotation = Quaternion.LookRotation(Vector3.up);

        int i = 0;
        while (i < lSystem.Length)
        {
            char command = lSystem[i];

            // Neue Rotationserkennung: R(x,y,z,angle)
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

/*using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LSystemGenerator : MonoBehaviour
{
    public float length = 0.5f;           // Schrittweite pro "F"
    public float angle = 25f;             // Drehwinkel bei "+" und "-"
    public float lineWidth = 0.02f;

    private LineRenderer lineRenderer;
    private List<Vector3> points;

    public void Generate(string axiom, Dictionary<char, string> rules, int iterations)
    {
        // Schritt 1: L-System-String generieren
        string generated = ExpandLSystem(axiom, rules, iterations);

        // Schritt 2: L-System-String interpretieren (als Turtle-Graphics)
        InterpretLSystem(generated);
    }

    private string ExpandLSystem(string axiom, Dictionary<char, string> rules, int iterations)
    {
        string current = axiom;
        for (int i = 0; i < iterations; i++)
        {
            string next = "";
            foreach (char c in current)
            {
                if (rules.ContainsKey(c))
                    next += rules[c];
                else
                    next += c.ToString();
            }
            current = next;
        }
        return current;
    }

    private void InterpretLSystem(string lSystem)
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.widthMultiplier = lineWidth;

        points = new List<Vector3>();
        Stack<TransformInfo> transformStack = new Stack<TransformInfo>();

        Vector3 position = transform.position;
        Quaternion rotation = Quaternion.LookRotation(Vector3.up);

        points.Add(position);

        foreach (char command in lSystem)
        {
            switch (command)
            {
                case 'F':
                    Vector3 nextPosition = position + (rotation * Vector3.forward * length);
                    points.Add(nextPosition);
                    position = nextPosition;
                    break;
                case '+':
                    rotation *= Quaternion.Euler(angle, 0, 0);
                    break;
                case '-':
                    rotation *= Quaternion.Euler(-angle, 0, 0);
                    break;
                case '*':
                    rotation *= Quaternion.Euler(0, angle, 0);
                    break;
                case '#':
                    rotation *= Quaternion.Euler(0, -angle, 0);
                    break;
                case '!':
                    rotation *= Quaternion.Euler(0, 0, angle);
                    break;
                case '?':
                    rotation *= Quaternion.Euler(0, 0, -angle);
                    break;
                case '[':
                    transformStack.Push(new TransformInfo(position, rotation));
                    break;
                case ']':
                    if (transformStack.Count > 0)
                    {
                        TransformInfo t = transformStack.Pop();
                        position = t.Position;
                        rotation = t.Rotation;
                        points.Add(position); // Linie „springt“ an alte Position
                    }
                    break;
            }
        }

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    private struct TransformInfo
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public TransformInfo(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}*/