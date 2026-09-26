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
        // Captions and artwork are saved in Title.unity and edited in the Inspector.
        if(startOfflineButton!=null)startOfflineButton.onClick.AddListener(OnStartOfflineClicked);
        if(startOnlineButton!=null)startOnlineButton.onClick.AddListener(OnStartOnlineClicked);
        if(quitButton!=null)quitButton.onClick.AddListener(QuitGame);
    }

    private void OnStartOnlineClicked()
    {
        RobotStrategy.Battle.LanConnect.OpenConnectionPage=true;
        SceneManager.LoadScene("robotdevelop");
    }

    private void OnStartOfflineClicked()
    {
        RobotStrategy.Battle.LanConnect.OpenConnectionPage=false;
        SceneManager.LoadScene("robotdevelop");
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