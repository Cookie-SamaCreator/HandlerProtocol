using UnityEngine;

public enum WeaponType { Primary, Secondary }
public enum FireMode { Semi, Burst, FullAuto }

[CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Weapons/WeaponDefinition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identification")]
    public string WeaponID;
    public string DisplayName;

    [Header("Type & Behaviour")]
    public WeaponType Type;
    public FireMode FireMode;
    public float FireRate = 600f; // shots/minute
    public float Damage = 20f;
    public int BurstCount = 3;

    [Header("Prefabs & Visuals")]
    public GameObject WeaponModelPrefab;
    public GameObject MuzzleFlashPrefab;
    public Projectile ProjectilePrefab;

    [Header("Audio")]
    public AudioClip FireSound;
    public AudioClip ReloadSound;
}
