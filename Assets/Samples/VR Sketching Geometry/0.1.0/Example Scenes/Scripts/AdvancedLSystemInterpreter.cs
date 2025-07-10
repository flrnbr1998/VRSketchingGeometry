using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Text;

using System;
using System.Globalization;

using VRSketchingGeometry;
using VRSketchingGeometry.Commands;
using VRSketchingGeometry.Commands.Line;
using VRSketchingGeometry.Meshing;
using VRSketchingGeometry.Serialization;
using VRSketchingGeometry.SketchObjectManagement;

namespace VRSketchingGeometryPackage.Samples.ExampleScenes.Scripts
{
    public class AdvancedLSystemInterpreter : MonoBehaviour
    {
        [SerializeField] private BrushExample drawer;
        [SerializeField] private Boolean interpretAsTree = true;


        public GameObject lineSegmentPrefab;
        public Transform parentObject;
        public float length = 0.5f;
        public float lineWidth = 0.02f;

        private bool TryParseVector(string token, out Vector3 vec)
        {
            vec = Vector3.zero;
            int start = token.IndexOf('(');
            int end = token.IndexOf(')');
            if (start < 0 || end < 0) return false;

            string paramString = token.Substring(start + 1, end - start - 1);
            string[] parts = paramString.Split(',');
            if (parts.Length != 3) return false;

            if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z))
            {
                vec = new Vector3(x, y, z);
                return true;
            }
            return false;
        }



        private string inflateSystem(string baseString, Dictionary<char, string> rules)
        {


            //Alle weiteren
            var clusterRegex = new Regex(@"T\([^)]*\)(?:K\([^)]*\))*J\(([^)]*)\)");
            var tokenPattern = new Regex(@"([A-Za-z])(\(([^)]*)\))?|\[|\]", RegexOptions.Compiled);


            var lowerPattern = @"([a-z])\(\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*\)";
            var upperPattern = @"([A-Z])\(\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*\)";
            var ruleMatcher = new Regex(lowerPattern, RegexOptions.Compiled);
            var operatorMatcher = new Regex(upperPattern, RegexOptions.Compiled);


            foreach (Match m in ruleMatcher.Matches(baseString))
            {

                // Buchstabe aus Gruppe 1
                char ch = m.Groups[1].Value[0];
                // Floats aus den Gruppen 2–4 parsen
                float xdiff = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                float ydiff = float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                float zdiff = float.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);

                

                // Zuordnen ins Dictionary
                Vector3 diffVec = new Vector3(xdiff, ydiff, zdiff);

                string rule = rules[ch];
                Debug.Log(rule);
                string newString = "";


                foreach (Match match1 in clusterRegex.Matches(rule))
                {
                    foreach (Match match2 in operatorMatcher.Matches(match1.Value))
                    {

                        // Buchstabe aus Gruppe 1
                        char op = match2.Groups[1].Value[0];

                        float xbase = float.Parse(match2.Groups[2].Value, CultureInfo.InvariantCulture);
                        float ybase = float.Parse(match2.Groups[3].Value, CultureInfo.InvariantCulture);
                        float zbase = float.Parse(match2.Groups[4].Value, CultureInfo.InvariantCulture);

                        // Zuordnen ins Dictionary
                        Vector3 baseVec = new Vector3(xbase, ybase, zbase);
                        float vecAbs = baseVec.magnitude;
                        

                        Vector3 targetVec = baseVec /*+ diffVec*/;
                        //targetVec = targetVec.normalized * vecAbs;

                        newString += op;
                        newString += targetVec.ToString();

                        Debug.Log("DEBUG new String:" +newString);
                    }
                    
                }

                //Debug.Log("DEBUG Eigesetzte Regel: " + newString);
                var tokenMatcher = new Regex($@"([{ch}])\(\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*\)",
                                            RegexOptions.Compiled);

                int counter = -1;    // Läuft bei jedem Match hoch
                int target = 0; // Dein Index, welches Match du ersetzen willst (0-basiert o. 1-basiert — je nachdem)

                baseString = tokenMatcher.Replace(baseString, m =>
                {
                    // Erhöhe den Zähler _vor_ dem Vergleich
                    counter++;

                    if (counter == target)
                    {
                        //Debug.Log("DEBUG Replacing match #" + counter + ": " + m.Value);
                        return newString;
                    }
                    else
                    {
                        //Debug.Log("DEBUG Skipping match #" + counter + ": " + m.Value);
                        return m.Value;
                    }
                });


            }



            return baseString;

        }


        private string expandCollapsedSystem(string collapsedString, Dictionary<char, string> rules)
        {
            var tokenMatcher = new Regex(
                @"F\(\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*,\s*([+-]?\d*\.?\d+)\s*\)",
                RegexOptions.Compiled
            );

            string output = tokenMatcher.Replace(collapsedString, match =>
            {
                string rule = rules['F'];
                var initialPattern = new Regex(@"[a-z]");

                float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                float z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

         

                string editedRule = initialPattern.Replace(rule, match2 => $"{match2.Groups[0].Value}(" + x + "," + y + "," + z + ")");

                
                return editedRule;
            });

            Regex xRegex = new Regex("X");
            string replacedX = xRegex.Replace(output, match => {

                string rule = rules['X'];
                string extendedRule = "";
                foreach(char c in rule)
                {
                    extendedRule += c;
                    extendedRule += Vector3.zero.ToString();
                }

                return extendedRule;
            });


            return replacedX;
        }

        private string compressSystem(string expandedSystem, Dictionary<char, string> rules)
        {
           

            var clusterRegex = new Regex(
                @"T\([^)]*\)(?:K\([^)]*\))*F\(([^)]*)\)",
                RegexOptions.Compiled
            );

            // Ersetze jeden kompletten Match durch "F(<Inhalt von Gruppe1>)"
            string output = clusterRegex.Replace(expandedSystem, m =>
            {
                foreach (var kvp in rules)
                {
                    if (kvp.Value == m.Value)
                    {
                        return kvp.Value;  // brich ab, sobald du den ersten Treffer hast
                    }
                }
                return m.Value;
            });

            return output;
            

        }


        private string ExpandLSystemII(string axiom, Dictionary<char, string> rules, int iterations)
        {
            string current = axiom; //F
            Vector3 dirDiff = Vector3.zero;

            //Initiale Ersetzung
            string baseString = "";
            foreach (char symbol in axiom)
            {
                baseString += axiom;
                baseString += "(" + dirDiff.x + ", " + dirDiff.y + ", " + dirDiff.z + ")";
            }

            var initialPattern = new Regex(@"[a-z]");
            
            //baseString = initialPattern.Replace(baseString, match => $"{match.Groups[0].Value}(" + dirDiff.x + "," + dirDiff.y + "," + dirDiff.z + ")");
            Debug.Log("Base String before inflation: " + baseString);
            string inflatedSystem = "";

            for (int i = 0; i < iterations; i++)
            {
                baseString = expandCollapsedSystem(baseString, rules);
                Debug.Log($"Colapsed Expanded After {i} Iteration: " + baseString);
                inflatedSystem = inflateSystem(baseString, rules); //TKFs
                string compressedString = compressSystem(inflatedSystem, rules);  //collapsed Strings

                baseString = inflatedSystem;
                Debug.Log($"inflatedSystem After {i} Iteration: " + inflatedSystem);
                Debug.Log($"Compressed System After {i} Iteration: " + compressedString);
                
            }
            return inflatedSystem;

        }

        public void Generate(string axiom, Dictionary<char, string> rules, int iterations)
        {
            string expanded = ExpandLSystemII(axiom, rules, iterations);
            InterpretLSystem(expanded);
        }

        private void InterpretLSystem(string lSystem)
        {
            var transformStack = new Stack<TransformInfo>();
            Vector3 position = Vector3.zero;
            Vector3 rotation = Vector3.up;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, rotation);
            

            var drawPoints = new List<Vector3>();
            var lines = new List<List<Vector3>>();

            int i = 0;
            while (i < lSystem.Length)
            {
                char command = lSystem[i];

                // Translation T(x,y,z)
                if (command == 'T' && i + 1 < lSystem.Length && lSystem[i + 1] == '(')
                {
                    int end = lSystem.IndexOf(')', i);
                    if (end > i && TryParseVector(lSystem.Substring(i, end - i + 1), out Vector3 offset))
                    {
                        if (drawPoints.Count > 1) lines.Add(drawPoints);
                        drawPoints = new List<Vector3>();
                        //TODO: Vector Rotation
                        if (interpretAsTree) {
                            offset = Vector3.zero;
                        }
                        rot = Quaternion.FromToRotation(Vector3.up, rotation.normalized);

                        offset = rot * offset;

                        position += offset;
                        drawPoints.Add(position);
                    }
                    i = end + 1;
                    continue;
                }

                // Movement F(x,y,z) or K(x,y,z)
                if ((command == 'J' || command == 'K') && i + 1 < lSystem.Length && lSystem[i + 1] == '(')
                {
                    int end = lSystem.IndexOf(')', i);
                    if (end > i && TryParseVector(lSystem.Substring(i, end - i + 1), out Vector3 dir))
                    {
                        //Vector Rotation
                        dir = rot * dir;
                        Vector3 newPos = position + dir;
                        
                        position = newPos;
                        drawPoints.Add(position);
                        if (command == 'J')
                        {
                            rotation = dir;
                        }
                    }
                    i = end + 1;
                    continue;
                }

                // Branch Push
                if (command == '[')
                {
                    transformStack.Push(new TransformInfo(position, rotation));
                    i++;
                    continue;
                }

                // Branch Pop
                if (command == ']')
                {
                    if (transformStack.Count > 0)
                    {
                        if (drawPoints.Count > 1) lines.Add(drawPoints);
                        drawPoints = new List<Vector3>();
                        var ti = transformStack.Pop();
                        position = ti.Position;
                        rotation = ti.Rotation;
                    }
                    i++;
                    continue;
                }

                i++;
            }

            if (drawPoints.Count > 1) lines.Add(drawPoints);
            foreach (var pts in lines)
            {
                Debug.Log(pts);
                drawer.drawLineThroughPoints(pts);
            }
        }

        private struct TransformInfo
        {
            public Vector3 Position;
            public Vector3 Rotation;
            public TransformInfo(Vector3 p, Vector3 r) { Position = p; Rotation = r; }
        }
    }
}


