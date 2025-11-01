using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using UnityEngine;

public class WeaponSlotUI : MonoBehaviour
{
    public int slotIndex; 
    public CanvasGroup canvasGroup;
    public Image weaponIcon;
    public TMP_Text ammoText;
    public bool isActiveWeapon = false;

    public void SelectWeapon()
    {
        if (isActiveWeapon) { return; }
        isActiveWeapon = true;
        canvasGroup.alpha = 1;
    }

    public void UnselectWeapon()
    {
        isActiveWeapon = false;
        canvasGroup.alpha = 0.6f;
    }
}
