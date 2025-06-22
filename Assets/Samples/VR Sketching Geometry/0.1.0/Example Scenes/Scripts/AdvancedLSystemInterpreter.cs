using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Text;

public class AdvancedLSystemInterpreter : MonoBehaviour
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
        Vector3 position = new Vector3(0.0f,0.0f,0.0f);
        Quaternion rotation = Quaternion.LookRotation(Vector3.up);

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
            if ((command == 'F'|| command == 'K') && i + 1 < lSystem.Length && lSystem[i + 1] == '(')
            {
                int end = lSystem.IndexOf(')', i + 2);
                if (end > i)
                {
                    string content = lSystem.Substring(i + 2, end - (i + 2));
                    if (float.TryParse(content, out float paramLen))
                    {
                        Vector3 nextPosition = position + (rotation * Vector3.forward * paramLen);
                        Vector3 dir = nextPosition - position;
                        float len = dir.magnitude;
                        Vector3 center = (position + nextPosition) / 2f;

                        GameObject segment = Instantiate(lineSegmentPrefab, center, Quaternion.LookRotation(dir));
                        segment.transform.localScale = new Vector3(lineWidth, lineWidth, len);
                        if (parentObject != null) segment.transform.SetParent(parentObject);

                        position = nextPosition;
                        i = end + 1;
                        continue;
                    }
                }
            }

            // T(dx,dy,dz): Translation
            if (command == 'T' && i + 1 < lSystem.Length && lSystem[i + 1] == '(')
            {
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

