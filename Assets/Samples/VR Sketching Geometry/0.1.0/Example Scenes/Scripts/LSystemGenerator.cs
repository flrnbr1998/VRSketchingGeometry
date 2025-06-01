using System.Collections.Generic;
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
        Quaternion rotation = Quaternion.identity;

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
}