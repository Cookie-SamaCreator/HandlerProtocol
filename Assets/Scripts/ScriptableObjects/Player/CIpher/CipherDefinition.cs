using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CipherLoadout", menuName = "Cipher/CipherDefinition")]
public class CipherDefinition : ScriptableObject
{
    [Header("Identification")]
    public string CipherID; // ex: "cipher_warden"
    public string DisplayName;
    public GameObject CipherPrefab;
    public List<WeaponDefinition> AvailableWeapons;
    public List<WeaponDefinition> PrimaryWeapons;
    public List<WeaponDefinition> SecondaryWeapons;
    public List<SkillDefinition> AvailableSkills;

    private List<string> skillsIDs;

    public CipherLoadout GenerateDefaultLoadout()
    {
        foreach (var w in AvailableWeapons)
        {
            if (w.Type == WeaponType.Primary)
            {
                PrimaryWeapons.Add(w);
            }
            else
            {
                SecondaryWeapons.Add(w);
            }
        }

        foreach (var s in AvailableSkills)
        {
            skillsIDs.Add(s.SkillID);
        }

        var loadout = new CipherLoadout
        {
            CipherID = this.CipherID,
            FirstPrimaryWeaponID = PrimaryWeapons[0].WeaponID,
            SecondPrimaryWeaponID = PrimaryWeapons[1].WeaponID,
            SecondaryWeaponID = SecondaryWeapons[0].WeaponID,
            SkillIDs = skillsIDs
        };
        return loadout;
    }
}
