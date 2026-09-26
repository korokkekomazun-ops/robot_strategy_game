using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RobotStrategy.Movement;

namespace RobotStrategy.Battle
{
    // 国と機体種類の選択、初期ポイント、機体能力を管理します。
    public partial class BattleManager
    {
        [Header("Nation bonuses / Initial points")]
        [SerializeField, Min(0)] private int nationACPBonus = 10000;
        [SerializeField, Min(0)] private int nationBPointBonus = 20;
        [SerializeField, Min(1)] private float nationDDamageMultiplier = 1.5f;
        [SerializeField, Min(0)] private int initialDesignPoints = 15;
        [SerializeField, Min(0)] private int initialStatPointCost = 1;
        [SerializeField, Min(0)] private int initialAptitudePointCost = 3;
        [Header("C nation giant prototype")]
        [SerializeField, Min(1)] private float giantStatMultiplier = 5;
        [SerializeField, Min(3)] private int giantCPMultiplier = 3;
        [Header("Chassis sprites (drag a Sprite here to replace artwork)")]
        [SerializeField, Tooltip("戦車の画像。空欄では仮の形状を表示します。")]
        private Sprite tankSprite;
        [SerializeField, Tooltip("戦闘機の画像。空欄ではRobot Spriteを使用します。")]
        private Sprite fighterSprite;
        [SerializeField, Tooltip("船の画像。空欄では仮の形状を表示します。")]
        private Sprite shipSprite;
        [SerializeField, Tooltip("トランスフォーマーの画像。空欄では仮の形状を表示します。")]
        private Sprite transformerSprite;
        [SerializeField, Tooltip("人型の画像。空欄では仮の形状を表示します。")]
        private Sprite humanoidSprite;
        [SerializeField, Tooltip("巨大機体の画像。空欄では仮の形状を表示します。")]
        private Sprite giantSprite;
        [SerializeField, Range(0,1), Tooltip("画像に陣営色を混ぜる強さ。0は元の色。HPバーと足元の所属色は常に表示します。再生開始時の生成に反映。")]
        private float chassisSpriteTintStrength = 0f;
        [Header("Chassis resistance / received damage multiplier")]
        [SerializeField, HideInInspector] private float tankDamageTakenMultiplier = 1f;
        [SerializeField, HideInInspector] private float fighterDamageTakenMultiplier = 1f;
        [SerializeField, HideInInspector] private float shipDamageTakenMultiplier = 1f;
        [SerializeField, HideInInspector] private float transformerDamageTakenMultiplier = 1f;
        [SerializeField, HideInInspector] private float humanoidDamageTakenMultiplier = 1f;
        [SerializeField, HideInInspector] private float giantDamageTakenMultiplier = 1f;
        private Nation playerNation = Nation.None;
        private ChassisKind selectedKind = ChassisKind.Tank;
        private bool battleStarted, initialSetup, editingExisting;
        private GameObject choiceOverlay, nationPanel, initialTypePanel, deploymentOverlay;
        private TextMeshProUGUI developmentTitle;
        private Button closeDevelopmentButton;
        private readonly Button[] kindButtons = new Button[6];
        private readonly Button[] initialKindButtons = new Button[6];
        private readonly Button[] designDeployButtons = new Button[8];
        private int deploymentPage;
        private bool AnyPanelOpen => (lanWaitOverlay!=null&&lanWaitOverlay.activeSelf) || finished || DevelopmentIsOpen || !battleStarted || (deploymentOverlay != null && deploymentOverlay.activeSelf);

