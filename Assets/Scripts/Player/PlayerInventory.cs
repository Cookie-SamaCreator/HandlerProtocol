using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages player loadout persistence and provides access to saved loadouts across scenes.
/// On first run, generates default loadouts for all available Ciphers.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    #region Singleton
    public static PlayerInventory Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInventory();
        }
        else
        {
            Debug.Log("[PlayerInventory] Instance already exists - destroying duplicate");
            Destroy(gameObject);
        }
    }
    #endregion

    #region Private Fields
    /// <summary>
    /// Cache of all available CipherDefinitions loaded from Resources
    /// </summary>
    private List<CipherDefinition> availableCiphers = new();
    
    /// <summary>
    /// Path to CipherDefinitions in Resources folder
    /// </summary>
    private const string CIPHER_RESOURCES_PATH = "CipherDefinitions";
    #endregion

    #region Loadout Storage
    [Header("Saved Loadouts")]
    [Tooltip("List of player's saved loadout configurations")]
    public List<CipherLoadout> SavedLoadouts = new();

    public CipherLoadout GetLoadoutFor(string cipherID)
    {
        Debug.Log($"[PlayerInventory] cipherID = {cipherID}");
        var loadout = SavedLoadouts.FirstOrDefault(l => l.CipherID == cipherID);
        if (loadout == null)
        {
            Debug.LogError("[PlayerInventory] Loadout is null");
        }
        else
        {
            Debug.Log($"[PlayerInventory] Loadout = {loadout.CipherID}");
        }
        return loadout;
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

    /// <summary>
    /// Loads saved loadouts from persistent storage
    /// </summary>
    #region Initialization
    private void InitializeInventory()
    {
        LoadAvailableCiphers();
        LoadOrCreateLoadouts();
    }

    /// <summary>
    /// Loads all CipherDefinitions from the Resources folder
    /// </summary>
    private void LoadAvailableCiphers()
    {
        availableCiphers.Clear();
        var cipherDefs = Resources.LoadAll<CipherDefinition>(CIPHER_RESOURCES_PATH);
        
        if (cipherDefs == null || cipherDefs.Length == 0)
        {
            Debug.LogError($"[PlayerInventory] No CipherDefinitions found in Resources/{CIPHER_RESOURCES_PATH}");
            return;
        }

        availableCiphers.AddRange(cipherDefs);
        Debug.Log($"[PlayerInventory] Loaded {availableCiphers.Count} CipherDefinitions from Resources");
    }

    /// <summary>
    /// Loads saved loadouts or creates default ones if none exist
    /// </summary>
    private void LoadOrCreateLoadouts()
    {
        SavedLoadouts.Clear();
        var data = PlayerLoadoutStorage.Load();
        
        if (data.Loadouts.Count == 0)
        {
            Debug.Log("[PlayerInventory] No saved loadouts found - generating defaults");
            GenerateDefaultLoadouts();
            SaveLoadouts(); // Save the generated defaults
            return;
        }

        foreach (var loadout in data.Loadouts)
        {
            SavedLoadouts.Add(loadout);
        }
        
        Debug.Log($"[PlayerInventory] Loaded {data.Loadouts.Count} loadouts from storage");
    }

    /// <summary>
    /// Creates default loadouts for each available Cipher using their definition
    /// </summary>
    private void GenerateDefaultLoadouts()
    {
        foreach (var cipher in availableCiphers)
        {
            var defaultLoadout = cipher.GenerateDefaultLoadout();
            SavedLoadouts.Add(defaultLoadout);
            Debug.Log($"[PlayerInventory] Generated default loadout for {cipher.CipherID}");
        }
    }
    #endregion

    public CipherLoadout GetOrGenerateLoadout(CipherDefinition def)
    {
        var existing = GetLoadoutFor(def.CipherID);
        if (existing != null)
            return existing;

        // Aucun loadout ? → génération par défaut
        var newLoadout = def.GenerateDefaultLoadout();
        SavedLoadouts.Add(newLoadout);
        SaveLoadouts();
        return newLoadout;
    }

    public CipherDefinition GetCipherDefinition(string cipher_id)
    {
        return availableCiphers.Find(c => c.CipherID == cipher_id);
    }

    #endregion
    
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

