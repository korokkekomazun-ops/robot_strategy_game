using UnityEngine;
using RobotStrategy.Movement;

namespace RobotStrategy.Battle
{
    // 機体ごとの基本能力・地形適性・攻撃相性を管理します。
    [System.Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RobotStrategy.Prototype", null, "ChassisBalance")]
    public class RobotStatus
    {
        public UnitSettings stats = new UnitSettings();
        public TerrainAptitude ground = TerrainAptitude.C;
        public TerrainAptitude sea = TerrainAptitude.C;
        public TerrainAptitude sky = TerrainAptitude.C;
        public TerrainAptitude sand = TerrainAptitude.B;
    }

    [System.Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RobotStrategy.Prototype", null, "ChassisMatchup")]
    public class AttackMatch
    {
        public ChassisKind attacker;
        public ChassisKind defender;
        [Min(0)] public float attackMultiplier = 1f;
    }

    public partial class BattleManager
    {
        [Header("Individual chassis balance")]
        [SerializeField] private RobotStatus tankBalance = new RobotStatus();
        [SerializeField] private RobotStatus fighterBalance = new RobotStatus();
        [SerializeField] private RobotStatus shipBalance = new RobotStatus();
        [SerializeField] private RobotStatus transformerBalance = new RobotStatus();
        [SerializeField] private RobotStatus humanoidBalance = new RobotStatus();
        [SerializeField] private RobotStatus giantBalance = new RobotStatus();
        [Header("Movement aptitude multipliers")]
        [SerializeField, Min(0)] private float terrainCMultiplier = .5f;
        [SerializeField, Min(0)] private float terrainBMultiplier = 1f;
        [SerializeField, Min(0)] private float terrainAMultiplier = 1.5f;
        [SerializeField, Min(0)] private float giantSpeedMultiplier = 1f;
        [SerializeField] private AttackMatch[] chassisMatchups = new AttackMatch[0];

        private int MatchupAttack(BattleUnit attacker,BattleUnit defender,int attack)
        {
            if(attacker.Design==null||defender.Design==null)return attack;
            foreach(var rule in chassisMatchups)
                if(rule!=null&&rule.attacker==attacker.Design.Kind&&rule.defender==defender.Design.Kind)
                    return BattleDamage.Calculate(attack,0,rule.attackMultiplier);
            return attack;
        }

        private int MaxAptitudeUpgrade(ChassisKind kind,int index)
        {
            var settings=BalanceFor(kind);
            return 2-Mathf.Clamp((int)(index==4?settings.ground:index==5?settings.sea:index==6?settings.sky:settings.sand),0,2);
        }

        private RobotStatus BalanceFor(ChassisKind kind)
        {
            switch(kind)
            {
                case ChassisKind.Tank:return tankBalance;
                case ChassisKind.Fighter:return fighterBalance;
                case ChassisKind.Ship:return shipBalance;
                case ChassisKind.Transformer:return transformerBalance;
                case ChassisKind.Giant:return giantBalance;
                default:return humanoidBalance;
            }
        }

        private int EffectiveAptitude(RobotDesign design,int index)
        {
            var settings=BalanceFor(design.Kind);
            int baseRank=(int)(index==4?settings.ground:index==5?settings.sea:index==6?settings.sky:settings.sand);
            return Mathf.Clamp(baseRank+design[index],0,2);
        }

        private void ApplyMovementBalance(BattleUnit unit)
        {
            if(unit.Movement==null)return;
            var profile=unit.Movement.profile;
            profile.cMultiplier=Mathf.Max(0,terrainCMultiplier);
            profile.bMultiplier=Mathf.Max(0,terrainBMultiplier);
            profile.aMultiplier=Mathf.Max(0,terrainAMultiplier);
            if(unit.Design==null)return;
            profile.ground=(TerrainAptitude)EffectiveAptitude(unit.Design,4);
            profile.sea=(TerrainAptitude)EffectiveAptitude(unit.Design,5);
            profile.sky=(TerrainAptitude)EffectiveAptitude(unit.Design,6);
            profile.sand=(TerrainAptitude)EffectiveAptitude(unit.Design,7);
        }

        public void ApplyBalanceToLivingUnits()
        {
            if(!Application.isPlaying||(networkMode&&!net.IsServer))return;
            foreach(var unit in units)
            {
                if(unit==null||!unit.Alive)continue;
                var settings=unit.IsBase?baseStats:unit==boss?bossStats:
                    unit.Design!=null?StatsFor(unit.Design):smallEnemyStats;
                unit.RefreshStats(settings);
                ApplyMovementBalance(unit);
            }
            feedback="現在の設定を生存中の機体へ反映しました。HPの割合は維持します。";
        }
    }
}
