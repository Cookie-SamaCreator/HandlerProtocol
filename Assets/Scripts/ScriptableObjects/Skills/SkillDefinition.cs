using UnityEngine;

public enum SkillType { SelfActive, Throwable, AOE, Targeted }
public enum SkillCategory { Primary, Secondary, Ultimate }

[CreateAssetMenu(fileName = "SkillDefinition", menuName = "Cipher/SkillDefinition")]
public class SkillDefinition : ScriptableObject
{
    [Header("Identification")]
    public string SkillID;
    public string DisplayName;

    [Header("Type & Behaviour")]
    public SkillType Type;
    public SkillCategory Category;
    public float Duration = 5f;
    public float Cooldown = 10f;

    [Header("Prefabs")]
    public GameObject ProjectilePrefab;
    public GameObject AreaPrefab;

    [Header("VFX / Audio")]
    public AudioClip ActivationSound;
    public GameObject ActivationVFX;
}
