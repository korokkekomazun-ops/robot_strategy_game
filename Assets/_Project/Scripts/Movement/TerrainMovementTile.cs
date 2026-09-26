using UnityEngine;
using UnityEngine.Tilemaps;

namespace RobotStrategy.Movement
{
    [CreateAssetMenu(fileName = "TerrainTile", menuName = "Robot Strategy/Terrain Tile")]
    public class TerrainMovementTile : Tile
    {
        public TerrainKind terrain;
    }
}
