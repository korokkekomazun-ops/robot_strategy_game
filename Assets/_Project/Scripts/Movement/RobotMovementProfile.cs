using System;
using UnityEngine;

namespace RobotStrategy.Movement
{
    public enum TerrainKind { Ground = 0, Sea = 1, Sand = 2 }
    // Existing development screen stores 0=C, 1=B, 2=A.
    public enum TerrainAptitude { C = 0, B = 1, A = 2 }

    [Serializable]
    public class RobotMovementProfile
    {
        [Min(0)] public float baseSpeed = 3f;
        [Min(0)] public float speedPerPoint = 0.5f;
        [Min(0)] public int speedPoints;
        public TerrainAptitude ground = TerrainAptitude.C;
        public TerrainAptitude sea = TerrainAptitude.C;
        public TerrainAptitude sky = TerrainAptitude.C;
        public TerrainAptitude sand = TerrainAptitude.B;
        [Min(0)] public float cMultiplier = 0.5f;
        [Min(0)] public float bMultiplier = 1f;
        [Min(0)] public float aMultiplier = 1.5f;

        public float GetSpeed(TerrainKind terrain, bool flying)
        {
            var rank = flying ? sky : terrain == TerrainKind.Sea ? sea : terrain == TerrainKind.Sand ? sand : ground;
            float multiplier = rank == TerrainAptitude.A ? aMultiplier
                : rank == TerrainAptitude.B ? bMultiplier : cMultiplier;
            return (Math.Max(0, baseSpeed) + Math.Max(0, speedPoints) * Math.Max(0, speedPerPoint))
                * Math.Max(0, multiplier);
        }

        public void SetDevelopmentValues(int points, int groundRank, int seaRank, int skyRank)
        {
            speedPoints = Math.Max(0, points);
            ground = ClampRank(groundRank);
            sea = ClampRank(seaRank);
            sky = ClampRank(skyRank);
        }

        private static TerrainAptitude ClampRank(int rank)
            => (TerrainAptitude)Math.Max(0, Math.Min(2, rank));
    }
}
