using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button quitBtn;
    private void Awake()
    {
        quitBtn.onClick.AddListener(OnQuitClicked);
    }

    private void OnQuitClicked()
    {
        Application.Quit();
    }
}
