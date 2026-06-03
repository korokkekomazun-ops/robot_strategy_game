using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button startOfflineButton;
    [SerializeField] private Button startOnlineButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        // ボタンにイベントを登録
        startOfflineButton.onClick.AddListener(OnStartOfflineClicked);
        startOnlineButton.onClick.AddListener(OnStartOnlineClicked);
        quitButton.onClick.AddListener(QuitGame);
    }

    private void OnStartOnlineClicked()
    {
        // オンラインモードが選ばれたら、Lobbyシーンへ移動する
        SceneManager.LoadScene("Lobby");
    }

    private void OnStartOfflineClicked()
    {
        Debug.Log("オフラインモードは開発中です。");
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}