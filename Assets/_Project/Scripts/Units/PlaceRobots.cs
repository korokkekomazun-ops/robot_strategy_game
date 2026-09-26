using System.Collections.Generic;
using UnityEngine;

namespace RobotStrategy.Battle
{
    // 四隅への配置、所属色、撃破報酬を管理します。
    public partial class BattleManager
    {
        [Header("Four nations / corner deployment")]
        [SerializeField, Min(2)] private float cornerInset = 5;
        [SerializeField] private bool npcAutoMove = true;
        [SerializeField] private Color nationAColor = new Color(.25f,.65f,1);
        [SerializeField] private Color nationBColor = new Color(1,.32f,.28f);
        [SerializeField] private Color nationCColor = new Color(.35f,.9f,.45f);
        [SerializeField] private Color nationDColor = new Color(1,.78f,.2f);
        private readonly Dictionary<Nation, DesignList> nationCatalogs = new Dictionary<Nation, DesignList>();

        private Color NationColor(Nation nation)
        {
            switch(nation)
            {
                case Nation.A: return nationAColor;
                case Nation.B: return nationBColor;
                case Nation.C: return nationCColor;
                case Nation.D: return nationDColor;
                default: return Color.white;
            }
        }

        private void DeployFourNations(RobotDesign playerDesign)
        {
            float edge=Mathf.Max(1,halfSize-Mathf.Clamp(cornerInset,2,halfSize-1));
            Vector3[] corners={new Vector3(-edge,-edge),new Vector3(edge,-edge),new Vector3(-edge,edge),new Vector3(edge,edge)};
            for(int i=0;i<4;i++)
            {
                Nation nation=(Nation)(i+1);
                RobotDesign design=nation==playerNation?playerDesign:
                    new RobotDesign(nation+"国 NPC初期機体",new int[RobotDesign.ValueCount],
                        nation==Nation.A?ChassisKind.Tank:nation==Nation.B?ChassisKind.Fighter:
                        nation==Nation.C?ChassisKind.Giant:ChassisKind.Transformer,nation);
                var designs=nation==playerNation?catalog:new DesignList(CountryRules.InitialCP(startingBattleCP,nation,nationACPBonus),design);
                nationCatalogs[nation]=designs;
                var home=SpawnUnit(nation+"国 拠点",nation==playerNation?BattleTeam.Player:BattleTeam.Enemy,true,whiteSprite,corners[i],baseStats,1.5f);
                home.SetNation(nation);home.SetOwnership((int)nation-1,nation==playerNation);ApplyNationColor(home);bases.Add(home);
                int count=Mathf.Clamp(initialRobotCount,1,Mathf.Max(1,maxPlayerRobots));
                for(int n=0;n<count;n++)
                    if(FindNationDeploymentPosition(nation,out Vector3 position))
                    {
                        var unit=SpawnNationDesign(design,position);
                        if(nation==playerNation)selected=unit;
                    }
                if(nation==playerNation)mapCamera.transform.position=new Vector3(corners[i].x,corners[i].y,-10);
            }
        }

        private bool FindNationDeploymentPosition(Nation nation,out Vector3 position,int owner=-1)
        {
            if(owner<0)owner=networkMode?localSeat:(int)nation-1;
            position=Vector3.zero;
            if(units.FindAll(u=>u!=null&&u.Alive&&!u.IsBase&&u.OwnerSlot==owner).Count>=Mathf.Max(1,maxPlayerRobots))return false;
            foreach(var home in bases)
            {
                if(home==null||!home.Alive||home.OwnerSlot!=owner)continue;
                for(int ring=2;ring<=4;ring++)for(int i=0;i<16;i++)
                {
                    float angle=i*Mathf.PI*2/16;
                    Vector3 p=home.transform.position+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*ring;
                    if(!terrainMap.TryGetTerrain(p,out _))continue;
                    if(units.Exists(u=>u!=null&&u.Alive&&Vector3.Distance(u.transform.position,p)<1.2f))continue;
                    position=p;return true;
                }
            }
            return false;
        }

