using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRSketchingGeometryPackage.Samples.ExampleScenes.Scripts
{
    public class LSystemZeichner : MonoBehaviour
    {
        [Header("L-System Prefab")]
        [SerializeField]
        private GameObject lSystemPrefab;

        [Header("L-System Settings")]
        [SerializeField]
        private string axiom = "F";
        [SerializeField]
        private int iterations = 4;
        [SerializeField]
        private List<string> ruleStrings = new List<string> { "F = F[+F]F[-F]F" };

        [Header("Platzierung")]
        [SerializeField]
        private float spawnDistance = 2f;

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                PlaceLSystem();
            }
        }

        private void PlaceLSystem()
        {
            Vector3 position = GetDrawPointInFrontOfCamera();
            Quaternion rotation = Quaternion.LookRotation(Camera.main.transform.right);

            GameObject instance = Instantiate(lSystemPrefab, position, rotation);

            if (instance.TryGetComponent(out LSystemGenerator generator))
            {
                var rules = ParseRules(ruleStrings);
                generator.Generate(axiom, rules, iterations);
            }
            else
            {
                Debug.LogWarning("Prefab has no LSystemGenerator attached.");
            }
        }

        private Vector3 GetDrawPointInFrontOfCamera()
        {
            Camera cam = Camera.main;
            return cam.transform.position + cam.transform.forward * spawnDistance;
        }

        private Dictionary<char, string> ParseRules(List<string> rules)
        {
            var dict = new Dictionary<char, string>();
            foreach (var r in rules)
            {
                var parts = r.Split('=');
                if (parts.Length == 2 && parts[0].Length == 1)
                {
                    dict[parts[0][0]] = parts[1];
                }
            }
            return dict;
        }
    }
}