using UnityEngine;
using UnityEngine.Tilemaps;

namespace RobotStrategy.Movement
{
    // Self-contained runtime demo; does not modify the game's scenes or prefabs.
    public class TerrainMovementDemo : MonoBehaviour
    {
        private Texture2D texture;
        private Sprite sprite;
        private TerrainMovementTile land;
        private TerrainMovementTile water;
        private readonly RobotTerrainMovement[] robots = new RobotTerrainMovement[3];
        private void Start()
        {
            texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
            var grid = new GameObject("Terrain Grid", typeof(Grid));
            grid.transform.SetParent(transform);
            var tiles = new GameObject("Ground and Sea", typeof(Tilemap), typeof(TilemapRenderer), typeof(TerrainMovementMap));
            tiles.transform.SetParent(grid.transform, false);
            var map = tiles.GetComponent<Tilemap>();
            land = ScriptableObject.CreateInstance<TerrainMovementTile>();
            land.sprite = sprite;
            land.color = new Color(0.25f, 0.55f, 0.3f);
            land.terrain = TerrainKind.Ground;
            water = ScriptableObject.CreateInstance<TerrainMovementTile>();
            water.sprite = sprite;
            water.color = new Color(0.15f, 0.4f, 0.7f);
            water.terrain = TerrainKind.Sea;
            for (int x = 0; x < 18; x++)
                for (int y = 0; y < 7; y++)
                    map.SetTile(new Vector3Int(x, y, 0), x < 9 ? land : water);
            for (int i = 0; i < robots.Length; i++)
            {
                var robot = new GameObject(new[] { "Land A - Sea C", "Land C - Sea A", "Flying - Sky B" }[i], typeof(SpriteRenderer), typeof(RobotTerrainMovement));
                robot.transform.SetParent(transform);
                robot.transform.position = new Vector3(1.5f, 1.5f + i * 2, 0);
                robot.transform.localScale = Vector3.one * 0.5f;
                var renderer = robot.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = new[] { Color.yellow, Color.red, Color.white }[i];
                renderer.sortingOrder = 1;
                robots[i] = robot.GetComponent<RobotTerrainMovement>();
                robots[i].terrainMap = tiles.GetComponent<TerrainMovementMap>();
                robots[i].ApplyDevelopmentValues(0, i == 0 ? 2 : 0, i == 1 ? 2 : 0, 1);
                robots[i].flying = i == 2;
                robots[i].SetDestination(new Vector3(16.5f, robot.transform.position.y, 0));
            }
        }

        private void Update()
        {
            foreach (var robot in robots)
                if (robot != null && !robot.IsMoving)
                    robot.SetDestination(new Vector3(robot.transform.position.x > 9 ? 1.5f : 16.5f, robot.transform.position.y, 0));
        }

        private void OnDestroy()
        {
            if (land != null) Destroy(land);
            if (water != null) Destroy(water);
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
        }
    }
}
