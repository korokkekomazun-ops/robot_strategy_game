using System.Collections.Generic;

namespace RobotStrategy.Battle
{
    // 参加順に右上・左上・左下・右下の席を割り当てます。
    // The first valid nation choice reserves a seat; changing nation keeps it.
    public sealed class PlayerSeats
    {
        private readonly Dictionary<ulong,int> slots = new Dictionary<ulong,int>();
        public int Reserve(ulong client)
        {
            if(slots.TryGetValue(client,out int current))return current;
            for(int i=0;i<4;i++)
                if(!slots.ContainsValue(i)){slots.Add(client,i);return i;}
            return -1;
        }
        public void Release(ulong client)=>slots.Remove(client);
        public static int X(int slot)=>slot==0||slot==3?1:-1;
        public static int Y(int slot)=>slot<2?1:-1;
        public static string Corner(int slot)=>new[]{"右上","左上","左下","右下"}[slot];
        public static bool CanControl(int owner,int player)=>owner>=0&&owner==player;
    }
}
