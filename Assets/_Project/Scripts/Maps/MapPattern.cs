using System;

namespace RobotStrategy.Battle
{
    // マップの各マスを森・砂・川に分けます。
    public enum BattleMapKind { ForestAndSand, Desert, DesertRiver }
    public enum BattleMapTerrain { Forest, Sand, River }

    // Deterministic tile layout: all LAN peers use the host's parameters.
    public static class MapPattern
    {
        public static bool IsValid(int value)=>value>=0&&value<=2;
        public static string Name(BattleMapKind kind)=>kind==BattleMapKind.Desert?"砂地のみ":
            kind==BattleMapKind.DesertRiver?"砂地と川":"森林・砂地";

        public static BattleMapTerrain At(BattleMapKind kind,int x,int y,int size,int sandWidth,int riverWidth)
        {
            if(kind==BattleMapKind.Desert)return BattleMapTerrain.Sand;
            if(kind==BattleMapKind.DesertRiver)
            {
                int width=Math.Max(1,Math.Min(5,riverWidth));
                // A narrow, winding river beside the boss; all four corners stay on sand.
                int bend=(int)Math.Round(Math.Sin(y*Math.PI*2/Math.Max(1,size-1))*Math.Min(2,size/12));
                int riverX=Math.Min(size-3-width/2,size/2+Math.Max(3,size/8)+bend);
                int left=riverX-width/2;
                bool water=x>=left&&x<left+width;
                if(Math.Abs(x-size/2)<=1&&Math.Abs(y-size/2)<=1)water=false;
                return water?BattleMapTerrain.River:BattleMapTerrain.Sand;
            }
            int strip=Math.Max(0,Math.Min(size-4,sandWidth));
            int low=(size-strip)/2;
            return strip>0&&((x>=low&&x<low+strip)||(y>=low&&y<low+strip))
                ?BattleMapTerrain.Sand:BattleMapTerrain.Forest;
        }
    }
}
