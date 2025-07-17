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
    /// Optional: Liste der ursprünglich gezeichneten Linienpunkte.
    /// </summary>
    public List<List<Vector3>> OriginalLineData = new List<List<Vector3>>();

    public LSystem() { }

    public LSystem(string axiom, Dictionary<char, string> rules)
    {
        Axiom = axiom;
        Rules = new Dictionary<char, string>(rules);
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
        string result = $"Axiom: {Axiom}\n Rules:\n";
        foreach (var kv in Rules)
        {
            if (char.IsUpper(kv.Key))
            {
                result += $"{kv.Key} -> {kv.Value}\n";
            }
            else
            {
                result += $"Line {kv.Key}\n";
            }
        }
        return result;
    }
}

