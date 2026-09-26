#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RobotStrategy.Movement;

namespace RobotStrategy.Battle
{
    // 再生前のMainSceneへ戦場の見本を表示します。
    public partial class BattleManager
    {
        internal GameObject CreateEditorBattlePreview(List<Object> resources)
        {
            if(Application.isPlaying || gameObject.scene.name!="MainScene")return null;
            if(mapCamera==null)mapCamera=Camera.main;
            if(mapCamera==null)return null;
            var oldTexture=whiteTexture;var oldSprite=whiteSprite;
            var oldForest=forestTile;var oldSand=sandTile;var oldRiver=riverTile;var oldMap=terrainMap;
            int firstPreviewUnit=units.Count;
            var root=new GameObject("Battle Preview (Editor Only)");
            root.transform.SetParent(transform,false);
            root.tag="EditorOnly";
            root.hideFlags=HideFlags.DontSave;
            try
            {
                whiteTexture=new Texture2D(1,1){hideFlags=HideFlags.HideAndDontSave};
                whiteTexture.SetPixel(0,0,Color.white);whiteTexture.Apply();resources.Add(whiteTexture);
                whiteSprite=Sprite.Create(whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
                whiteSprite.hideFlags=HideFlags.HideAndDontSave;resources.Add(whiteSprite);
                BuildMap();
                resources.Add(forestTile);resources.Add(sandTile);resources.Add(riverTile);
                forestTile.hideFlags=sandTile.hideFlags=riverTile.hideFlags=HideFlags.HideAndDontSave;
                terrainMap.transform.parent.SetParent(root.transform,false);
                var monster=SpawnUnit("中央ボス（プレビュー）",BattleTeam.Enemy,false,bossSprite,
                    new Vector3(.5f,.5f,0),bossStats,2.2f,false);
                monster.transform.SetParent(root.transform,true);
                float half=Mathf.Max(10,mapSize)/2f;
                float edge=Mathf.Max(1,half-Mathf.Clamp(cornerInset,2,half-1));
                Vector3[] corners={new Vector3(-edge,-edge),new Vector3(edge,-edge),new Vector3(-edge,edge),new Vector3(edge,edge)};
                for(int i=0;i<4;i++)
                {
                    Nation country=(Nation)(i+1);
                    var home=SpawnUnit(country+"国 拠点（プレビュー）",BattleTeam.Player,true,whiteSprite,corners[i],baseStats,1.5f);
                    home.SetNation(country);home.SetOwnership(i,true);ApplyNationColor(home);home.transform.SetParent(root.transform,true);
                    var kind=i==0?ChassisKind.Tank:i==1?ChassisKind.Fighter:i==2?ChassisKind.Giant:ChassisKind.Transformer;
                    var design=new RobotDesign(country+"国 機体（プレビュー）",new int[RobotDesign.ValueCount],kind,country);
                    int count=Mathf.Clamp(initialRobotCount,1,Mathf.Max(1,maxPlayerRobots));
                    for(int n=0;n<count;n++)
                    {
                        // Same placement search as combat, using the preview base temporarily.
                        bases.Add(home);
                        bool found=FindNationDeploymentPosition(country,out Vector3 position);
                        bases.Remove(home);
                        if(!found)break;
                        var unit=SpawnNationDesign(design,position);
                        unit.transform.SetParent(root.transform,true);
                    }
                }
                foreach(var node in root.GetComponentsInChildren<Transform>(true))
                    node.gameObject.hideFlags=HideFlags.DontSave;
                if(legacyCanvas!=null)legacyCanvas.SetActive(false);
                BuildAllUI();
                ShowPreparationPage(PreparationPage.Hidden);
                deploymentOverlay.SetActive(false);countdownOverlay.SetActive(false);resultOverlay.SetActive(false);
                cpLabel.text="戦闘画面の編集プレビュー";
                battleInfo.text="再生前の配置確認用です。実際の出撃設定はrobotdevelopから行います。";
                mapCamera.enabled=true;mapCamera.orthographic=true;
                mapCamera.transform.position=new Vector3(0,0,-10);
                mapCamera.transform.rotation=Quaternion.identity;
                mapCamera.orthographicSize=Mathf.Max(half+4,(half+4)/Mathf.Max(.1f,mapCamera.aspect));
                SceneView.RepaintAll();
                return root;
            }
            catch
            {
                Object.DestroyImmediate(root);
                throw;
            }
            finally
            {
                units.RemoveRange(firstPreviewUnit,units.Count-firstPreviewUnit);
                whiteTexture=oldTexture;whiteSprite=oldSprite;
                forestTile=oldForest;sandTile=oldSand;riverTile=oldRiver;terrainMap=oldMap;
            }
        }
    }

    [InitializeOnLoad]
    internal static class ScenePreview
    {
        private static GameObject preview;
        private static readonly List<Object> resources=new List<Object>();
        static ScenePreview()
        {
            EditorApplication.delayCall+=ShowIfMainScene;
            EditorSceneManager.sceneOpened+=(scene,mode)=>EditorApplication.delayCall+=ShowIfMainScene;
            EditorSceneManager.sceneClosing+=(scene,removing)=>Clear();
            AssemblyReloadEvents.beforeAssemblyReload+=Clear;
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.ExitingEditMode)Clear();
                if(state==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=ShowIfMainScene;
            };
        }

        [MenuItem("Tools/Robot Strategy/Preview Battle in MainScene")]
        private static void Refresh(){Clear();ShowIfMainScene();}

        private static void ShowIfMainScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||preview!=null)return;
            foreach(var controller in Object.FindObjectsByType<BattleManager>(FindObjectsSortMode.None))
            {
                if(controller.gameObject.scene.name!="MainScene"||EditorUtility.IsPersistent(controller))continue;
                try {preview=controller.CreateEditorBattlePreview(resources);}
                catch(System.Exception error){Clear();Debug.LogException(error,controller);}
                break;
            }
        }

        private static void Clear()
        {
            if(preview!=null)Object.DestroyImmediate(preview);
            preview=null;
            foreach(var resource in resources)if(resource!=null)Object.DestroyImmediate(resource);
            resources.Clear();
        }
    }
}
#endif