/*
[T(0.00, 0.00, 0.00)K(-0.09, 0.25, 0.19)K(-0.18, 0.44, 0.30)K(-0.47, 0.67, 0.36)K(-0.42, 0.59, 0.24)K(-0.62, 0.57, 0.12)K(-0.47, 0.40, 0.03)K(-0.48, 0.40, -0.02)K(-0.65, 0.62, -0.07)K(-1.10, 1.43, -0.25)K(-0.69, 1.45, -0.33)K(-0.31, 1.14, -0.30)F(-0.07, 0.50, -0.14) 
    [T(0.00, 0.00, 0.00)K(-0.09, 0.25, 0.19)K(-0.18, 0.44, 0.30)K(-0.47, 0.67, 0.36)K(-0.42, 0.59, 0.24)K(-0.62, 0.57, 0.12)K(-0.47, 0.40, 0.03)K(-0.48, 0.40, -0.02)K(-0.65, 0.62, -0.07)K(-1.10, 1.43, -0.25)K(-0.69, 1.45, -0.33)K(-0.31, 1.14, -0.30)F(-0.07, 0.50, -0.14)]
    T(0.00, 0.00, 0.00)K(-0.09, 0.25, 0.19)K(-0.18, 0.44, 0.30)K(-0.47, 0.67, 0.36)K(-0.42, 0.59, 0.24)K(-0.62, 0.57, 0.12)K(-0.47, 0.40, 0.03)K(-0.48, 0.40, -0.02)K(-0.65, 0.62, -0.07)K(-1.10, 1.43, -0.25)K(-0.69, 1.45, -0.33)K(-0.31, 1.14, -0.30)F(-0.07, 0.50, -0.14)]
    [T(0.00, 0.00, 0.00)K(-0.09, 0.25, 0.19)K(-0.18, 0.44, 0.30)K(-0.47, 0.67, 0.36)K(-0.42, 0.59, 0.24)K(-0.62, 0.57, 0.12)K(-0.47, 0.40, 0.03)K(-0.48, 0.40, -0.02)K(-0.65, 0.62, -0.07)K(-1.10, 1.43, -0.25)K(-0.69, 1.45, -0.33)K(-0.31, 1.14, -0.30)F(-0.07, 0.50, -0.14)
    [T(0.00, 0.00, 0.00)K(-0.09, 0.25, 0.19)K(-0.18, 0.44, 0.30)K(-0.47, 0.67, 0.36)K(-0.42, 0.59, 0.24)K(-0.62, 0.57, 0.12)K(-0.47, 0.40, 0.03)K(-0.48, 0.40, -0.02)K(-0.65, 0.62, -0.07)K(-1.10, 1.43, -0.25)K(-0.69, 1.45, -0.33)K(-0.31, 1.14, -0.30)F(-0.07, 0.50, -0.14)]
T(0.00, 0.00, 0.00)K(-0.09, 0.25, 0.19)K(-0.18, 0.44, 0.30)K(-0.47, 0.67, 0.36)K(-0.42, 0.59, 0.24)K(-0.62, 0.57, 0.12)K(-0.47, 0.40, 0.03)K(-0.48, 0.40, -0.02)K(-0.65, 0.62, -0.07)K(-1.10, 1.43, -0.25)K(-0.69, 1.45, -0.33)K(-0.31, 1.14, -0.30)F(-0.07, 0.50, -0.14)]
*/