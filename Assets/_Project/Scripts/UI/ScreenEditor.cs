using UnityEngine;
using UnityEngine.UI;

namespace RobotStrategy.Battle
{
    // 再生前に編集できる戦闘画面を作ります。
    public partial class BattleManager
    {
        [Header("Editable scene UI")]
        [SerializeField] private Canvas sceneUICanvas;

        private GameObject FindUI(Transform parent, string key)
        {
            foreach (Transform child in parent)
            {
                var tag = child.GetComponent<ScreenPart>();
                if (tag != null && tag.BindingKey == key) return child.gameObject;
            }
            return null;
        }

        private void TagUI(GameObject obj, string key)
        {
            obj.AddComponent<ScreenPart>().Initialize(key);
        }

        private static string PositionKey(string prefix, float x, float y) =>
            prefix + ":" + x.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ":" + y.ToString(System.Globalization.CultureInfo.InvariantCulture);

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Robot Strategy/Create Editable Battle UI")]
        private static void CreateEditableBattleUI()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Stop Play mode before creating the editable UI.");
                return;
            }
            var controller = FindAnyObjectByType<BattleManager>();
            if (controller == null)
            {
                Debug.LogWarning("Open robotdevelop or MainScene with BattleManager first.");
                return;
            }
            bool existed = controller.sceneUICanvas != null;
            UnityEditor.Undo.IncrementCurrentGroup();
            int group = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName("Create Editable Battle UI");
            UnityEditor.Undo.RecordObject(controller, "Assign Battle UI");
            if(existed)UnityEditor.Undo.RegisterFullObjectHierarchyUndo(controller.sceneUICanvas.gameObject,"Update Battle UI");
            controller.BuildAllUI();
            controller.cpLabel.text = "国家選択後にCPを表示";
            controller.battleInfo.text = "国家 → 初期機体 → ポイント配分 → 戦闘";
            controller.developmentTitle.text = "開発画面";
            controller.ShowPreparationPage(PreparationPage.Map);
            if(!existed)UnityEditor.Undo.RegisterCreatedObjectUndo(controller.sceneUICanvas.gameObject, "Create Battle UI");
            UnityEditor.EditorUtility.SetDirty(controller);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            UnityEditor.Undo.CollapseUndoOperations(group);
            UnityEditor.Selection.activeGameObject = controller.sceneUICanvas.gameObject;
        }
#endif
    }
}

