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

        [SerializeField] private DefaultReferences defaults;
        [SerializeField] private Material customMaterial;
        [SerializeField] private AdvancedLSystemInterpreter generator;
        [SerializeField] private Drawer drawer;
        [SerializeField] private Dictionary<char, string> rules;


        [Header("L-System Settings")]
        [SerializeField] private Transform lSystemParent;


        public LSystem GenerateParaRulesFromMultipleLines(List<LineSketchObject> lines)
        {
            rules = new Dictionary<char, string>();
            if (lines == null || lines.Count == 0)  return null;

            
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

            LSystem lSystem = new LSystem(axiom,rules);
            
            
            return lSystem;

        }

    }
}
