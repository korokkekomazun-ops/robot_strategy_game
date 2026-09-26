using System;
namespace RobotStrategy.Battle
{
    // A～D国の特典と機体種類のルールをまとめます。
    public enum Nation { None, A, B, C, D }
    public enum ChassisKind { Tank, Fighter, Ship, Transformer, Humanoid, Giant }
    public static class CountryRules
    {
        public static string KindName(ChassisKind kind)
            => new[] { "戦車", "戦闘機", "船", "トランスフォーマー", "人型", "巨大機体" }[(int)kind];
        public static bool CanChoose(Nation nation, ChassisKind kind) => kind != ChassisKind.Giant || nation == Nation.C;
        public static int CPCost(int normal, ChassisKind kind, int giantMultiplier)
            => (int)Math.Min(int.MaxValue,(long)Math.Max(0,normal)*(kind==ChassisKind.Giant?Math.Max(3,giantMultiplier):1));
        public static int InitialCP(int normal, Nation nation, int bonus)
            => (int)Math.Min(int.MaxValue, (long)Math.Max(0,normal)+(nation==Nation.A?Math.Max(0,bonus):0));
        public static int InitialPoints(int normal, Nation nation, int bonus)
            => (int)Math.Min(int.MaxValue,(long)Math.Max(0,normal)+(nation==Nation.B?Math.Max(0,bonus):0));
        public static int Damage(int attack, Nation source, Nation target, float multiplier)
        {
            double value=Math.Max(0,attack);
            if(source==Nation.D&&target!=Nation.None&&target!=source)value*=Math.Max(1,multiplier);
            return (int)Math.Min(int.MaxValue,Math.Ceiling(value));
        }
    }
}
