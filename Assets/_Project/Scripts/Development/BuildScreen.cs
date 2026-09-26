using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using RobotStrategy.Movement;

namespace RobotStrategy.Battle
{
    // 戦闘中の開発画面と設計・配備操作を管理します。
    public partial class BattleManager
    {
        [Header("Battle Development / CP")]
        [SerializeField, Min(0)] private int startingBattleCP = 7500;
        [SerializeField, Min(0)] private int developmentStatCost = 500;
        [SerializeField, Min(0)] private int developmentAptitudeCost = 500;
        [SerializeField, Min(0)] private int unitDeploymentCP = 600;
        [SerializeField, Min(0)] private int enemyDefeatCP = 100;
        [SerializeField, Min(1)] private int maxPlayerRobots = 30;
        [Header("Design: gain per level")]
        [SerializeField, Min(0)] private int hpPerLevel = 10;
        [SerializeField, Min(0)] private int attackPerLevel = 2;
        [SerializeField, Min(0)] private int armourPerLevel = 1;
        [SerializeField, Min(0)] private float speedPerLevel = 0.5f;

        [Header("UI readability")]
        [SerializeField, Range(12, 20)] private int minimumUIFontSize = 16;
        [SerializeField] private bool pixelPerfectUI = false;

        private DesignList catalog;
        private GameObject developmentOverlay;
        private TextMeshProUGUI cpLabel, designLabel, draftLabel, messageLabel, battleInfo;
        private readonly TextMeshProUGUI[] valueLabels = new TextMeshProUGUI[RobotDesign.ValueCount];
        private readonly Button[] plusButtons = new Button[RobotDesign.ValueCount];
        private readonly Button[] minusButtons = new Button[RobotDesign.ValueCount];
        private readonly Button[] rosterButtons = new Button[7];
        private readonly TextMeshProUGUI[] rosterLabels = new TextMeshProUGUI[7];
        private readonly List<BattleUnit> roster = new List<BattleUnit>();
        private Button confirmButton, deployButton, newButton, modifyButton;
        private RobotDesign basis;
        private int[] draft;
        private int chosenDesign, rosterPage;
        private string feedback = "開発で設計を作成できます。味方を選択し、地面を左クリックで移動。";
        private bool DevelopmentIsOpen => developmentOverlay != null && developmentOverlay.activeSelf;

        private bool PointerOnBattleUI()
        {
            if (EventSystem.current == null || Mouse.current == null) return false;
            // Explicit raycast avoids a one-frame lag from this controller's early Update order.
            var data = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            return hits.Exists(hit => hit.module is GraphicRaycaster);
        }

