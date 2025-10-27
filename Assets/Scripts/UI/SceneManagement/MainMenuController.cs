using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button createBtn;
    [SerializeField] private Button joinBtn;
    [SerializeField] private Button quitBtn;
    [SerializeField] private SceneTransition transitionImage;

    private void Awake()
    {
        createBtn.onClick.AddListener(OnCreateClicked);
        joinBtn.onClick.AddListener(OnJoinClicked);
        quitBtn.onClick.AddListener(OnQuitClicked);
    }

    private void OnCreateClicked()
    {
        // TODO: call LobbyManager.CreateLobby() (network integration)
        Debug.Log("Create Game clicked");
        // For now, just load the prototype scene as host
        //SceneManager.LoadScene("PrototypeArena");
        transitionImage.LoadScene("PrototypeArena");
    }

    private void OnJoinClicked()
    {
        string code = joinCodeInput != null ? joinCodeInput.text : "";
        // TODO: call LobbyManager.JoinLobby(code)
        Debug.Log($"Join Game clicked with code: {code}");
        SceneManager.LoadScene("PrototypeArena");
    }

    private void OnQuitClicked()
    {
        Application.Quit();
    }
}
