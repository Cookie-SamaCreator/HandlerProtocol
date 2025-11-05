using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CipherLoadout", menuName = "Scriptable Objects/CipherLoadout")]
public class CipherDefinition : ScriptableObject
{
    public string CipherID; // ex: "cipher_warden"
    public string DisplayName;
    public GameObject CipherPrefab;
    public List<WeaponDefinition> AvailableWeapons;
    public List<SkillDefinition> AvailableSkills;
}
