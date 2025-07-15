using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Beschreibt ein L-System mit Axiom, Regeln und Metadaten.
/// </summary>
[System.Serializable]
public class LSystem
{
    /// <summary>
    /// Das Startwort des L-Systems (z. B. "F").
    /// </summary>
    public string Axiom;

    /// <summary>
    /// Die Produktionsregeln des L-Systems.
    /// Beispiel: 'F' -> "[aX][bX]"
    /// </summary>
    public Dictionary<char, string> Rules = new Dictionary<char, string>();

    /// <summary>
    /// Anzahl der Iterationen für das L-System.
    /// </summary>
    public int Iterations;

    /// <summary>
    /// Optional: Liste der ursprünglich gezeichneten Linienpunkte.
    /// </summary>
    public List<List<Vector3>> OriginalLineData = new List<List<Vector3>>();

    public LSystem() { }

    public LSystem(string axiom, Dictionary<char, string> rules, int iterations)
    {
        Axiom = axiom;
        Rules = new Dictionary<char, string>(rules);
        Iterations = iterations;
    }

    /// <summary>
    /// Gibt die Produktionsregel für ein gegebenes Symbol zurück, falls vorhanden.
    /// </summary>
    public string GetRule(char symbol)
    {
        return Rules.ContainsKey(symbol) ? Rules[symbol] : null;
    }

    /// <summary>
    /// Fügt eine neue Regel hinzu oder ersetzt eine bestehende.
    /// </summary>
    public void AddOrUpdateRule(char symbol, string rule)
    {
        Rules[symbol] = rule;
    }

    /// <summary>
    /// Gibt das L-System als lesbare Zeichenkette aus.
    /// </summary>
    public override string ToString()
    {
        string result = $"Axiom: {Axiom}\nIterations: {Iterations}\nRules:\n";
        foreach (var kv in Rules)
        {
            result += $"{kv.Key} -> {kv.Value}\n";
        }
        return result;
    }
}

