using UnityEngine;
using RobotStrategy.Movement;

namespace RobotStrategy.Battle
{
    // 戦場にいる1体のHP・攻撃・所有者を管理します。
    public enum BattleTeam { Player, Enemy }

    [System.Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RobotStrategy.Prototype", null, "PrototypeUnitSettings")]
    public class UnitSettings
    {
        [Min(1)] public int maxHP = 100;
        [Min(0)] public int attack = 10;
        [Min(0)] public int armour;
        [Min(0), Tooltip("Received damage multiplier after armour. 0.25 = 75% reduction, 1 = normal.")]
        public float damageTakenMultiplier = 1f;
        [Min(0)] public float moveSpeed = 3;
        [Min(0.1f)] public float attackRange = 2.5f;
        [Min(0.1f)] public float attackInterval = 1;
    }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RobotStrategy.Prototype", null, "PrototypeCombatUnit")]
    public class BattleUnit : MonoBehaviour
    {
        [SerializeField] private Nation ownerNation = Nation.None;
        public Nation Country => ownerNation;
        public int OwnerSlot { get; private set; } = -1;
        public int NetworkId { get; set; }
        public int AttackSerial { get; set; }
        public Vector3 LastHit { get; set; }
        public void SetOwnership(int slot,bool local){OwnerSlot=slot;Team=local?BattleTeam.Player:BattleTeam.Enemy;}
        public UnitSettings CurrentSettings()=>new UnitSettings{maxHP=MaxHP,attack=Attack,armour=Armour,
            damageTakenMultiplier=DamageTakenMultiplier,moveSpeed=Movement!=null?Movement.profile.baseSpeed:0,attackRange=Range,attackInterval=Interval};
        private bool replica;
        private Vector3 replicaFrom,replicaTarget;
        private float replicaTime;
        private void Update()
        {
            if(replica)transform.position=Vector3.Lerp(replicaFrom,replicaTarget,(Time.unscaledTime-replicaTime)/.1f);
        }
        public void ApplyReplica(UnitSettings stats,int hp,Vector3 position)
        {
            RefreshStats(stats);HP=Mathf.Clamp(hp,0,MaxHP);
            replicaFrom=replica?transform.position:position;replicaTarget=position;replicaTime=Time.unscaledTime;replica=true;
            if(healthFill!=null)healthFill.localScale=new Vector3((float)HP/MaxHP,1,1);
        }
        public RobotDesign Design { get; private set; }
        public void SetDesign(RobotDesign design) => Design = design;
        public void SetNation(Nation nation) => ownerNation = nation;
        public BattleTeam Team { get; private set; }
        public bool IsBase { get; private set; }
        public int HP { get; private set; }
        public int MaxHP { get; private set; }
        public int Attack { get; private set; }
        public int Armour { get; private set; }
        public float DamageTakenMultiplier { get; private set; }
        public float Range { get; private set; }
        public float Interval { get; private set; }
        public float NextAttackTime { get; set; }
        public bool Alive => HP > 0;
        public RobotTerrainMovement Movement { get; private set; }
        private Transform healthFill;

        public void Configure(BattleTeam team, bool isBase, UnitSettings settings,
            RobotTerrainMovement movement, Transform fill)
        {
            Team = team;
            IsBase = isBase;
            MaxHP = Mathf.Max(1, settings.maxHP);
            HP = MaxHP;
            Attack = Mathf.Max(0, settings.attack);
            Armour = Mathf.Max(0, settings.armour);
            DamageTakenMultiplier = Mathf.Max(0, settings.damageTakenMultiplier);
            Range = Mathf.Max(0.1f, settings.attackRange);
            Interval = Mathf.Max(0.1f, settings.attackInterval);
            Movement = movement;
            healthFill = fill;
        }

        public void RefreshStats(UnitSettings settings)
        {
            if(!Alive)return;
            int previousMax=MaxHP;
            MaxHP=Mathf.Max(1,settings.maxHP);
            HP=StatusMath.PreserveHealth(HP,previousMax,MaxHP);
            Attack=Mathf.Max(0,settings.attack);
            Armour=Mathf.Max(0,settings.armour);
            DamageTakenMultiplier=Mathf.Max(0,settings.damageTakenMultiplier);
            Range=Mathf.Max(.1f,settings.attackRange);
            Interval=Mathf.Max(.1f,settings.attackInterval);
            if(Movement!=null)Movement.profile.baseSpeed=Mathf.Max(0,settings.moveSpeed);
            if(healthFill!=null)healthFill.localScale=new Vector3((float)HP/MaxHP,1,1);
        }

        // 最大HPを超えず、破壊済みの機体は復活させません。
        public int Heal(int amount)
        {
            if (!Alive || amount <= 0) return 0;
            int gained = Mathf.Min(amount, MaxHP - HP);
            HP += gained;
            if (healthFill != null) healthFill.localScale = new Vector3((float)HP / MaxHP, 1, 1);
            return gained;
        }

        public void TakeDamage(int attack)
        {
            if (!Alive) return;
            int damage = BattleDamage.Calculate(attack, Armour, DamageTakenMultiplier);
            HP = Mathf.Max(0, HP - damage);
            if (healthFill != null) healthFill.localScale = new Vector3((float)HP / MaxHP, 1, 1);
            if (!Alive)
            {
                if (Movement != null) Movement.Stop();
                gameObject.SetActive(false);
            }
        }
    }
}

