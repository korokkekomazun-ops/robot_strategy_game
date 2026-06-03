using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // TextMeshProを使用

public class LobbyUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject browserPanel;
    [SerializeField] private GameObject createRoomPanel;

    [Header("Browser UI")]
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button goCreateRoomButton;
    [SerializeField] private Button backToTitleButton;
    [SerializeField] private Transform sessionListContent; // スクロールビューの中身
    [SerializeField] private GameObject sessionButtonPrefab; // リストの1項目のプレハブ

    [Header("Create Room UI")]
    [SerializeField] private TMP_InputField hostNameInput;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button executeCreateButton;
    [SerializeField] private Button cancelCreateButton;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    private ISession currentSession;
    private bool isServicesInitialized = false;

    private async void Start()
    {
        // イベント登録
        refreshButton.onClick.AddListener(() => _ = RefreshBrowserAsync());
        goCreateRoomButton.onClick.AddListener(() => SwitchPanel(createRoomPanel));
        backToTitleButton.onClick.AddListener(() => SceneManager.LoadScene("Title"));

        executeCreateButton.onClick.AddListener(() => _ = CreateSessionAsync());
        cancelCreateButton.onClick.AddListener(() => SwitchPanel(browserPanel));

        // 初期化処理の開始
        await InitializeNetworkAsync();
    }

    private void SwitchPanel(GameObject activePanel)
    {
        loadingPanel.SetActive(false);
        browserPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        activePanel.SetActive(true);
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.Log($"[Lobby Status] {message}");
    }

    private async Task InitializeNetworkAsync()
    {
        SwitchPanel(loadingPanel);
        SetStatus("ネットワークを準備中...");
        try
        {
            if (!isServicesInitialized)
            {
                InitializationOptions options = new InitializationOptions();
                options.SetProfile(System.Guid.NewGuid().ToString().Substring(0, 8));
                await UnityServices.InitializeAsync(options);
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                isServicesInitialized = true;
            }
            await RefreshBrowserAsync();
        }
        catch (Exception e)
        {
            SetStatus("初期化エラー");
            Debug.LogError(e);
        }
    }

    private async Task RefreshBrowserAsync()
    {
        SwitchPanel(loadingPanel);
        SetStatus("掲示板を更新中...");
        try
        {
            var options = new QuerySessionsOptions();
            var result = await MultiplayerService.Instance.QuerySessionsAsync(options);
            var sessions = result.Sessions.ToList();

            string filter = searchInput.text.ToLower();
            if (!string.IsNullOrEmpty(filter))
            {
                sessions = sessions.Where(s => s.Name.ToLower().Contains(filter)).ToList();
            }

            // UIリストの更新
            UpdateSessionListUI(sessions);

            SetStatus($"更新完了 ({sessions.Count}件)");
            SwitchPanel(browserPanel);
        }
        catch (Exception e)
        {
            SetStatus("更新失敗");
            Debug.LogError(e);
            SwitchPanel(browserPanel);
        }
    }

    private void UpdateSessionListUI(List<ISessionInfo> sessions)
    {
        // 古いリストを削除
        foreach (Transform child in sessionListContent) Destroy(child.gameObject);

        foreach (var session in sessions.Take(10))
        {
            string rawName = session.Name;
            int currentPlayers = session.MaxPlayers - session.AvailableSlots;
            string rName = rawName, hostName = "Unknown"; bool hasPassword = false;

            string[] parts = rawName.Split(new string[] { ":::" }, StringSplitOptions.None);
            if (parts.Length >= 3) { rName = parts[0]; hostName = parts[1]; hasPassword = (parts[2] == "true"); }

            // プレハブを生成
            GameObject btnObj = Instantiate(sessionButtonPrefab, sessionListContent);

            // ボタンのテキスト設定 (子オブジェクトのTMPを取得)
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            btnText.text = $"[{currentPlayers}/{session.MaxPlayers}] {rName} (Host: {hostName}) {(hasPassword ? "🔒" : "")}";

            // クリックイベントの登録
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                // TODO: 実際の開発ではここでパスワード入力ポップアップを挟む
                _ = JoinSessionAsync(session.Id, "");
            });
        }
    }

    private async Task CreateSessionAsync()
    {
        SwitchPanel(loadingPanel);
        SetStatus("部屋を作成中...");
        try
        {
            string rName = roomNameInput.text.Replace(":::", "");
            string hName = hostNameInput.text.Replace(":::", "");
            string pass = passwordInput.text;
            bool hasPass = !string.IsNullOrEmpty(pass);

            string combinedName = $"{rName}:::{hName}:::{(hasPass ? "true" : "false")}";

            var options = new SessionOptions { MaxPlayers = 4, Name = combinedName, IsPrivate = false }.WithRelayNetwork();
            if (hasPass) options.Password = pass;

            currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);

            // ★重要：ホストがゲームシーンをロードし、クライアントも自動でついてこさせる
            NetworkManager.Singleton.SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            SetStatus("作成失敗");
            Debug.LogError(e);
            SwitchPanel(createRoomPanel);
        }
    }

    private async Task JoinSessionAsync(string sessionId, string password)
    {
        SwitchPanel(loadingPanel);
        SetStatus("参加中...");
        try
        {
            var options = new JoinSessionOptions();
            if (!string.IsNullOrEmpty(password)) options.Password = password;

            currentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId, options);

            // クライアント側はホストのシーン移動に自動追従するため、ここで LoadScene は呼ばない
        }
        catch (Exception e)
        {
            SetStatus("参加失敗");
            Debug.LogError(e);
            SwitchPanel(browserPanel);
        }
    }
}