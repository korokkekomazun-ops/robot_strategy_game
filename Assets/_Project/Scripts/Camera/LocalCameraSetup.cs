using Unity.Netcode;
using UnityEngine;

/// <summary>
/// マルチプレイ時、自分のキャラクター（司令塔）以外のカメラを強制的に無効化するクラス
/// </summary>
public class LocalCameraSetup : NetworkBehaviour
{
    [Header("Camera References")]
    [SerializeField, Tooltip("このプレイヤープレハブの子にあるCameraオブジェクト")]
    private GameObject _localCameraObject;

    [SerializeField, Tooltip("二重音声防止用のAudioListener")]
    private AudioListener _audioListener;

    public override void OnNetworkSpawn()
    {
        // IsOwner = 「このネットワークオブジェクトの操作権限が自分にあるか」
        if (!IsOwner)
        {
            // 自分以外のプレイヤーのカメラと耳を完全にオフにする
            if (_localCameraObject != null) _localCameraObject.SetActive(false);
            if (_audioListener != null) _audioListener.enabled = false;
        }
        else
        {
            // 自分の場合は確実にオンにする
            if (_localCameraObject != null) _localCameraObject.SetActive(true);
            if (_audioListener != null) _audioListener.enabled = true;
        }
    }
}
