using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using RobotStrategy.Movement;

namespace RobotStrategy.Battle
{
    // マップ選択画面と地形タイルの配置を管理します。
    public partial class BattleManager
    {
        [Header("Battle map / river")]
        [SerializeField] private BattleMapKind battleMap=BattleMapKind.ForestAndSand;
        [SerializeField, Range(1,5)] private int riverWidth=3;
        [SerializeField] private Sprite riverSprite;
        private TerrainMovementTile riverTile;
        private bool mapConfirmed;
        private readonly Button[] mapButtons=new Button[3];
        private TextMeshProUGUI mapDescription;

        private void BuildMapChoices(Transform panel)
        {
            // Keep the user's existing panel; replace the old single-map action.
            var legacy=FindUI(panel,PositionKey("Button",24,248));
            if(legacy!=null)legacy.SetActive(false);
            mapDescription=Text(panel,"Description",24,94,672,100,22);
            for(int i=0;i<mapButtons.Length;i++)
            {
                var kind=(BattleMapKind)i;
                mapButtons[i]=MakeButton(panel,MapPattern.Name(kind),24,178+i*50,672,()=>SelectBattleMap(kind));
            }
            RefreshMapChoice();
        }

        private void RefreshMapChoice()
        {
            if(mapDescription==null)return;
            bool host=!networkMode||(NetworkManager.Singleton!=null&&NetworkManager.Singleton.IsServer);
            mapDescription.text=host?$"{mapSize} × {mapSize}　中央の怪獣を倒すと戦闘終了。\nマップを選んで出撃準備へ。":
                "ホストがマップを選択しています。\n確定すると国家選択へ進みます。";
            foreach(var button in mapButtons)if(button!=null)button.interactable=host&&!mapConfirmed;
        }

        private void SelectBattleMap(BattleMapKind kind)
        {
            if(!MapPattern.IsValid((int)kind)||mapConfirmed||battleStarted||countingDown)return;
            if(networkMode&&(net==null||!net.IsServer))return;
            battleMap=kind;mapConfirmed=true;
            RefreshMapTiles();
            ShowPreparationPage(PreparationPage.Nation);
        }

        private void RefreshMapTiles()
        {
            if(terrainMap==null)return; // robotdevelop prepares the choice without creating the battlefield.
            halfSize=mapSize/2f;
            var map=terrainMap.GetComponent<Tilemap>();
            map.ClearAllTiles();
            int start=-(mapSize/2);
            for(int x=0;x<mapSize;x++)for(int y=0;y<mapSize;y++)
            {
                var terrain=MapPattern.At(battleMap,x,y,mapSize,sandStripWidth,riverWidth);
                var tile=terrain==BattleMapTerrain.River?riverTile:terrain==BattleMapTerrain.Sand?sandTile:forestTile;
                var cell=new Vector3Int(start+x,start+y,0);
                map.SetTile(cell,tile);
                if(terrain==BattleMapTerrain.River&&riverSprite==null)
                {
                    map.SetTileFlags(cell,TileFlags.None);
                    float shade=(x+y)%3==0?1f:.91f;
                    map.SetColor(cell,new Color(.17f*shade,.53f*shade,.67f*shade));
                }
            }
            map.gameObject.name=MapPattern.Name(battleMap)+" "+mapSize+"x"+mapSize;
        }

        private void ApplyNetworkMap(BattleState state)
        {
            if(!MapPattern.IsValid(state.map)||state.mapSize<10||state.mapSize>500)return;
            bool changed=battleMap!=(BattleMapKind)state.map||mapSize!=state.mapSize||sandStripWidth!=state.sandWidth||riverWidth!=state.riverWidth;
            battleMap=(BattleMapKind)state.map;mapSize=state.mapSize;
            sandStripWidth=state.sandWidth;riverWidth=state.riverWidth;
            if(changed)RefreshMapTiles();
            bool newlyConfirmed=!mapConfirmed&&state.mapConfirmed;
            mapConfirmed=state.mapConfirmed;
            if(newlyConfirmed&&!localReady)ShowPreparationPage(PreparationPage.Nation);
        }
    }
}