        private void BuildBattleUI()
        {
            catalog = new DesignList(startingBattleCP);
            if (Application.isPlaying && EventSystem.current == null)
                new GameObject("Battle EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            else if (Application.isPlaying)
            {
                var old = EventSystem.current.GetComponent<StandaloneInputModule>();
                if (old != null) old.enabled = false;
                if (EventSystem.current.GetComponent<InputSystemUIInputModule>() == null)
                    EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            var canvas = sceneUICanvas != null ? sceneUICanvas.gameObject : null;
            if (canvas == null)
            {
                canvas = new GameObject("Battle UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas.transform.SetParent(transform, false);
                sceneUICanvas = canvas.GetComponent<Canvas>();
                sceneUICanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                sceneUICanvas.sortingOrder = 100;
                sceneUICanvas.pixelPerfect = pixelPerfectUI;
                var scaler = canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.matchWidthOrHeight = 0.5f;
            }
            canvas.SetActive(true);
            var top = Panel(canvas.transform, "開発・配備", new Vector2(0,1), new Vector2(0,1), Vector2.zero, new Vector2(760,64));
            MakeButton(top, "開発", 12, 10, 120, () => { OpenDraft(false); });
            deployButton = MakeButton(top, "配備", 140, 10, 120, OpenDeploymentList);
            cpLabel = Text(top, "CP", 278, 4, 470, 56, 20);

            var side = Panel(canvas.transform, "機体一覧", new Vector2(1,1), new Vector2(1,1), new Vector2(-12,-76), new Vector2(265,490));
            Text(side,"title",12,10,240,32,22).text="配備済み機体";
            for (int i=0;i<rosterButtons.Length;i++)
            {
                int row=i;
                rosterButtons[i]=MakeButton(side,"機体",12,54+i*52,241,()=>SelectRoster(row));
                rosterLabels[i]=rosterButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                // Font size is authored on the label and retained on Play.
            }
            MakeButton(side,"前",12,432,110,()=>{rosterPage=Mathf.Max(0,rosterPage-1);});
            MakeButton(side,"次",135,432,118,()=>{if((rosterPage+1)*7<roster.Count)rosterPage++;});
            var bottom=Panel(canvas.transform,"戦況",new Vector2(0,0),new Vector2(0,0),new Vector2(12,12),new Vector2(860,116));
            battleInfo=Text(bottom,"BattleInfo",12,8,830,98,17);

            developmentOverlay=Overlay(canvas.transform,"Development Overlay");
            var panel=Panel(developmentOverlay.transform,"開発画面",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(660,650));
            developmentTitle=Text(panel,"Title",18,12,500,30,24);
            closeDevelopmentButton=MakeButton(panel,"閉じる",536,10,106,CloseDraft);
            MakeButton(panel,"＜",18,54,48,()=>ChooseDesign(-1));
            designLabel=Text(panel,"Design",78,60,484,32,20);
            MakeButton(panel,"＞",582,54,60,()=>ChooseDesign(1));
            newButton=MakeButton(panel,"新規作成",18,102,300,()=>OpenDraft(false));
            modifyButton=MakeButton(panel,"選択設計を改造",330,102,312,()=>OpenDraft(true));
            BuildKindButtons(panel);
            string[] captions={"最大HP","攻撃","装甲","速度","陸の適性","海の適性","空の適性"};
            for(int i=0;i<7;i++)
            {
                int index=i;
                Text(panel,captions[i],20,204+i*43,175,36,20).text=(i==4?"森・陸の適性":captions[i]);
                minusButtons[i]=MakeButton(panel,"－",204,196+i*43,48,()=>ChangeDraft(index,-1));
                valueLabels[i]=Text(panel,"value",270,204+i*43,285,35,19);
                plusButtons[i]=MakeButton(panel,"＋",588,196+i*43,54,()=>ChangeDraft(index,1));
            }
            var sandPanel=Panel(developmentOverlay.transform,"砂地の適性設定",Vector2.one*.5f,Vector2.one*.5f,new Vector2(-475,-175),new Vector2(260,150));
            Text(sandPanel,"Title",16,12,228,34,22).text="砂地の適性";
            minusButtons[7]=MakeButton(sandPanel,"－",16,62,48,()=>ChangeDraft(7,-1));
            valueLabels[7]=Text(sandPanel,"SandRank",88,64,84,36,24);
            plusButtons[7]=MakeButton(sandPanel,"＋",196,62,48,()=>ChangeDraft(7,1));
            Text(sandPanel,"Hint",16,108,228,30,15).text="基礎適性＋ポイント強化";
            draftLabel=Text(panel,"Cost",18,505,622,42,20);
            messageLabel=Text(panel,"Feedback",18,552,622,30,16);
            confirmButton=MakeButton(panel,"確定して試作機1体を配備",18,598,408,ConfirmDraft);
            MakeButton(panel,"キャンセル",438,598,204,CloseDraft);
            developmentOverlay.SetActive(false);
        }

        private void OpenDraft(bool modify)
        {
            if(finished||!battleStarted)return;
            editingExisting=modify;
            basis=modify?catalog.Designs[chosenDesign]:BaseDesign(selectedKind);
            selectedKind=basis.Kind;
            draft=basis.CopyLevels();
            feedback=modify?"既存の配備済み機体は変更せず、新しい設計を作ります。":"基本能力から新しい設計を作ります。";
            developmentOverlay.SetActive(true);
            developmentOverlay.transform.SetAsLastSibling();
            var panel=FindUI(developmentOverlay.transform,PositionKey("Panel:開発画面",0,0));
            if(panel!=null)panel.SetActive(true);
        }
        private void CloseDraft(){developmentOverlay.SetActive(false);draft=null;basis=null;if(initialSetup){initialSetup=false;ShowPreparationPage(PreparationPage.Chassis);}}
        private void ChooseDesign(int delta)
        {
            if(initialSetup)return;
            chosenDesign=(chosenDesign+delta+catalog.Designs.Count)%catalog.Designs.Count;
            feedback="対象設計を選択しました。「改造」で編集、「閉じる」→「配備」で追加生産できます。";
        }
        private long DraftCost => DesignList.Cost(basis,draft,StatCost,AptitudeCost);
        private void ChangeDraft(int index,int delta)
        {
            if(finished||draft==null)return;
            int next=draft[index]+delta;
            if(next<basis[index]||next>(index>=4?MaxAptitudeUpgrade(basis.Kind,index):10000))return;
            int old=draft[index];draft[index]=next;
            if(DraftCost>AvailableBudget){draft[index]=old;feedback=initialSetup?"ポイントが不足しています。":"CPが不足しています。";return;}
            feedback=initialSetup?"初期ポイントを配分中。CPは消費しません。":"調整中。CPは確定時に消費します。";
        }
        private bool FindDeploymentPosition(out Vector3 position)
        {
            if(FindNationDeploymentPosition(playerNation,out position))return true;
            feedback="自国の配備上限、または拠点周辺の空きを確認してください。";
            return false;
        }

        private void ConfirmDraft()
        {
            if(finished||draft==null)return;
            if(initialSetup){StartBattleFromDesign();return;}
            if(networkMode)
            {
                SendCommand(new PlayerOrder{op="develop",kind=(int)basis.Kind,designId=editingExisting?basis.DesignId:null,levels=(int[])draft.Clone()});
                CloseDraft();return;
            }
            if(!editingExisting&&!CountryRules.CanChoose(playerNation,basis.Kind))return;
            if(!FindDeploymentPosition(out Vector3 position))return;
            if(!catalog.TryCreate(basis,draft,StatCost,AptitudeCost,out RobotDesign design))
            {feedback="1段階以上変更し、必要CPを用意してください。";return;}
            chosenDesign=catalog.Designs.Count-1;
            DeployDesign(design,position);
            CloseDraft(); feedback=design.Name+"を開発し、試作機を配備しました。";
        }
        private void DeployChosen()
        {
            if(networkMode){SendCommand(new PlayerOrder{op="deploy",designId=catalog.Designs[chosenDesign].DesignId});deploymentOverlay.SetActive(false);return;}
            if(finished)return;
            if(!FindDeploymentPosition(out Vector3 position))return;
            if(!catalog.TrySpend(DeploymentCost(catalog.Designs[chosenDesign].Kind))){feedback="追加配備のCPが不足しています。";return;}
            DeployDesign(catalog.Designs[chosenDesign],position);
            feedback=catalog.Designs[chosenDesign].Name+"を追加配備しました。";
        }
        private void DeployDesign(RobotDesign design,Vector3 position)
        {
            selected=SpawnNationDesign(design,position);
        }

        private static int SafeStat(int value,int level,int gain)=>(int)System.Math.Min(int.MaxValue,(long)System.Math.Max(0,value)+(long)level*System.Math.Max(0,gain));
        private void SelectRoster(int row)
        {
            int index=rosterPage*7+row;
            if(index>=roster.Count)return;
            selected=roster[index];mapCamera.transform.position=new Vector3(selected.transform.position.x,selected.transform.position.y,-10);
        }
        private void RefreshBattleUI()
        {
            RefreshNationUI();
            cpLabel.text=(networkMode&&localSeat>=0?$"P{localSeat+1}（{PlayerSeats.Corner(localSeat)}） ":"")+$"{playerNation}国　CP {catalog.CP:N0}　配備費 {DeploymentCost(catalog.Designs[chosenDesign].Kind):N0}\n配備する設計：{catalog.Designs[chosenDesign].Name}";
            deployButton.interactable=battleStarted&&!finished;
            roster.Clear();
            foreach(var u in units)if(u!=null&&u.Alive&&u.Team==BattleTeam.Player&&!u.IsBase)roster.Add(u);
            rosterPage=Mathf.Clamp(rosterPage,0,Mathf.Max(0,(roster.Count-1)/7));
            for(int i=0;i<7;i++)
            {
                int index=rosterPage*7+i;rosterButtons[i].gameObject.SetActive(index<roster.Count);
                if(index<roster.Count){var u=roster[index];rosterLabels[i].text=$"{(u==selected?"▶ ":"")}{u.name}　HP {u.HP}/{u.MaxHP}";}
            }
            string state=selected!=null&&selected.Alive?$"{selected.name}：HP {selected.HP}/{selected.MaxHP}　攻撃 {selected.Attack}　装甲 {selected.Armour}":"左クリックで機体選択、地面を左クリックで移動。WASD・中ドラッグでカメラ移動。";
            battleInfo.text=(!battleStarted?"戦闘準備中":finished?result:$"怪獣HP {(boss!=null?boss.HP:0)}/{(boss!=null?boss.MaxHP:0)}　次の増援 {Mathf.CeilToInt(Mathf.Max(0,nextSpawn-Time.time))}秒")+"\n"+state+"\n"+feedback;
            if(!DevelopmentIsOpen||draft==null||basis==null)return;
            designLabel.text="対象："+catalog.Designs[chosenDesign].Name;
            newButton.gameObject.SetActive(!initialSetup);modifyButton.gameObject.SetActive(!initialSetup);
            newButton.interactable=modifyButton.interactable=!finished;
            developmentTitle.text=initialSetup?"4. 主力機体のステータス設定":playerNation+"国の開発（戦闘継続中）";
            closeDevelopmentButton.GetComponentInChildren<TextMeshProUGUI>().text=initialSetup?"戻る":"閉じる";
            for(int k=0;k<6;k++)kindButtons[k].interactable=!finished&&!editingExisting&&CountryRules.CanChoose(playerNation,(ChassisKind)k);
            var preview=StatsFor(new RobotDesign("preview",draft,basis.Kind,playerNation));
            long cost=DraftCost;
            bool changed=false;
            for(int i=0;i<RobotDesign.ValueCount;i++)
            {
                changed|=draft[i]!=basis[i];
                minusButtons[i].interactable=!finished&&draft[i]>basis[i];
                long price=i<4?StatCost:AptitudeCost;
                plusButtons[i].interactable=!finished&&draft[i]<(i>=4?MaxAptitudeUpgrade(basis.Kind,i):10000)&&cost+price<=AvailableBudget;
                if(i>=4)valueLabels[i].text=((char)('C'-EffectiveAptitude(new RobotDesign("preview",draft,basis.Kind),i))).ToString();
                else
                {
                    string value=i==0?preview.maxHP.ToString():i==1?preview.attack.ToString():i==2?preview.armour.ToString():preview.moveSpeed.ToString("0.##");
                    valueLabels[i].text=$"{value} （強化 +{draft[i]}）";
                }
            }
            draftLabel.text=initialSetup?$"{playerNation}国 {CountryRules.KindName(basis.Kind)}　残り {PointBudget-cost} / {PointBudget} ポイント":$"編集元：{basis.Name}　必要 {cost:N0} CP ／ 所持 {catalog.CP:N0}";
            messageLabel.text=finished?"戦闘が終了しました。":feedback;
            confirmButton.GetComponentInChildren<TextMeshProUGUI>().text=initialSetup?"この機体で出撃（カウントダウン開始）":"確定して試作機1体を配備";
            confirmButton.interactable=!finished&&(initialSetup||changed)&&cost>=0&&cost<=AvailableBudget;
        }

        private RectTransform Panel(Transform parent,string title,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size,string key=null)
        {
            key = key ?? PositionKey("Panel:"+title,pos.x,pos.y);
            var existing=FindUI(parent,key);
            if(existing!=null)return existing.GetComponent<RectTransform>();
            var obj=new GameObject(title,typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;rect.anchoredPosition=pos;rect.sizeDelta=size;
            TagUI(obj,key);
            StyleMetalPanel(rect,key.StartsWith("Button:"));return rect;
        }
        private TextMeshProUGUI Text(Transform parent,string name,float x,float y,float width,float height,int size)
        {
            string key=PositionKey("Text:"+name,x,y);
            var existing=FindUI(parent,key);
            if(existing!=null)return existing.GetComponent<TextMeshProUGUI>();
            var obj=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
            var text=obj.GetComponent<TextMeshProUGUI>();if(uiFont!=null)text.font=uiFont;
            text.fontSize=size;text.color=Color.white;text.raycastTarget=false;text.enableAutoSizing=true;text.fontSizeMin=Mathf.Min(size, minimumUIFontSize);text.fontSizeMax=size;
            text.extraPadding=true;text.isOrthographic=true;TagUI(obj,key);return text;
        }
        private Button MakeButton(Transform parent,string caption,float x,float y,float width,UnityAction action)
        {
            var rect=Panel(parent,caption,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-y),new Vector2(width,38),PositionKey("Button",x,y));
            var button=rect.GetComponent<Button>();
            bool created=button==null;
            if(created)
            {

                button=rect.gameObject.AddComponent<Button>();
                button.targetGraphic=rect.GetComponent<Image>();
                StyleMetalButton(button);
            }
            if(Application.isPlaying)button.onClick.AddListener(action);
            var label=Text(rect,"Label",5,2,width-10,34,20);
            label.text=caption;
            if(created)label.alignment=TextAlignmentOptions.Center;
            return button;
        }
    }
}


