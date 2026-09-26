using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using RobotStrategy.Movement;

namespace RobotStrategy.Battle
{
    // 戦場全体を作り、移動・攻撃・敵増援を進める中心クラスです。
    [DefaultExecutionOrder(-1000)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RobotStrategy.Prototype", null, "ForestBattlePrototype")]
    public partial class BattleManager : MonoBehaviour
    {
        [Header("Local Prototype")]
        [SerializeField] private bool enableLocalPrototype = true;
        [SerializeField] private Camera mapCamera;
        [SerializeField] private GameObject legacyCanvas;
        [SerializeField] private Behaviour legacyUIController;
        [Header("Artwork")]
        [SerializeField] private Sprite forestSprite;
        [SerializeField] private Sprite robotSprite;
        [SerializeField] private Sprite bossSprite;
        [SerializeField] private Sprite smallEnemySprite;
        [SerializeField] private Sprite smokeSprite;
        [SerializeField] private TMP_FontAsset uiFont;
        [Header("Map and Spawning")]
        [SerializeField, Min(10)] private int mapSize = 50;
        [SerializeField, Min(1)] private int initialRobotCount = 3;
        [SerializeField, Min(0.1f)] private float spawnInterval = 10;
        [SerializeField, Min(1)] private float spawnRadius = 3;
        [SerializeField, Min(1)] private int maxSmallEnemies = 120;
        [SerializeField, Range(1,200)] private int minEnemiesPerWave = 10;
        [SerializeField, Range(1,200)] private int maxEnemiesPerWave = 20;
        [SerializeField, Min(1)] private float cameraSpeed = 12;
        [Header("Unit Stats (prototype values)")]
        [SerializeField, HideInInspector] private UnitSettings robotStats = new UnitSettings();
        [SerializeField] private UnitSettings bossStats = new UnitSettings
            { maxHP = 500, attack = 75, damageTakenMultiplier = .25f, moveSpeed = 0, attackRange = 4, attackInterval = 1.5f };
        [SerializeField] private UnitSettings smallEnemyStats = new UnitSettings
            { maxHP = 40, attack = 25, damageTakenMultiplier = .25f, moveSpeed = 1.5f, attackRange = 1.2f };
        [SerializeField] private UnitSettings baseStats = new UnitSettings
            { maxHP = 400, attack = 0, moveSpeed = 0 };

        private readonly List<BattleUnit> units = new List<BattleUnit>();
        private readonly List<BattleUnit> bases = new List<BattleUnit>();
        private BattleUnit boss;
        private BattleUnit selected;
        private TerrainMovementMap terrainMap;
        private TerrainMovementTile forestTile;
        private TerrainMovementTile sandTile;
        [Header("Sand terrain")]
        [SerializeField] private Sprite sandSprite;
        [SerializeField, Range(0,20)] private int sandStripWidth = 8;
        private Texture2D whiteTexture;
        private Sprite whiteSprite;
        private Material lineMaterial;
        private LineRenderer arrow;
        private float nextSpawn;
        private float halfSize;
        private Vector3 center;
        private bool finished;
        private string result = "";

        private void Awake()
        {
            // In LAN mode only the host simulates combat; clients send commands and display snapshots.
            networkMode=NetworkManager.Singleton!=null&&NetworkManager.Singleton.IsListening;
            if (!enableLocalPrototype)
            { enabled = false; return; }
            if (mapCamera == null) mapCamera = Camera.main;
            if (mapCamera == null) { Debug.LogError("Forest prototype: assign Map Camera.", this); enabled = false; return; }
            if (legacyUIController != null) legacyUIController.enabled = false;
            if (legacyCanvas != null) legacyCanvas.SetActive(false);
            var oldCameraControl = mapCamera.GetComponent("CameraController") as Behaviour;
            if (oldCameraControl != null) oldCameraControl.enabled = false;
        }

        private void Start()
        {
            if (IsPreparationScene)
            {
                BuildAllUI();
                return;
            }
            RobotDesign prepared=null;
            BattleMapKind preparedMap=battleMap;
            bool hasPrepared=!networkMode&&BattleStart.TryConsume(out prepared,out preparedMap);
            if(hasPrepared){battleMap=preparedMap;mapConfirmed=true;}
            mapSize = Mathf.Max(10, mapSize);
            halfSize = mapSize / 2f;
            center = new Vector3(0.5f, 0.5f, 0);
            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
            whiteSprite = Sprite.Create(whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
            BuildMap();
            mapCamera.orthographic = true;
            mapCamera.orthographicSize = Mathf.Min(16, halfSize);
            mapCamera.transform.position = new Vector3(0, 0, -10);
            mapCamera.transform.rotation = Quaternion.identity;
            mapCamera.backgroundColor = new Color(0.04f, 0.09f, 0.06f);
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            if(!networkMode||NetworkManager.Singleton.IsServer)boss = SpawnUnit("Central Monster", BattleTeam.Enemy, false, bossSprite, center, bossStats, 2.2f, false);
            BuildArrow();
            BuildAllUI();
            if(networkMode)InitializeLan();
            else if(hasPrepared) DeployInitialDesign(prepared);
            nextSpawn = Time.time + Mathf.Max(0.1f, spawnInterval);
        }

        private void BuildMap()
        {
            var grid = new GameObject("Battle Grid", typeof(Grid));
            grid.transform.SetParent(transform, false);
            var tiles = new GameObject("Forest 50x50", typeof(Tilemap), typeof(TilemapRenderer), typeof(TerrainMovementMap));
            tiles.transform.SetParent(grid.transform, false);
            terrainMap = tiles.GetComponent<TerrainMovementMap>();
            forestTile = ScriptableObject.CreateInstance<TerrainMovementTile>();
            forestTile.terrain = TerrainKind.Ground;
            forestTile.sprite = forestSprite != null ? forestSprite : whiteSprite;
            forestTile.color = forestSprite != null ? Color.white : new Color(0.15f, 0.35f, 0.1f);
            float scale = SpriteScale(forestTile.sprite, 1);
            forestTile.transform = Matrix4x4.Scale(Vector3.one * scale);
            sandTile=ScriptableObject.CreateInstance<TerrainMovementTile>();
            sandTile.terrain=TerrainKind.Sand;
            sandTile.sprite=sandSprite!=null?sandSprite:whiteSprite;
            sandTile.color=sandSprite!=null?Color.white:new Color(.92f,.77f,.4f);
            sandTile.transform=Matrix4x4.Scale(Vector3.one*SpriteScale(sandTile.sprite,1));
            riverTile=ScriptableObject.CreateInstance<TerrainMovementTile>();
            riverTile.terrain=TerrainKind.Sea;
            riverTile.sprite=riverSprite!=null?riverSprite:whiteSprite;
            riverTile.color=Color.white;
            riverTile.transform=Matrix4x4.Scale(Vector3.one*SpriteScale(riverTile.sprite,1));
            RefreshMapTiles();
        }

        private BattleUnit SpawnUnit(string title, BattleTeam team, bool isBase, Sprite sprite,
            Vector3 position, UnitSettings stats, float size, bool canMove = true)
        {
            var root = new GameObject(title, typeof(BattleUnit));
            root.transform.SetParent(transform, false);
            root.transform.position = position;
            var art = new GameObject("Artwork", typeof(SpriteRenderer));
            art.transform.SetParent(root.transform, false);
            var renderer = art.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : whiteSprite;
            renderer.sortingOrder = 10;
            renderer.color = isBase ? new Color(0.2f, 0.9f, 1) : Color.white;
            art.transform.localScale = Vector3.one * SpriteScale(renderer.sprite, size);
            var background = new GameObject("HP Background", typeof(SpriteRenderer));
            background.transform.SetParent(root.transform, false);
            background.transform.localPosition = new Vector3(0, size * 0.55f, 0);
            background.transform.localScale = new Vector3(size, 0.12f, 1);
            var bg = background.GetComponent<SpriteRenderer>(); bg.sprite = whiteSprite; bg.color = Color.black; bg.sortingOrder = 20;
            var pivot = new GameObject("HP Pivot"); pivot.transform.SetParent(background.transform, false);
            pivot.transform.localPosition = new Vector3(-0.5f, 0, 0);
            var bar = new GameObject("HP", typeof(SpriteRenderer)); bar.transform.SetParent(pivot.transform, false);
            bar.transform.localPosition = new Vector3(0.5f, 0, 0);
            var fill = bar.GetComponent<SpriteRenderer>(); fill.sprite = whiteSprite; fill.sortingOrder = 21;
            fill.color = team == BattleTeam.Player ? Color.green : new Color(1, 0.3f, 0.2f);
            RobotTerrainMovement movement = null;
            if (canMove && !isBase)
            {
                movement = root.AddComponent<RobotTerrainMovement>();
                movement.terrainMap = terrainMap;
                movement.profile.baseSpeed = Mathf.Max(0, stats.moveSpeed);
                movement.profile.ground = TerrainAptitude.B;
            }
            var unit = root.GetComponent<BattleUnit>();
            unit.Configure(team, isBase, stats, movement, pivot.transform);
            unit.NetworkId=nextUnitId++;
            ApplyMovementBalance(unit);
            units.Add(unit);
            return unit;
        }

        private void Update()
        {
            if(lanStatus!=null)lanStatus.text=LanConnect.Status;
            if(networkMode)TickLan();
            if (!battleStarted) { RefreshBattleUI(); return; }
            if(networkMode&&!net.IsServer)
            {
                if(!AnyPanelOpen){HandleCamera();HandleSelection();}
                RefreshBattleUI();UpdateArrow();return;
            }
            if (!AnyPanelOpen) HandleCamera();
            if (!AnyPanelOpen) HandleSelection();
            if (!finished)
            {
                if (Time.time >= nextSpawn)
                {
                    SpawnSmallEnemyWave();
                    nextSpawn = Time.time + Mathf.Max(0.1f, spawnInterval);
                }
                TickHealthBoxes();
                for (int i = 0; i < units.Count && !finished; i++) TickUnit(units[i]);
                bool anyBase = bases.Exists(b => b != null && b.Alive && b.Country == playerNation);
                if (!finished && boss != null && !boss.Alive) EndBattle("VICTORY - central monster defeated");
                else if (!finished && !anyBase && !networkMode) EndBattle("DEFEAT - all bases destroyed");
            }
            for (int i = units.Count - 1; i >= 0; i--)
            {
                var dead = units[i];
                if (dead != null && dead != boss && !dead.IsBase && !dead.Alive)
                { units.RemoveAt(i); if (selected == dead) selected = null; Destroy(dead.gameObject); }
            }
            RefreshBattleUI();
            UpdateArrow();
        }

        private void TickUnit(BattleUnit unit)
        {
            if (unit == null || !unit.Alive || unit.IsBase) return;
            BattleUnit target = null;
            float best = float.PositiveInfinity;
            foreach (var candidate in units)
            {
                if (candidate == null || !candidate.Alive || !IsHostile(unit, candidate)) continue;
                // Small enemies follow the closest surviving base; the boss remains stationary.
                if (unit.Country == Nation.None && unit != boss && !candidate.IsBase) continue;
                float distance = Vector3.Distance(unit.transform.position, candidate.transform.position);
                if (distance < best) { best = distance; target = candidate; }
            }
            if (target == null) { if (unit.Country == Nation.None && unit.Movement != null) unit.Movement.Stop(); return; }
            if (unit != boss && unit.Movement != null && (unit.Country == Nation.None || (!networkMode && npcAutoMove && unit.Country != playerNation)))
            {
                if (best > unit.Range) unit.Movement.SetDestination(target.transform.position);
                else unit.Movement.Stop();
            }
            if (best <= unit.Range && Time.time >= unit.NextAttackTime && unit.Attack > 0)
            {
                unit.NextAttackTime = Time.time + unit.Interval;
                unit.AttackSerial++;unit.LastHit=target.transform.position;
                target.TakeDamage(MatchupAttack(unit,target,CountryRules.Damage(unit.Attack, unit.Country, target.Country, nationDDamageMultiplier)));
                if(target==boss && !target.Alive)
                {
                    bossDefeatedBy=unit.Country;bossFinisher=unit.name;
                    EndBattle(networkMode?"P"+(unit.OwnerSlot+1)+"（"+unit.Country+"国）の勝利\n中央の怪獣を撃破しました。":"中央の怪獣を撃破しました。");
                    return;
                }
                if (!target.Alive) AwardDefeat(unit,target);
                StartCoroutine(ShowSmoke(target.transform.position));
            }
        }

        private void SpawnSmallEnemyWave()
        {
            int alive=units.FindAll(u=>u!=null&&u!=boss&&u.Country==Nation.None&&u.Alive).Count;
            int minimum=Mathf.Clamp(minEnemiesPerWave,1,200);
            int maximum=Mathf.Clamp(maxEnemiesPerWave,minimum,200);
            int count=EnemyGroup.Count(alive,maxSmallEnemies,Random.Range(minimum,maximum+1));
            if(count==0)return;
            int spawned=0;
            foreach(var offset in EnemyGroup.Candidates(spawnRadius,Random.Range(0f,Mathf.PI*2)))
            {
                Vector3 position=center+new Vector3((float)offset.x,(float)offset.y,0);
                if(!terrainMap.TryGetTerrain(position,out _))continue;
                if(units.Exists(u=>u!=null&&u.Alive&&Vector3.Distance(u.transform.position,position)<.9f))continue;
                SpawnUnit("Small Enemy",BattleTeam.Enemy,false,smallEnemySprite,position,smallEnemyStats,1);
                if(++spawned>=count)break;
            }
        }

        private void HandleSelection()
        {
            var mouse = Mouse.current;
            if (mouse == null || PointerOnBattleUI()) return;
            Vector2 pointer = mouse.position.ReadValue();
            if (!mapCamera.pixelRect.Contains(pointer)) return;
            Vector3 world = mapCamera.ScreenToWorldPoint(new Vector3(pointer.x, pointer.y, -mapCamera.transform.position.z));
            world.z = 0;
            if (mouse.leftButton.wasPressedThisFrame)
            {
                BattleUnit hit = null;
                float best = 1.2f;
                foreach (var unit in units)
                {
                    if (unit == null || !unit.Alive) continue;
                    float d = Vector3.Distance(unit.transform.position, world);
                    if (d < best) { hit = unit; best = d; }
                }
                if (hit != null)
                {
                    selected = hit;
                    feedback = hit.Team == BattleTeam.Player && hit.Movement != null
                        ? hit.name + "を選択。地面を左クリックで移動できます。"
                        : hit.name + "を確認中。移動する味方を選択してください。";
                }
                else IssueMoveOrder(world);
            }
            if (mouse.rightButton.wasPressedThisFrame) IssueMoveOrder(world);
        }

        private void IssueMoveOrder(Vector3 world)
        {
            if (finished) return;
            if(networkMode)
            {
                if(selected!=null&&selected.Alive&&PlayerSeats.CanControl(selected.OwnerSlot,localSeat)&&!selected.IsBase)
                    SendCommand(new PlayerOrder{op="move",unit=selected.NetworkId,x=world.x,y=world.y});
                return;
            }
            if (selected == null || !selected.Alive || selected.Team != BattleTeam.Player || selected.Movement == null)
            { feedback = "先に移動する味方を左クリック、または配備済み機体一覧から選択してください。"; return; }
            if (!selected.Movement.SetDestination(world))
            { feedback = "マップ内の地面をクリックしてください。"; return; }
            feedback = selected.name + "に移動指示を出しました。";
        }

        private void HandleCamera()
        {
            Vector3 direction = Vector3.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction.x--;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction.x++;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) direction.y--;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) direction.y++;
            }
            Vector3 pos = mapCamera.transform.position + direction.normalized * cameraSpeed * Time.deltaTime;
            var mouse = Mouse.current;
            if (mouse != null && !PointerOnBattleUI())
            {
                float scale = 2 * mapCamera.orthographicSize / Mathf.Max(1, mapCamera.pixelHeight);
                if (mouse.middleButton.isPressed)
                { Vector2 delta = mouse.delta.ReadValue(); pos -= new Vector3(delta.x, delta.y, 0) * scale; }
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll != 0) mapCamera.orthographicSize = Mathf.Clamp(mapCamera.orthographicSize - Mathf.Sign(scroll), 4, halfSize + 2);
            }
            pos.x = Mathf.Clamp(pos.x, -halfSize, halfSize);
            pos.y = Mathf.Clamp(pos.y, -halfSize, halfSize);
            pos.z = -10;
            mapCamera.transform.position = pos;
        }

        private void BuildArrow()
        {
            var obj = new GameObject("Move Order Arrow", typeof(LineRenderer)); obj.transform.SetParent(transform, false);
            arrow = obj.GetComponent<LineRenderer>();
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) { lineMaterial = new Material(shader); arrow.sharedMaterial = lineMaterial; }
            arrow.startColor = arrow.endColor = Color.yellow; arrow.startWidth = arrow.endWidth = 0.08f;
            arrow.sortingOrder = 30; arrow.useWorldSpace = true; arrow.positionCount = 5; arrow.enabled = false;
        }

        private void UpdateArrow()
        {
            bool show = selected != null && selected.Alive && selected.Team == BattleTeam.Player
                && selected.Movement != null && selected.Movement.IsMoving;
            arrow.enabled = show;
            if (!show) return;
            Vector3 from = selected.transform.position;
            Vector3 to = selected.Movement.Destination;
            Vector3 back = (from - to).normalized * 0.6f;
            Vector3 side = new Vector3(-back.y, back.x, 0) * 0.5f;
            arrow.SetPositions(new[] { from, to, to + back + side, to, to + back - side });
        }

        private IEnumerator ShowSmoke(Vector3 position)
        {
            var obj = new GameObject("Hit Smoke", typeof(SpriteRenderer)); obj.transform.SetParent(transform, false);
            obj.transform.position = position;
            var sr = obj.GetComponent<SpriteRenderer>(); sr.sprite = smokeSprite != null ? smokeSprite : whiteSprite; sr.sortingOrder = 25;
            float elapsed = 0;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                sr.color = new Color(1, 1, 1, 1 - elapsed / 0.4f);
                obj.transform.localScale = Vector3.one * SpriteScale(sr.sprite, 0.3f + elapsed * 2);
                yield return null;
            }
            Destroy(obj);
        }

        private void EndBattle(string message)
        {
            if(finished)return;
            finished = true; result = message;
            ShowBattleResult();
            if(networkMode)resultText.text=result;
            foreach (var unit in units) if (unit != null && unit.Movement != null) unit.Movement.Stop();
        }

        private Vector3 ClampPosition(Vector3 value)
        {
            int start = -(mapSize / 2);
            value.x = Mathf.Clamp(value.x, start + 0.5f, start + mapSize - 0.5f);
            value.y = Mathf.Clamp(value.y, start + 0.5f, start + mapSize - 0.5f);
            return value;
        }

        private static float SpriteScale(Sprite sprite, float size)
            => size / Mathf.Max(0.001f, Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));

        private void OnDestroy()
        {
            StopLan();
            if (forestTile != null) Destroy(forestTile);
            if (sandTile != null) Destroy(sandTile);
            if (riverTile != null) Destroy(riverTile);
            if (lineMaterial != null) Destroy(lineMaterial);
            if (whiteSprite != null) Destroy(whiteSprite);
            if (whiteTexture != null) Destroy(whiteTexture);
        }
    }
}

