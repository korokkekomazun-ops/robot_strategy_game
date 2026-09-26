using System;
using System.Collections.Generic;

namespace RobotStrategy.Battle
{
    // 一度に出す敵の数と出現位置候補を計算します。
    public struct EnemyPoint
    {
        public double x,y;
    }

    public static class EnemyGroup
    {
        public static int Count(int alive,int limit,int requested)
            =>Math.Max(0,Math.Min(Math.Max(0,requested),Math.Max(0,limit)-Math.Max(0,alive)));

        // Start three tiles from the boss, then use outer rings if the inner ring is crowded.
        public static IEnumerable<EnemyPoint> Candidates(float radius,float rotation)
        {
            for(int ring=0;ring<4;ring++)
            {
                double distance=Math.Max(1,radius)+ring*1.2;
                int count=Math.Max(6,(int)(2*Math.PI*distance/1.1));
                for(int i=0;i<count;i++)
                {
                    // Disperse consecutive spawns around the boss, rather than one side.
                    int index=i%2==0?i/2:count-1-i/2;
                    double angle=rotation+index*Math.PI*2/count;
                    yield return new EnemyPoint{x=Math.Cos(angle)*distance,y=Math.Sin(angle)*distance};
                }
            }
        }
    }
}
