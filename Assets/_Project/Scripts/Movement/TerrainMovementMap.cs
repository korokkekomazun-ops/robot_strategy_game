using UnityEngine;
using UnityEngine.Tilemaps;

namespace RobotStrategy.Movement
{
    [RequireComponent(typeof(Tilemap))]
    public class TerrainMovementMap : MonoBehaviour
    {
        private Tilemap map;
        private Tilemap Map => map != null ? map : (map = GetComponent<Tilemap>());

        // Empty and ordinary tiles are deliberately outside the movement area.
        public bool TryGetTerrain(Vector3 worldPosition, out TerrainKind terrain)
        {
            var tile = Map.GetTile(Map.WorldToCell(worldPosition)) as TerrainMovementTile;
            terrain = tile != null ? tile.terrain : TerrainKind.Ground;
            return tile != null;
        }

        public float SamplingDistance
        {
            get
            {
                Vector3 origin = Map.CellToWorld(Vector3Int.zero);
                float x = Vector3.Distance(origin, Map.CellToWorld(Vector3Int.right));
                float y = Vector3.Distance(origin, Map.CellToWorld(Vector3Int.up));
                return Mathf.Max(0.0001f, Mathf.Min(x, y) * 0.05f);
            }
        }
    }
}
