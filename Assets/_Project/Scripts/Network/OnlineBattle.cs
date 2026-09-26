using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace RobotStrategy.Battle
{
    // 4人対戦の操作をホストで処理し、戦況を各PCへ送ります。
    public partial class BattleManager
    {
        private const string CommandChannel="RobotBattle.Command.v2",StateChannel="RobotBattle.State.v2";
        private class Seat
        {
            public ulong client;
            public int slot=-1;
            public Nation nation;
            public bool ready;
            public RobotDesign initial;
            public DesignList designs;
            public string report="";
            public float rateStart;
            public int rateCount;
        }
        private readonly PlayerSeats seatOrder=new PlayerSeats();
        private readonly Dictionary<ulong,Seat> peers=new Dictionary<ulong,Seat>();
        private readonly Dictionary<int,BattleUnit> replicatedUnits=new Dictionary<int,BattleUnit>();
        private NetworkManager net;
        private bool networkMode,localReady;
        private int localSeat=-1,nextUnitId=1;
        private float nextNetworkSend,networkCountdownEnd=-1,lastSnapshotTime,nextHello;
        private bool receivedSnapshot;
        private string hostAddresses="";
        private GameObject lanWaitOverlay,lanConnectOverlay;
        private TextMeshProUGUI lanWaitText,lanStatus;
        private TMP_InputField lanAddress;
        public static bool LanMatchStarted {get;private set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLanFlags()=>LanMatchStarted=false;

        private void BuildLanUI()
        {
            var canvas=developmentOverlay.transform.parent;
            lanWaitOverlay=Overlay(canvas,"LAN参加待ち");
            var panel=Panel(lanWaitOverlay.transform,"参加状況",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(760,420));
            lanWaitText=Text(panel,"Status",24,24,710,280,24);
            MakeButton(panel,"接続を終了",24,338,710,LeaveLan);
            var resultPanel=FindUI(resultOverlay.transform,PositionKey("Panel:リザルトパネル",0,0));
            if(resultPanel!=null)MakeButton(resultPanel.transform,"タイトルへ",24,354,672,LeaveLan);
            lanWaitOverlay.SetActive(false);
            var entry=FindUI(mapChoiceOverlay.transform,PositionKey("Button",24,24));
            if(entry!=null)entry.SetActive(!networkMode);
            if(networkMode)
            {
                var previous=FindUI(canvas,"Overlay:LAN接続");
                if(previous!=null)previous.SetActive(false);
                return;
            }
            MakeButton(mapChoiceOverlay.transform,"LAN対戦（4人）",24,24,280,()=>lanConnectOverlay.SetActive(true));
            lanConnectOverlay=Overlay(canvas,"LAN接続");
            var p=Panel(lanConnectOverlay.transform,"LAN接続設定",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(700,420));
            Text(p,"Title",24,18,650,50,26).text="LAN対戦：1台が作成、ほか3台が参加";
            Text(p,"IPLabel",24,86,650,34,20).text="参加先ホストのIPv4 ／ 自分："+(Application.isPlaying?LanConnect.LocalAddresses:"");
            var input=Panel(p,"IP入力",new Vector2(0,1),new Vector2(0,1),new Vector2(24,-126),new Vector2(650,44));
            lanAddress=input.GetComponent<TMP_InputField>();
            if(lanAddress==null)lanAddress=input.gameObject.AddComponent<TMP_InputField>();
            var label=Text(input,"InputText",10,4,630,36,22);
            lanAddress.textViewport=label.rectTransform;lanAddress.textComponent=label;lanAddress.text="127.0.0.1";lanAddress.characterLimit=45;
            MakeButton(p,"ホストとして作成",24,194,310,()=>LanConnect.Connect(true,""));
            MakeButton(p,"IPへ参加",350,194,324,()=>LanConnect.Connect(false,lanAddress.text.Trim()));
            lanStatus=Text(p,"Status",24,246,650,84,19);
            MakeButton(p,"戻る",24,354,650,()=>lanConnectOverlay.SetActive(false));
            lanConnectOverlay.SetActive(Application.isPlaying&&LanConnect.OpenConnectionPage);
            if(Application.isPlaying)LanConnect.OpenConnectionPage=false;
        }

        private void InitializeLan()
        {
            net=NetworkManager.Singleton;
            hostAddresses=net.IsServer?LanConnect.LocalAddresses:"";
            net.CustomMessagingManager.RegisterNamedMessageHandler(CommandChannel,OnLanCommand);
            net.CustomMessagingManager.RegisterNamedMessageHandler(StateChannel,OnLanState);
            net.OnClientDisconnectCallback+=OnPeerDisconnected;
            lastSnapshotTime=Time.unscaledTime;
            SendCommand(new PlayerOrder{op="hello"});
        }

        private void LeaveLan()
        {
            if(net!=null)net.Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
        }

        private void SendCommand(PlayerOrder command)
        {
            if(net==null||!net.IsListening)return;
            if(net.IsServer){HandleCommand(net.LocalClientId,command);return;}
            SendJson(CommandChannel,NetworkManager.ServerClientId,JsonUtility.ToJson(command));
        }
        private void SendJson(string channel,ulong client,string json)
        {
            using(var writer=new FastBufferWriter(json.Length*4+16,Allocator.Temp))
            {
                writer.WriteValueSafe(json);
                net.CustomMessagingManager.SendNamedMessage(channel,client,writer,NetworkDelivery.ReliableFragmentedSequenced);
            }
        }
        private void OnLanCommand(ulong sender,FastBufferReader reader)
        {
            if(!net.IsServer||reader.Length>8192)return;
            try {reader.ReadValueSafe(out string json);HandleCommand(sender,JsonUtility.FromJson<PlayerOrder>(json));}
            catch(Exception e){Debug.LogWarning("Invalid LAN command: "+e.Message);}
        }

        private void HandleCommand(ulong client,PlayerOrder cmd)
        {
            if(cmd==null||!net.ConnectedClientsIds.Contains(client))return;
            if(!peers.TryGetValue(client,out Seat seat))
            {
                if(peers.Count>=4||battleStarted||countingDown)return;
                peers[client]=seat=new Seat{client=client};
            }
            if(Time.unscaledTime-seat.rateStart>=1){seat.rateStart=Time.unscaledTime;seat.rateCount=0;}
            if(++seat.rateCount>40)return;
            if(cmd.op=="hello")return;
            if((cmd.op=="nation"||cmd.op=="ready")&&!mapConfirmed)return;
            if(cmd.op=="nation")
            {
                if(seat.ready||battleStarted||countingDown||cmd.nation<1||cmd.nation>4)return;
                if(seat.slot<0)seat.slot=seatOrder.Reserve(client);
                if(seat.slot<0)return;
                seat.nation=(Nation)cmd.nation;
                if(client==net.LocalClientId){localSeat=seat.slot;playerNation=seat.nation;}
                return;
            }
            if(cmd.op=="ready")
            {
                if(seat.ready||countingDown||battleStarted||seat.slot<0||!ValidLevels(cmd.levels,(ChassisKind)cmd.kind)||!CountryRules.CanChoose(seat.nation,(ChassisKind)cmd.kind))return;
                var basisDesign=new RobotDesign("base",new int[RobotDesign.ValueCount],(ChassisKind)cmd.kind,seat.nation);
                long cost=DesignList.Cost(basisDesign,cmd.levels,initialStatPointCost,initialAptitudePointCost);
                if(cost<0||cost>CountryRules.InitialPoints(initialDesignPoints,seat.nation,nationBPointBonus))return;
                seat.initial=new RobotDesign("初期"+CountryRules.KindName((ChassisKind)cmd.kind),cmd.levels,(ChassisKind)cmd.kind,seat.nation);
                seat.designs=new DesignList(CountryRules.InitialCP(startingBattleCP,seat.nation,nationACPBonus),seat.initial);
                seat.ready=true;
                if(client==net.LocalClientId)
                {
                    catalog=seat.designs;localReady=true;
                    initialSetup=false;CloseDraft();ShowPreparationPage(PreparationPage.Hidden);
                }
                if(mapConfirmed&&peers.Count==4&&peers.Values.All(p=>p.ready))StartLanBattle();
                return;
            }
            if(!battleStarted||finished||!seat.ready)return;
            if(cmd.op=="move")
            {
                var unit=units.Find(u=>u!=null&&u.NetworkId==cmd.unit&&PlayerSeats.CanControl(u.OwnerSlot,seat.slot)&&u.Alive&&u.Movement!=null);
                if(unit!=null&&!float.IsNaN(cmd.x)&&!float.IsInfinity(cmd.x)&&!float.IsNaN(cmd.y)&&!float.IsInfinity(cmd.y))unit.Movement.SetDestination(new Vector3(cmd.x,cmd.y,0));
                return;
            }
            if(cmd.op!="deploy"&&cmd.op!="develop")return;
            if(!FindNationDeploymentPosition(seat.nation,out Vector3 position,seat.slot)){seat.report="配備上限、または拠点周囲の空きがありません。";return;}
            if(cmd.op=="deploy")
            {
                var design=seat.designs.Designs.FirstOrDefault(d=>d.DesignId==cmd.designId);
                if(design==null||!seat.designs.TrySpend(DeploymentCost(design.Kind))){seat.report="配備CPが不足しています。";return;}
                SpawnNationDesign(design,position,seat.slot);seat.report="機体を追加配備しました。";
            }
            else if(cmd.op=="develop")
            {
                if(!ValidLevels(cmd.levels,(ChassisKind)cmd.kind))return;
                var basisDesign=string.IsNullOrEmpty(cmd.designId)?null:seat.designs.Designs.FirstOrDefault(d=>d.DesignId==cmd.designId);
                if(!string.IsNullOrEmpty(cmd.designId)&&basisDesign==null)return;
                if(basisDesign==null)
                {
                    if(!CountryRules.CanChoose(seat.nation,(ChassisKind)cmd.kind))return;
                    basisDesign=new RobotDesign("base",new int[RobotDesign.ValueCount],(ChassisKind)cmd.kind,seat.nation);
                }
                if(basisDesign.Kind!=(ChassisKind)cmd.kind)return;
                if(!seat.designs.TryCreate(basisDesign,cmd.levels,ChassisCost(developmentStatCost,basisDesign.Kind),ChassisCost(developmentAptitudeCost,basisDesign.Kind),out RobotDesign created))
                {seat.report="開発CPが不足、または変更がありません。";return;}
                SpawnNationDesign(created,position,seat.slot);seat.report="新しい設計と試作機を作成しました。";
            }
        }

        private bool ValidLevels(int[] values,ChassisKind kind)
        {
            if((int)kind<0||(int)kind>5||values==null||values.Length!=RobotDesign.ValueCount)return false;
            for(int i=0;i<values.Length;i++)if(values[i]<0||values[i]>(i<4?10000:MaxAptitudeUpgrade(kind,i)))return false;
            return true;
        }

        private void StartLanBattle()
        {
            LanMatchStarted=true;
            foreach(var player in peers.Values.OrderBy(p=>p.slot))
            {
                Vector3 corner=SeatCorner(player.slot);
                var home=SpawnUnit("P"+(player.slot+1)+" "+player.nation+"国 拠点",BattleTeam.Enemy,true,whiteSprite,corner,baseStats,1.5f);
                home.SetNation(player.nation);home.SetOwnership(player.slot,player.slot==localSeat);ApplyNationColor(home);bases.Add(home);
                for(int i=0;i<Mathf.Clamp(initialRobotCount,1,maxPlayerRobots);i++)
                    if(FindNationDeploymentPosition(player.nation,out Vector3 pos,player.slot))SpawnNationDesign(player.initial,pos,player.slot);
            }
            initialSetup=false;CloseDraft();ShowPreparationPage(PreparationPage.Hidden);
            networkCountdownEnd=Time.unscaledTime+3*Mathf.Max(.1f,countdownStepSeconds)+Mathf.Max(.1f,battleStartMessageSeconds);
            countingDown=true;
            mapCamera.transform.position=SeatCorner(localSeat)+new Vector3(0,0,-10);
        }

        private Vector3 SeatCorner(int slot)
        {
            float edge=Mathf.Max(1,halfSize-Mathf.Clamp(cornerInset,2,halfSize-1));
            return new Vector3(PlayerSeats.X(slot)*edge,PlayerSeats.Y(slot)*edge,0);
        }

        private void TickLan()
        {
            if(net==null||!net.IsListening)
            {finished=true;foreach(var u in units)if(u!=null&&u.Movement!=null)u.Movement.Stop();
                result="通信が終了しました。対戦をやり直してください。";ShowLanWaiting(result);return;}
            if(net.IsServer)
            {
                if(peers.TryGetValue(net.LocalClientId,out var host)&&!string.IsNullOrEmpty(host.report))
                {feedback=host.report;host.report="";}
                if(countingDown&&Time.unscaledTime>=networkCountdownEnd)
                {countingDown=false;battleStarted=true;countdownOverlay.SetActive(false);nextSpawn=Time.time+Mathf.Max(.1f,spawnInterval);}
                UpdateLanPresentation(peers.Values.Select(p=>new PlayerInfo{seat=p.slot,nation=(int)p.nation,ready=p.ready}).ToArray(),countingDown?networkCountdownEnd-Time.unscaledTime:-1);
                if(Time.unscaledTime>=nextNetworkSend)
                {
                    nextNetworkSend=Time.unscaledTime+.1f;
                    foreach(var id in net.ConnectedClientsIds.ToArray())if(id!=net.LocalClientId)SendSnapshot(id);
                }
            }
            else if(!receivedSnapshot&&Time.unscaledTime>=nextHello)
            {nextHello=Time.unscaledTime+1;SendCommand(new PlayerOrder{op="hello"});}
            else if(Time.unscaledTime-lastSnapshotTime>10)
            {ShowLanWaiting("ホストからの更新を待っています…");}
        }

        private void UpdateLanPresentation(PlayerInfo[] players,float countdown)
        {
            if(finished){lanWaitOverlay.SetActive(false);return;}
            if(countdown>=0)
            {
                lanWaitOverlay.SetActive(false);ShowPreparationPage(PreparationPage.Hidden);
                countdownOverlay.SetActive(true);countdownOverlay.transform.SetAsLastSibling();
                int number=Mathf.CeilToInt((countdown-battleStartMessageSeconds)/Mathf.Max(.1f,countdownStepSeconds));
                countdownText.text=number>0?Mathf.Clamp(number,1,3).ToString():"戦闘スタート";
            }
            else if(!battleStarted&&localReady)
            {
                // 自分の準備が済んでも、4人全員が完了するまでは戦闘を始めません。
                int readyCount=players.Count(p=>p.ready);
                string members=string.Join("\n",players.OrderBy(p=>p.seat<0?4:p.seat).Select(p=>
                    (p.seat<0?"国家を選択中":"P"+(p.seat+1)+" "+PlayerSeats.Corner(p.seat)+" "+(Nation)p.nation+"国")
                    +(p.seat==localSeat?"（あなた）":"")+(p.ready?"：準備完了":"：準備中")));
                for(int i=players.Length;i<4;i++)members+="\n未参加：接続を待っています";
                ShowLanWaiting("あなたの準備は完了しました\nほかのプレイヤーの準備完了を待っています\n"
                    +"準備完了 "+readyCount+" / 4人　接続 "+players.Length+" / 4人\n"
                    +members+"\n全員の準備完了後、自動でカウントダウンを開始します。");
            }
            else lanWaitOverlay.SetActive(false);
        }
        private void ShowLanWaiting(string message)
        {
            lanWaitText.text=message;lanWaitOverlay.SetActive(true);lanWaitOverlay.transform.SetAsLastSibling();
        }

        private void SendSnapshot(ulong client)
        {
            if(!peers.TryGetValue(client,out Seat seat))return;
            var snapshot=new BattleState{
                map=(int)battleMap,mapSize=mapSize,sandWidth=sandStripWidth,riverWidth=riverWidth,mapConfirmed=mapConfirmed,
                seat=seat.slot,nation=(int)seat.nation,cp=seat.designs?.CP??startingBattleCP,
                started=battleStarted,finished=finished,result=result,ready=seat.ready,feedback=seat.report,
                countdown=countingDown?Mathf.Max(0,networkCountdownEnd-Time.unscaledTime):-1,nextSpawn=Mathf.Max(0,nextSpawn-Time.time),
                boxes=GetHealthBoxes(),
                players=peers.Values.Select(p=>new PlayerInfo{seat=p.slot,nation=(int)p.nation,ready=p.ready}).ToArray(),
                designs=seat.designs?.Designs.Select(DesignInfo.From).ToArray(),
                units=units.Where(u=>u!=null&&u.Alive).Select(u=>new UnitReport{id=u.NetworkId,seat=u.OwnerSlot,nation=(int)u.Country,hp=u.HP,role=u.IsBase?1:u==boss?2:u.Design==null?3:0,
                    x=u.transform.position.x,y=u.transform.position.y,name=u.name,stats=u.CurrentSettings(),design=u.Design==null?null:DesignInfo.From(u.Design),
                    attackSerial=u.AttackSerial,hitX=u.LastHit.x,hitY=u.LastHit.y,
                    moving=u.Movement!=null&&u.Movement.IsMoving,moveX=u.Movement!=null?u.Movement.Destination.x:0,moveY=u.Movement!=null?u.Movement.Destination.y:0}).ToArray()};
            SendJson(StateChannel,client,JsonUtility.ToJson(snapshot));
            seat.report="";
        }

        private void OnLanState(ulong sender,FastBufferReader reader)
        {
            if(net.IsServer||sender!=NetworkManager.ServerClientId)return;
            try
            {
                reader.ReadValueSafe(out string json);var state=JsonUtility.FromJson<BattleState>(json);
                lastSnapshotTime=Time.unscaledTime;receivedSnapshot=true;
                ApplyNetworkMap(state);
                bool firstReady=!localReady&&state.ready;
                localSeat=state.seat;if(state.ready&&state.nation>0)playerNation=(Nation)state.nation;
                localReady=state.ready;
                if(state.designs!=null&&state.designs.Length>0)
                {
                    string chosen=catalog.Designs[Mathf.Clamp(chosenDesign,0,catalog.Designs.Count-1)].DesignId;
                    catalog=DesignList.FromNetwork(state.cp,state.designs.Select(d=>d.ToDesign()).ToArray());
                    chosenDesign=Mathf.Max(0,catalog.Designs.ToList().FindIndex(d=>d.DesignId==chosen));
                }
                if(firstReady){initialSetup=false;CloseDraft();ShowPreparationPage(PreparationPage.Hidden);mapCamera.transform.position=SeatCorner(localSeat)+new Vector3(0,0,-10);}
                bool wasFinished=finished;
                battleStarted=state.started;countingDown=state.countdown>=0;finished=state.finished;result=state.result;nextSpawn=Time.time+state.nextSpawn;
                if(!string.IsNullOrEmpty(state.feedback))feedback=state.feedback;
                var live=new HashSet<int>();
                foreach(var data in state.units)
                {
                    live.Add(data.id);
                    if(!replicatedUnits.TryGetValue(data.id,out var unit))
                    {
                        Sprite sprite=data.role==1?whiteSprite:data.role==2?bossSprite:data.role==3?smallEnemySprite:SpriteFor((ChassisKind)data.design.kind);
                        unit=SpawnUnit(data.name,BattleTeam.Enemy,data.role==1,sprite,new Vector3(data.x,data.y),data.stats,data.role==2?2.2f:data.role==1?1.5f:data.design!=null&&(ChassisKind)data.design.kind==ChassisKind.Giant?2.4f:1.2f,data.role==0||data.role==3);
                        if(unit.Movement!=null)unit.Movement.enabled=false; // Host owns simulation; clients only display snapshots.
                        if(data.design!=null){unit.SetDesign(data.design.ToDesign());if(sprite==null)BuildChassisPlaceholder(unit,unit.Design.Kind);}
                        unit.SetNation((Nation)data.nation);unit.SetOwnership(data.seat,PlayerSeats.CanControl(data.seat,localSeat));unit.NetworkId=data.id;
                        if(data.seat>=0)ApplyNationColor(unit);
                        if(data.role==2)boss=unit;
                        if(data.role==1)bases.Add(unit);
                        replicatedUnits[data.id]=unit;
                    }
                    if(data.attackSerial>unit.AttackSerial)StartCoroutine(ShowSmoke(new Vector3(data.hitX,data.hitY)));
                    unit.AttackSerial=data.attackSerial;
                    unit.ApplyReplica(data.stats,data.hp,new Vector3(data.x,data.y));
                    if(unit.Movement!=null)
                    {
                        if(data.moving)unit.Movement.SetDestination(new Vector3(data.moveX,data.moveY));
                        else unit.Movement.Stop();
                    }
                }
                foreach(int id in replicatedUnits.Keys.Where(id=>!live.Contains(id)).ToArray())
                {
                    var unit=replicatedUnits[id];units.Remove(unit);bases.Remove(unit);if(selected==unit)selected=null;
                    if(unit==boss)boss=null;Destroy(unit.gameObject);replicatedUnits.Remove(id);
                }
                ApplyHealthBoxes(state.boxes);
                UpdateLanPresentation(state.players,state.countdown);
                if(battleStarted)countdownOverlay.SetActive(false);
                if(finished&&!wasFinished){ShowBattleResult();resultText.text=result;}
            }
            catch(Exception error){Debug.LogWarning("LAN state error: "+error.Message);}
        }

        private void OnPeerDisconnected(ulong id)
        {
            if(net==null||!net.IsServer)return;
            peers.Remove(id);seatOrder.Release(id);
            if(LanMatchStarted&&!finished)EndBattle("接続が切れたため対戦を中止しました。勝敗は確定しません。");
        }
        private void StopLan()
        {
            if(net==null)return;
            net.OnClientDisconnectCallback-=OnPeerDisconnected;
            if(net.CustomMessagingManager!=null)
            {net.CustomMessagingManager.UnregisterNamedMessageHandler(CommandChannel);net.CustomMessagingManager.UnregisterNamedMessageHandler(StateChannel);}
            LanMatchStarted=false;
        }
    }
}
