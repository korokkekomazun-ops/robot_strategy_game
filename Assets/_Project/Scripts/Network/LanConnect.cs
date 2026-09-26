using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RobotStrategy.Battle
{
    // 同じLANのホストへ接続し、戦闘シーンへ入ります。
    // Direct-IP LAN transport. No online service account or Relay is required.
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RobotStrategy.Prototype", null, "LanBattleConnection")]
    public sealed class LanConnect : MonoBehaviour
    {
        public static string Status { get; private set; } = "";
        private NetworkManager manager;
        private bool entered;
        public static bool OpenConnectionPage;
        public static string LocalAddresses
        {
            get
            {
                try
                {
                    var addresses=System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;
                    return string.Join(" / ",System.Array.ConvertAll(System.Array.FindAll(addresses,
                        a=>a.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork&&!System.Net.IPAddress.IsLoopback(a)),a=>a.ToString()));
                }
                catch{return "Windowsのネットワーク設定でIPv4を確認";}
            }
        }

        public static bool Connect(bool host,string address)
        {
            var nm=NetworkManager.Singleton;
            if(nm!=null&&(nm.IsListening||nm.ShutdownInProgress)){Status="接続済みです。";return false;}
            if(!host && !System.Net.IPAddress.TryParse(address,out _)){Status="ホストのIPアドレスを入力してください。";return false;}
            if(nm==null)nm=new GameObject("LAN Network",typeof(NetworkManager)).GetComponent<NetworkManager>();
            DontDestroyOnLoad(nm.gameObject);
            Application.runInBackground=true;
            var transport=nm.GetComponent<UnityTransport>();
            if(transport==null)transport=nm.gameObject.AddComponent<UnityTransport>();
            nm.NetworkConfig.NetworkTransport=transport;
            nm.NetworkConfig.ProtocolVersion=2; // Map selection is part of the shared match state.
            nm.NetworkConfig.EnableSceneManagement=false;
            nm.NetworkConfig.PlayerPrefab=null;
            nm.NetworkConfig.ConnectionApproval=true;
            transport.SetConnectionData(host?"127.0.0.1":address,7777,host?"0.0.0.0":null);
            nm.ConnectionApprovalCallback=(request,response)=>
            {
                bool full=nm.ConnectedClientsIds.Count>=4;
                response.Approved=!full && !BattleManager.LanMatchStarted;
                response.CreatePlayerObject=false;
                response.Reason=full?"満員です（4人）":"対戦開始後は参加できません。";
                response.Pending=false;
            };
            var connection=nm.GetComponent<LanConnect>();
            if(connection==null)connection=nm.gameObject.AddComponent<LanConnect>();
            connection.Unsubscribe();connection.StopAllCoroutines();
            connection.manager=nm;connection.entered=false;
            nm.OnClientConnectedCallback+=connection.OnConnected;
            nm.OnClientDisconnectCallback+=connection.OnDisconnected;
            Status=host?"部屋を作成中…":"接続中…";
            bool started;
            try{started=host?nm.StartHost():nm.StartClient();}
            catch(System.Exception error)
            {Status="接続開始エラー："+error.Message;connection.Unsubscribe();nm.Shutdown();return false;}
            if(!started){Status="接続を開始できませんでした。";connection.Unsubscribe();return false;}
            connection.StartCoroutine(connection.Timeout());
            return true;
        }

        private IEnumerator Timeout()
        {
            yield return new WaitForSecondsRealtime(15);
            if(!entered){Status="接続できませんでした。IP・LAN・ファイアウォールを確認してください。";manager.Shutdown();Unsubscribe();}
        }
        private void OnConnected(ulong id)
        {
            if(id==manager.LocalClientId&&!entered){entered=true;StartCoroutine(Enter());}
        }
        private IEnumerator Enter()
        {
            yield return null;
            if(!Application.CanStreamedLevelBeLoaded("MainScene")){Status="MainSceneをBuild Profilesへ登録してください。";manager.Shutdown();yield break;}
            BattleStart.TryConsume(out _);
            SceneManager.LoadScene("MainScene");
        }
        private void OnDisconnected(ulong id)
        {
            if(id==manager.LocalClientId){Status="接続が切れました。"+manager.DisconnectReason;StopAllCoroutines();Unsubscribe();}
        }
        private void Unsubscribe()
        {
            if(manager==null)return;
            manager.OnClientConnectedCallback-=OnConnected;manager.OnClientDisconnectCallback-=OnDisconnected;
        }
        private void OnDestroy()=>Unsubscribe();
    }
}
