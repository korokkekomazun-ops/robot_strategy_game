using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Services.Multiplayer;

public class MainUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button leaveRoomButton;

    private void Start()
    {
        // 初期状態はポーズ非表示
        pausePanel.SetActive(false);

        resumeButton.onClick.AddListener(TogglePause);
        leaveRoomButton.onClick.AddListener(DisconnectAndLeaveRoom);
    }

    private void Update()
    {
        // Escキーでポーズメニューの切り替え
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        pausePanel.SetActive(!pausePanel.activeSelf);
    }

    private void DisconnectAndLeaveRoom()
    {
        Debug.Log("ルームから退出します...");

        // NetworkManagerのシャットダウン
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Titleシーンへ強制的に戻す
        SceneManager.LoadScene("Title");
    }
}