        private RobotDesign BaseDesign(ChassisKind kind) => new RobotDesign(CountryRules.KindName(kind),new int[RobotDesign.ValueCount],kind,playerNation);
        private int PointBudget => CountryRules.InitialPoints(initialDesignPoints,playerNation,nationBPointBonus);
        private long AvailableBudget => initialSetup ? PointBudget : catalog.CP;
        private void BuildKindButtons(Transform panel)
        {
            for(int i=0;i<6;i++)
            {
                int index=i;
                kindButtons[i]=MakeButton(panel,CountryRules.KindName((ChassisKind)i),18+i*105,146,99,()=>SelectNewKind((ChassisKind)index));
            }
        }
        private void SelectNewKind(ChassisKind kind)
        {
            if(editingExisting||!CountryRules.CanChoose(playerNation,kind))return;
            selectedKind=kind;basis=BaseDesign(kind);draft=basis.CopyLevels();
            feedback="種類："+CountryRules.KindName(kind)+"。能力値を調整してください。";
        }
        private GameObject Overlay(Transform parent,string name)
        {
            var existing=FindUI(parent,"Overlay:"+name);
            if(existing!=null)return existing;
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            TagUI(obj,"Overlay:"+name);
            obj.GetComponent<Image>().color=new Color(0,0,0,.75f);return obj;
        }
        private void BuildNationFlow()
        {
            Transform canvas=developmentOverlay.transform.parent;
            choiceOverlay=Overlay(canvas,"国家と初期機体の選択");
            nationPanel=Panel(choiceOverlay.transform,"国家選択",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(740,520)).gameObject;
            Text(nationPanel.transform,"Title",20,20,700,50,28).text="2. 国家を選択";
            string[] descriptions={
                $"A国：開始CP +{nationACPBonus:N0}",
                $"B国：初期配分ポイント +{nationBPointBonus}",
                $"C国：巨大機体（HP・攻撃・装甲 {giantStatMultiplier:0.##}倍 ／ CP費用 {Mathf.Max(3,giantCPMultiplier)}倍）",
                $"D国：他国の機体への攻撃 {nationDDamageMultiplier:0.##}倍（怪獣は対象外）"};
            for(int i=0;i<4;i++)
            {
                int n=i+1;MakeButton(nationPanel.transform,descriptions[i],20,100+i*80,700,()=>ChooseNation((Nation)n));
            }
            Text(nationPanel.transform,"Foot",20,438,700,60,18).text="国家 → 初期機体の種類 → ポイント配分 → 戦闘\n選択中は戦闘・増援は始まりません。";
            initialTypePanel=Panel(choiceOverlay.transform,"初期機体選択",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(700,550)).gameObject;
            Text(initialTypePanel.transform,"Title",20,20,660,50,26).text="3. 主力機体の種類を選択";
            for(int i=0;i<6;i++)
            {
                int index=i;
                initialKindButtons[i]=MakeButton(initialTypePanel.transform,CountryRules.KindName((ChassisKind)i),20,86+i*60,660,()=>BeginInitialDesign((ChassisKind)index));
            }
            MakeButton(initialTypePanel.transform,"国家選択へ戻る",20,468,660,()=>ShowPreparationPage(PreparationPage.Nation));
            choiceOverlay.SetActive(true);nationPanel.SetActive(true);
            initialTypePanel.SetActive(false);

            deploymentOverlay=Overlay(canvas,"設計一覧から配備");
            var dp=Panel(deploymentOverlay.transform,"配備一覧",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(730,610));
            Text(dp,"Title",18,14,520,44,25).text="配備する設計を選択";
            MakeButton(dp,"閉じる",590,10,122,()=>deploymentOverlay.SetActive(false));
            for(int i=0;i<8;i++)
            {
                int row=i;designDeployButtons[i]=MakeButton(dp,"設計",18,70+i*56,694,()=>DeployFromList(row));
            }
            MakeButton(dp,"前",18,530,330,()=>deploymentPage=Mathf.Max(0,deploymentPage-1));
            MakeButton(dp,"次",360,530,352,()=>{if((deploymentPage+1)*8<catalog.Designs.Count)deploymentPage++;});
            deploymentOverlay.SetActive(false);
        }
        private void ChooseNation(Nation nation)
        {
            if(networkMode)SendCommand(new PlayerOrder{op="nation",nation=(int)nation});
            playerNation=nation;ShowPreparationPage(PreparationPage.Chassis);
            for(int i=0;i<6;i++)initialKindButtons[i].interactable=CountryRules.CanChoose(nation,(ChassisKind)i);
        }
        private void BeginInitialDesign(ChassisKind kind)
        {
            if(!CountryRules.CanChoose(playerNation,kind))return;
            initialSetup=true;editingExisting=false;selectedKind=kind;basis=BaseDesign(kind);draft=basis.CopyLevels();
            ShowPreparationPage(PreparationPage.Stats);
            RefreshBattleUI();feedback="初期ポイントで機体を作成します。CPは消費しません。";
        }
        private void StartBattleFromDesign()
        {
            if(countingDown||!initialSetup||DraftCost<0||DraftCost>PointBudget)return;
            var design=new RobotDesign(playerNation+"国 初期"+CountryRules.KindName(selectedKind),draft,selectedKind,playerNation);
            ConfirmInitialDesign(design);
        }
        private void OpenDeploymentList()
        {
            if(!battleStarted||finished)return;
            deploymentOverlay.SetActive(true);RefreshNationUI();
        }
        private void DeployFromList(int row)
        {
            int index=deploymentPage*8+row;if(index>=catalog.Designs.Count||finished)return;
            chosenDesign=index;
            var before=selected;DeployChosen();
            if(selected!=before)
            {
                deploymentOverlay.SetActive(false);
                mapCamera.transform.position=new Vector3(selected.transform.position.x,selected.transform.position.y,-10);
            }
        }
        private void RefreshNationUI()
        {
            if(deploymentOverlay!=null&&deploymentOverlay.activeSelf)
            {
                for(int i=0;i<8;i++)
                {
                    int index=deploymentPage*8+i;var button=designDeployButtons[i];button.gameObject.SetActive(index<catalog.Designs.Count);
                    if(index<catalog.Designs.Count)
                    {
                        var d=catalog.Designs[index];button.GetComponentInChildren<TextMeshProUGUI>().text=$"{d.Name}　[{CountryRules.KindName(d.Kind)}]　{DeploymentCost(d.Kind):N0} CPで配備";
                        button.interactable=!finished&&catalog.CP>=DeploymentCost(d.Kind);
                    }
                }
            }
        }
        private Sprite SpriteFor(ChassisKind kind)
        {
            var sprites=new[]{tankSprite,fighterSprite,shipSprite,transformerSprite,humanoidSprite,giantSprite};
            return sprites[(int)kind]!=null?sprites[(int)kind]:(kind==ChassisKind.Fighter?robotSprite:null);
        }
        private UnitSettings StatsFor(RobotDesign design)
        {
            bool giant=design.Kind==ChassisKind.Giant;
            var basic=BalanceFor(design.Kind).stats;
            return new UnitSettings{
                maxHP=ScaledStat(SafeStat(basic.maxHP,design[0],hpPerLevel),giant?giantStatMultiplier:1),
                attack=ScaledStat(SafeStat(basic.attack,design[1],attackPerLevel),giant?giantStatMultiplier:1),
                armour=ScaledStat(SafeStat(basic.armour,design[2],armourPerLevel),giant?giantStatMultiplier:1),
                moveSpeed=StatusMath.Speed(basic.moveSpeed,design[3],speedPerLevel,giant?giantSpeedMultiplier:1),
                damageTakenMultiplier=basic.damageTakenMultiplier,
                attackRange=basic.attackRange,attackInterval=basic.attackInterval};
        }
        private static int ScaledStat(int value,float multiplier)=>(int)System.Math.Min(int.MaxValue,(double)value*System.Math.Max(0,multiplier));
        private int ChassisCost(int normal,ChassisKind kind) => CountryRules.CPCost(normal,kind,giantCPMultiplier);
        private int DeploymentCost(ChassisKind kind) => ChassisCost(unitDeploymentCP,kind);
        private int StatCost => initialSetup?Mathf.Max(0,initialStatPointCost):ChassisCost(developmentStatCost,basis.Kind);
        private int AptitudeCost => initialSetup?Mathf.Max(0,initialAptitudePointCost):ChassisCost(developmentAptitudeCost,basis.Kind);
    }
}

