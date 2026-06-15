using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField, Tooltip("プロジェクトウィンドウにあるNetworkManagerのプレハブをセット")]
    private GameObject networkManagerPrefab;

    [Header("UI References")]
    [SerializeField] private Button startOfflineButton;
    [SerializeField] private Button startOnlineButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        if (NetworkManager.Singleton == null)
        {
            if (networkManagerPrefab != null)
            {
                Instantiate(networkManagerPrefab);
                Debug.Log("<color=green>[Title] NetworkManagerをプレハブから新規生成しました！</color>");
            }
        }
        else
        {
            // 既に存在する場合は何もしない（ここでログを出すのがポイント）
            Debug.Log("<color=yellow>[Title] 既にNetworkManagerが生きているため、生成をスキップしました！</color>");
        }
    }

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