        private BattleUnit SpawnNationDesign(RobotDesign design,Vector3 position,int owner=-1)
        {
            if(owner<0)owner=networkMode?localSeat:(int)design.Country-1;
            var unit=SpawnUnit(networkMode?"P"+(owner+1)+" "+design.Country+"国 / "+design.Name:design.Name,design.Country==playerNation?BattleTeam.Player:BattleTeam.Enemy,false,
                SpriteFor(design.Kind),position,StatsFor(design),design.Kind==ChassisKind.Giant?2.4f:1.2f);
            if(SpriteFor(design.Kind)==null)BuildChassisPlaceholder(unit,design.Kind);
            unit.SetNation(design.Country);unit.SetDesign(design);unit.SetOwnership(owner,networkMode?owner==localSeat:design.Country==playerNation);
            unit.Movement.flying=design.Kind==ChassisKind.Fighter;
            unit.Movement.profile.ground=(RobotStrategy.Movement.TerrainAptitude)design[4];
            unit.Movement.profile.sea=(RobotStrategy.Movement.TerrainAptitude)design[5];
            unit.Movement.profile.sky=(RobotStrategy.Movement.TerrainAptitude)design[6];
            ApplyMovementBalance(unit);
            ApplyNationColor(unit);
            return unit;
        }

        private void ApplyNationColor(BattleUnit unit)
        {
            Color tint=NationColor(networkMode?(Nation)(unit.OwnerSlot+1):unit.Country);
            // Keep the supplied pixel art's palette; markers still identify all four players.
            bool hasChassisSprite=unit.Design!=null&&SpriteFor(unit.Design.Kind)!=null;
            float artworkTint=hasChassisSprite?Mathf.Clamp01(chassisSpriteTintStrength):.65f;
            foreach(var renderer in unit.transform.Find("Artwork").GetComponentsInChildren<SpriteRenderer>())
                renderer.color=Color.Lerp(renderer.color,tint,artworkTint);
            var hp=unit.transform.Find("HP Background/HP Pivot/HP");
            if(hp!=null)hp.GetComponent<SpriteRenderer>().color=tint;
            var marker=new GameObject("Nation marker - "+unit.Country,typeof(SpriteRenderer));
            marker.transform.SetParent(unit.transform,false);
            marker.transform.localPosition=new Vector3(0,-.75f,0);
            marker.transform.localScale=new Vector3(1.1f,.12f,1);
            var sr=marker.GetComponent<SpriteRenderer>();sr.sprite=whiteSprite;sr.color=tint;sr.sortingOrder=25;
        }

        private static bool IsHostile(BattleUnit source,BattleUnit target)
            => source.OwnerSlot!=target.OwnerSlot;

        private void AwardDefeat(BattleUnit attacker,BattleUnit defeated)
        {
            DesignList winner;
            if(networkMode)
            {
                var owner=System.Linq.Enumerable.FirstOrDefault(peers.Values,p=>p.slot==attacker.OwnerSlot);
                if(owner==null)return;winner=owner.designs;
            }
            else if(!nationCatalogs.TryGetValue(attacker.Country,out winner))return;
            if(defeated.Country==Nation.None && defeated!=boss)winner.Earn(enemyDefeatCP);
            if(defeated.Design==null || defeated.OwnerSlot==attacker.OwnerSlot)return;
            bool acquired=winner.Capture(defeated.Design,attacker.Country);
            if(networkMode)
            {
                if(acquired)
                {
                    var attackerSeat=System.Linq.Enumerable.FirstOrDefault(peers.Values,p=>p.slot==attacker.OwnerSlot);
                    var defeatedSeat=System.Linq.Enumerable.FirstOrDefault(peers.Values,p=>p.slot==defeated.OwnerSlot);
                    if(attackerSeat!=null)attackerSeat.report="設計図を獲得："+defeated.Design.Name;
                    if(defeatedSeat!=null)defeatedSeat.report="P"+(attacker.OwnerSlot+1)+"に「"+defeated.Design.Name+"」の設計図を奪われました。";
                }
                return;
            }
            if(acquired && attacker.Country==playerNation)
                feedback="設計図を獲得："+defeated.Design.Name+"（配備一覧から生産できます）";
            else if(acquired && defeated.Country==playerNation)
                feedback=attacker.Country+"国に「"+defeated.Design.Name+"」の設計図を奪われました。";
        }
    }
}
