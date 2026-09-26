using System;

namespace RobotStrategy.Battle
{
    // 攻撃・装甲・耐性から最終ダメージを計算します。
    public static class BattleDamage
    {
        // Armour is subtracted first; fractional HP damage is rounded up.
        public static int Calculate(int attack, int armour, float receivedMultiplier)
        {
            double raw=Math.Max(0L,(long)attack-Math.Max(0,armour));
            double multiplier=float.IsNaN(receivedMultiplier)?1:Math.Max(0,receivedMultiplier);
            if(raw==0 || multiplier==0)return 0;
            return (int)Math.Min(int.MaxValue,Math.Ceiling(raw*multiplier));
        }
    }
}
