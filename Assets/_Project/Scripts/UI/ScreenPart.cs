using UnityEngine;

namespace RobotStrategy.Battle
{
    // 画面部品を再利用するための管理番号を保存します。
    // Retains the binding when an artist renames or repositions a UI object.
    [AddComponentMenu("")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RobotStrategy.Prototype", null, "PrototypeUIElement")]
    public sealed class ScreenPart : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string bindingKey;
        public string BindingKey => bindingKey;
        public void Initialize(string key) => bindingKey = key;
    }
}
