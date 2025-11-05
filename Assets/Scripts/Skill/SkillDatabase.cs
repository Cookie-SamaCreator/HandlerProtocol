using System.Collections.Generic;
using UnityEngine;

public static class SkillDatabase
{
    private static readonly Dictionary<string, SkillDefinition> _skills = new();

    public static void LoadAll()
    {
        _skills.Clear();
        var allSkills = Resources.LoadAll<SkillDefinition>("Skills");
        foreach (var s in allSkills)
            _skills[s.SkillID] = s;
    }

    public static SkillDefinition Get(string id)
    {
        if (_skills.Count == 0) LoadAll();

        if (_skills.TryGetValue(id, out var def))
            return def;

        Debug.LogError($"SkillDefinition not found: {id}");
        return null;
    }

    public static IEnumerable<SkillDefinition> GetAll() => _skills.Values;
}
