using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public List<CipherLoadout> SavedLoadouts = new();

    public CipherLoadout GetLoadoutFor(string cipherID)
    {
        return SavedLoadouts.FirstOrDefault(l => l.CipherID == cipherID);
    }

    public void SaveLoadout(CipherLoadout loadout, bool forceSave = false)
    {
        // Find existing loadout with the same CipherID
        int existingIndex = SavedLoadouts.FindIndex(l => l.CipherID == loadout.CipherID);

        if (existingIndex >= 0)
        {
            // Update the existing entry
            SavedLoadouts[existingIndex] = loadout;
        }
        else
        {
            // Add a new entry
            SavedLoadouts.Add(loadout);
        }

        if (forceSave)
        {
            SaveLoadouts();
        }

    }

    public void SaveLoadouts()
    {
        if (SavedLoadouts.Count > 0)
        {
            PlayerLoadoutStorage.Save(new PlayerLoadoutData { Loadouts = SavedLoadouts });
        }
        else
        {
            Debug.LogWarning("[PlayerInventory] SavedLoadouts is empty !");
        }
    }

    public void LoadLoadouts()
    {
        var data = PlayerLoadoutStorage.Load();
        if (data.Loadouts.Count == 0)
        {
            Debug.LogWarning("[PlayerInventory] No loadouts detected");
            return;
        }

        foreach (var l in data.Loadouts)
        {
            SavedLoadouts.Add(l);
        }
    }
    
    public void InitializeLoadouts(List<CipherLoadout> startingLoadouts)
    {
        SavedLoadouts.Clear();
        foreach (var l in startingLoadouts)
        {
            SavedLoadouts.Add(l);
        }
        SaveLoadouts();
    }
    
}

[System.Serializable]
public class PlayerLoadoutData
{
    public List<CipherLoadout> Loadouts;
}

public static class PlayerLoadoutStorage
{
    private static readonly string SaveKey = "PlayerLoadouts";

    public static void Save(PlayerLoadoutData data)
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static PlayerLoadoutData Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
            return new PlayerLoadoutData { Loadouts = new List<CipherLoadout>() };

        return JsonUtility.FromJson<PlayerLoadoutData>(PlayerPrefs.GetString(SaveKey));
    }
}

