using System;

namespace RobotStrategy.Battle
{
    // LANで送る操作と戦況データの形を定義します。
    [Serializable] public class PlayerOrder
    {
        public string op;
        public int nation,kind,unit;
        public string designId;
        public int[] levels;
        public float x,y;
    }
    [Serializable] public class DesignInfo
    {
        public string id,name;
        public int nation,kind;
        public int[] levels;
        public static DesignInfo From(RobotDesign d)=>new DesignInfo{id=d.DesignId,name=d.Name,nation=(int)d.Country,kind=(int)d.Kind,levels=d.CopyLevels()};
        public RobotDesign ToDesign()=>new RobotDesign(name,levels,(ChassisKind)kind,(Nation)nation,id);
    }
    [Serializable] public class PlayerInfo
    {
        public int seat,nation;
        public bool ready;
    }
    [Serializable] public class UnitReport
    {
        public int id,seat,nation,hp,role,attackSerial;
        public float x,y,hitX,hitY,moveX,moveY;
        public bool moving;
        public string name;
        public UnitSettings stats;
        public DesignInfo design;
    }
    [Serializable] public class BoxReport
    {
        public int seat;
        public float x,y;
    }
    [Serializable] public class BattleState
    {
        public int map,mapSize,sandWidth,riverWidth;
        public bool mapConfirmed;
        public int seat=-1,nation,cp;
        public float countdown=-1,nextSpawn;
        public bool started,finished,ready;
        public string result,feedback;
        public PlayerInfo[] players;
        public DesignInfo[] designs;
        public UnitReport[] units;
        public BoxReport[] boxes;
    }
}
