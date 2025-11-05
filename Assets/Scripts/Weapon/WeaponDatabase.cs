using System.Collections.Generic;
using UnityEngine;

public static class WeaponDatabase
{
    private static readonly Dictionary<string, WeaponDefinition> _weapons = new();

    public static void LoadAll()
    {
        _weapons.Clear();
        var allWeapons = Resources.LoadAll<WeaponDefinition>("Weapons");
        foreach (var w in allWeapons)
            _weapons[w.WeaponID] = w;
    }

    public static WeaponDefinition Get(string id)
    {
        if (_weapons.Count == 0) LoadAll();

        if (_weapons.TryGetValue(id, out var def))
            return def;

        Debug.LogError($"WeaponDefinition not found: {id}");
        return null;
    }

    public static IEnumerable<WeaponDefinition> GetAll() => _weapons.Values;
}
