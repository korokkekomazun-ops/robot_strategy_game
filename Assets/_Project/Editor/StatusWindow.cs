#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RobotStrategy.Battle
{
    // 機体・敵・適性をまとめて編集するUnity専用画面です。
    public class StatusWindow : EditorWindow
    {
        private BattleManager target;
        private Vector2 scroll;
        private int page;

        [MenuItem("Tools/Robot Strategy/Balance Settings")]
        private static void Open() => GetWindow<StatusWindow>("ロボット・敵の調整");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("停止中：共通Prefabを選んで編集・保存。再生中：シーン上の対象を選び、反映ボタンで試せます。再生中の変更は終了すると戻ります。",MessageType.Info);
            target=(BattleManager)EditorGUILayout.ObjectField("調整対象",target,typeof(BattleManager),true);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("共通Prefabを選択"))
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/System/BattleManager.prefab");
                if(prefab!=null)target=prefab.GetComponent<BattleManager>();
            }
            if(GUILayout.Button("現在のシーンを選択"))target=Object.FindAnyObjectByType<BattleManager>();
            EditorGUILayout.EndHorizontal();
            if(target==null)return;
            var so=new SerializedObject(target);so.Update();
            page=GUILayout.Toolbar(page,new[]{"機体","敵・拠点","相性・適性","強化・倍率"});
            scroll=EditorGUILayout.BeginScrollView(scroll);
            if(page==0)
            {
                EditorGUILayout.HelpBox("statsで基本能力、ground/sea/sky/sandで初期適性を設定します。開発で購入した強化はこの基本値に加算します。",MessageType.None);
                string[] fields={"tankBalance","fighterBalance","shipBalance","transformerBalance","humanoidBalance","giantBalance"};
                string[] names={"戦車","戦闘機","船","トランスフォーマー","人型","巨大機体"};
                for(int i=0;i<fields.Length;i++)Draw(so,fields[i],names[i]);
            }
            if(page==1)
            {
                Draw(so,"spawnInterval","増援の間隔（秒）");
                Draw(so,"minEnemiesPerWave","1回の出現数・最小");Draw(so,"maxEnemiesPerWave","1回の出現数・最大");
                Draw(so,"maxSmallEnemies","小型敵の同時生存上限");Draw(so,"spawnRadius","ボスからの出現距離");
                Draw(so,"bossStats","中央ボス");Draw(so,"smallEnemyStats","小型モンスター");Draw(so,"baseStats","拠点");
            }
            if(page==2)
            {
                Draw(so,"battleMap","編集プレビューのマップ");
                Draw(so,"riverWidth","川幅（マス）");Draw(so,"riverSprite","川の素材（空欄なら青い水面）");
                Draw(so,"sandStripWidth","砂地の道幅（再生開始時に生成）");Draw(so,"sandSprite","砂地の素材");
                Draw(so,"terrainCMultiplier","C適性の速度倍率");Draw(so,"terrainBMultiplier","B適性の速度倍率");Draw(so,"terrainAMultiplier","A適性の速度倍率");
                EditorGUILayout.HelpBox("戦闘機は空の適性、それ以外は現在の森・陸／海／砂地の適性を使用します。",MessageType.None);
                Draw(so,"chassisMatchups","機体同士の攻撃相性");
                EditorGUILayout.HelpBox("攻撃側・防御側の種類と倍率を指定します。未登録は1倍。同じ組み合わせは先頭の設定を使用します。怪獣と拠点には適用しません。",MessageType.None);
            }
            if(page==3)
            {
                Draw(so,"giantStatMultiplier","巨大機体のHP・攻撃・装甲倍率");Draw(so,"giantSpeedMultiplier","巨大機体の速度倍率");
                Draw(so,"giantCPMultiplier","巨大機体のCP倍率");
                Draw(so,"hpPerLevel","HP強化1段階");Draw(so,"attackPerLevel","攻撃強化1段階");
                Draw(so,"armourPerLevel","装甲強化1段階");Draw(so,"speedPerLevel","速度強化1段階");
                Draw(so,"nationDDamageMultiplier","D国の対他国攻撃倍率");
            }
            EditorGUILayout.EndScrollView();
            so.ApplyModifiedProperties();
            using(new EditorGUI.DisabledScope(!Application.isPlaying||EditorUtility.IsPersistent(target)))
                if(GUILayout.Button("生存中の機体・敵へ反映（HP割合を維持）"))target.ApplyBalanceToLivingUnits();
            using(new EditorGUI.DisabledScope(Application.isPlaying))
                if(GUILayout.Button("変更を保存"))
                {
                    if(EditorUtility.IsPersistent(target))AssetDatabase.SaveAssets();
                    else UnityEditor.SceneManagement.EditorSceneManager.SaveScene(target.gameObject.scene);
                }
        }

        private static void Draw(SerializedObject so,string field,string label)
        {
            var property=so.FindProperty(field);
            if(property!=null)EditorGUILayout.PropertyField(property,new GUIContent(label),true);
        }
    }
}
#endif
