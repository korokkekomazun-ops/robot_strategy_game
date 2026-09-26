using UnityEngine;

namespace RobotStrategy.Battle
{
    // 開発画面で決めた内容を戦闘シーンへ渡します。
    // One-shot hand-off: robotdevelop prepares a design; MainScene consumes it.
    public static class BattleStart
    {
        private static RobotDesign pending;
        private static BattleMapKind pendingMap;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){pending=null;pendingMap=BattleMapKind.ForestAndSand;}

        public static void Prepare(RobotDesign design,BattleMapKind map=BattleMapKind.ForestAndSand){pending=design;pendingMap=map;}

        public static bool TryConsume(out RobotDesign design)=>TryConsume(out design,out _);

        public static bool TryConsume(out RobotDesign design,out BattleMapKind map)
        {
            map=pendingMap;
            design = pending;
            pending = null;
            return design != null;
        }
    }
}
