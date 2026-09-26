using System;

namespace RobotStrategy.Battle
{
    // 速度と最大HP変更後の残りHPを計算します。
    public static class StatusMath
    {
        public static float Speed(float baseSpeed,int level,float gain,float speedMultiplier)
            => (Math.Max(0,baseSpeed)+Math.Max(0,level)*Math.Max(0,gain))*Math.Max(0,speedMultiplier);

        public static int PreserveHealth(int hp,int oldMax,int newMax)
        {
            if(hp<=0)return 0;
            newMax=Math.Max(1,newMax);
            return (int)Math.Max(1,Math.Min(newMax,Math.Ceiling((double)hp/Math.Max(1,oldMax)*newMax)));
        }
    }
}
