using UnityEngine;
using UnityEngine.UI;

namespace RobotStrategy.Battle
{
    // 画面へ鉄板・縁・ねじの見た目を付けます。
    public partial class BattleManager
    {
        private void StyleMetalPanel(RectTransform panel, bool button)
        {
            // An existing decoration belongs to the user: preserve all Inspector edits.
            if(panel.Find("Metal Trim")!=null)return;
            panel.GetComponent<Image>().color=button
                ? new Color(.22f,.27f,.30f,1) : new Color(.075f,.095f,.115f,.98f);
            var root=new GameObject("Metal Trim",typeof(RectTransform));
            root.transform.SetParent(panel,false);
            var rect=root.GetComponent<RectTransform>();
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            root.transform.SetAsFirstSibling();
            MetalLine(rect,"Upper steel edge",new Vector2(0,1),Vector2.one,new Vector2(1,-2),new Vector2(-1,0),new Color(.50f,.57f,.60f));
            MetalLine(rect,"Lower shadow",Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,2),new Color(.025f,.035f,.04f));
            MetalLine(rect,"Left bevel",Vector2.zero,new Vector2(0,1),new Vector2(0,2),new Vector2(2,-2),new Color(.32f,.38f,.41f));
            MetalLine(rect,"Right bevel",new Vector2(1,0),Vector2.one,new Vector2(-2,2),new Vector2(0,-2),new Color(.035f,.045f,.055f));
            MetalLine(rect,"Recessed seam",Vector2.zero,new Vector2(1,0),new Vector2(9,5),new Vector2(-9,6),new Color(.035f,.055f,.065f));
            if(!button)
                MetalLine(rect,"Amber identification strip",new Vector2(0,1),new Vector2(0,1),new Vector2(14,-5),new Vector2(65,-3),new Color(.86f,.57f,.20f));
            for(int i=0;i<4;i++)
            {
                bool right=(i&1)!=0, top=(i&2)!=0;
                var obj=new GameObject("Slotted screw "+(i+1),typeof(RectTransform),typeof(ButtonScrew));
                obj.transform.SetParent(rect,false);
                var screw=obj.GetComponent<RectTransform>();
                screw.anchorMin=screw.anchorMax=new Vector2(right?1:0,top?1:0);
                screw.sizeDelta=Vector2.one*(button?5:8);
                float inset=button?5:7;
                screw.anchoredPosition=new Vector2(right?-inset:inset,top?-inset:inset);
                screw.localRotation=Quaternion.Euler(0,0,i%2==0?25:-25);
                var graphic=obj.GetComponent<ButtonScrew>();
                graphic.color=new Color(.60f,.65f,.67f,1);graphic.raycastTarget=false;
            }
#if UNITY_EDITOR
            if(!Application.isPlaying)UnityEditor.Undo.RegisterCreatedObjectUndo(root,"Add metal trim");
#endif
        }

        private static void MetalLine(Transform parent,string name,Vector2 min,Vector2 max,Vector2 low,Vector2 high,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=low;rect.offsetMax=high;
            var image=obj.GetComponent<Image>();image.color=color;image.raycastTarget=false;
        }

        private static void StyleMetalButton(Button button)
        {
            var colors=button.colors;
            colors.normalColor=Color.white;
            colors.highlightedColor=new Color(1.25f,1.35f,1.40f,1);
            colors.selectedColor=new Color(1.15f,1.28f,1.35f,1);
            colors.pressedColor=new Color(.80f,.72f,.55f,1);
            colors.disabledColor=new Color(.40f,.43f,.45f,1);
            colors.fadeDuration=.08f;
            button.colors=colors;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Robot Strategy/Apply Mechanical UI Theme")]
        private static void ApplyMechanicalUITheme()
        {
            if(Application.isPlaying){Debug.LogWarning("Stop Play mode before styling the UI.");return;}
            var controller=FindAnyObjectByType<BattleManager>();
            if(controller==null){Debug.LogWarning("Open MainScene first.");return;}
            if(controller.sceneUICanvas==null)CreateEditableBattleUI();
            if(controller.sceneUICanvas==null)return;
            UnityEditor.Undo.IncrementCurrentGroup();
            int group=UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName("Apply Mechanical UI Theme");
            UnityEditor.Undo.RegisterFullObjectHierarchyUndo(controller.sceneUICanvas.gameObject,"Style Battle UI");
            foreach(var tag in controller.sceneUICanvas.GetComponentsInChildren<ScreenPart>(true))
            {
                string key=tag.BindingKey;
                if(!key.StartsWith("Panel:")&&!key.StartsWith("Button:"))continue;
                bool hasTrim=tag.transform.Find("Metal Trim")!=null;
                controller.StyleMetalPanel(tag.GetComponent<RectTransform>(),key.StartsWith("Button:"));
                var button=tag.GetComponent<Button>();
                if(button!=null&&!hasTrim)StyleMetalButton(button);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            UnityEditor.Undo.CollapseUndoOperations(group);
            UnityEditor.Selection.activeGameObject=controller.sceneUICanvas.gameObject;
        }
#endif
    }
}
