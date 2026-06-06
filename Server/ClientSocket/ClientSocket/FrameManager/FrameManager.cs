using ClientSocket;
using ClientSocket.Tools;
using ClientSocket.UDP;
using GameMessage;
using GamePlayer;
using GameSystem;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.InteropServices;
using ClientSocket.Physics;
using ClientSocket.Physics.Colliders;
using ClientSocket.ServerPlayer;
using GameMessage;
using Vector3 = ClientSocket.Tools.Vector3;

public class FrameManager
{
    public int FixedFrameRate =15;
    public float FixedDeltaTime= 0;
    private static long CurrentLogicFrame=DelayBufferFrames + 1;
    private float accumulatedFrameTime=0;
    private bool shouldOpenLogic = false;
    public const int DelayBufferFrames = 2;
    public ConcurrentDictionary<long, ConcurrentDictionary<int, ClientInput>> frameInputBuffer = new();
    public ConcurrentDictionary<int,long> SendTimeBuffer = new();
    private List<ServerInputAndStateData> inputs=new List<ServerInputAndStateData>();
    private DateTime lastTime=DateTime.Now;
    private float MoveSpeed = 2f;
    private int timespan = 1000 / 15;
    public FrameManager(int fixedFrameRate)
    {
        FixedFrameRate = fixedFrameRate;
        FixedDeltaTime = 1f / FixedFrameRate;
        shouldOpenLogic=true;

        ThreadPool.QueueUserWorkItem(Update);
        
    }
    private void Update(object obj)
    {
        while (shouldOpenLogic)
        {
                // 计算当前距离上一帧过了多久
                var elapsedMs = (DateTime.Now - lastTime).TotalMilliseconds;
                
                if (elapsedMs < timespan)
                {
                    int sleepMs = timespan - (int)elapsedMs;
                    Thread.Sleep(1);
                    continue;
                }
                
                lastTime = DateTime.Now;
                try{
                // 延迟缓冲：当前推进到帧N，但实际执行帧N-2
                long executeFrame = ReadLogicFrame() - DelayBufferFrames;
                
                
                ServerFrameAuthenMsg serverLogicFrame = new ServerFrameAuthenMsg();
                serverLogicFrame.serLogicFrame = executeFrame;
                
                CollectPlayerInputs(executeFrame);
                
                PhysicsWorld.Instance.Tick();
                serverLogicFrame.ServerInputStateData = inputs.Count > 0 ? inputs : new List<ServerInputAndStateData>();
                
                MainClass.udpserver.BroadCastMsg(serverLogicFrame, E_UDP_MSG_TYPE.ORDER_STEADY);
                
                foreach (var v in SendTimeBuffer)
                {
                    UDPPingMsg udpPingMsg = new UDPPingMsg();
                    udpPingMsg.playerId = v.Key;
                    udpPingMsg.SendTime = v.Value;
                    MainClass.udpserver.SendMessage(udpPingMsg,
                        MainClass.udpserver.GetIPEndPointFromClientDic(MainClass.udpserver.ClientPID_TO_Addr_Dic[v.Key])
                        ,E_UDP_MSG_TYPE.SIMPLE);
                }
                
                List<long> removeList=new List<long>();
                foreach (var frame in frameInputBuffer)
                {
                    if (frame.Key <= executeFrame)
                    {
                        removeList.Add(frame.Key);
                    }
                }

                foreach (var v in removeList)
                {
                    frameInputBuffer.TryRemove(v, out _);
                }
                inputs.Clear();
                Interlocked.Increment(ref CurrentLogicFrame);
                lastTime = DateTime.Now;
                    
                }
                catch (Exception e) {
            
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine(e);
                    lastTime=DateTime.Now;
                }
        }
    }


    public void CollectPlayerInputs(long executeFrame)
    {
                // 收集 executeFrame 的所有玩家输入
               // List<long> sortedFrameKeys = frameInputBuffer.Keys.ToList();
               //  sortedFrameKeys.Sort();
               //  if(sortedFrameKeys.Count>0)
               //  if (sortedFrameKeys[^1]<executeFrame)
               //  {
                    // Console.WriteLine($"警告：当前执行帧{executeFrame}的输入还没有到齐," +
                    //                   $" 当前缓冲区最大帧是{sortedFrameKeys[^1]}");
                // }
                var frameInputs = new Dictionary<int, ClientInput>();
                if (frameInputBuffer.ContainsKey(executeFrame))
                {
                    
                    foreach (var kvp in frameInputBuffer[executeFrame])
                    {
                        frameInputs[kvp.Key] = kvp.Value;
                    }
                }
                
                // 补空输入：遍历所有在线玩家，没到齐的补空输入
                foreach (var client in MainClass.udpserver.UDP_Client_Dic.Values)
                {
                    if (!frameInputs.ContainsKey(client.playerID) && client.playerID != -1)
                    {
                        frameInputs[client.playerID] = new ClientInput
                        {
                            playerId = client.playerID,
                            predictFrame = executeFrame,
                            input = new InputData
                            {
                                Horizontal = 0,
                                Vertical = 0,
                                Jump = false,
                            }
                        };
                    }
                }
                
                //加入该帧所有玩家的输入
                foreach (var kvp in frameInputs)
                {
                    inputs.Add(CalcPlayerState(kvp.Value));
                }
    }
    public ServerInputAndStateData CalcPlayerState(ClientInput msg)
    {
        try
        {
            ServerInputAndStateData playerStateAndInput = new ServerInputAndStateData();
            playerStateAndInput.playerId = msg.playerId;
            playerStateAndInput.inputdata = msg.input;

            PlayerStateData playerStateData;

            if (PlayerManager.Instance.player_Dic.ContainsKey(msg.playerId))
            {
                    playerStateData = PlayerManager.Instance.player_Dic[msg.playerId].playerState;
            }
            else
            {
                playerStateData = new PlayerStateData();
                playerStateData.hp = 100;
                playerStateData.playerPos = new PlayerPosData();
                playerStateAndInput.playerstate = playerStateData;
                return playerStateAndInput;
            }

            playerStateData.hp += 1;
            
            Vector3 dirPos = new Vector3(
                msg.input.Horizontal,
                msg.input.Jump ? 1 : 0,
                msg.input.Vertical
            );
            
            Vector3 beginPos = new Vector3(playerStateData.playerPos);
            
            UpdateMove(ref beginPos, dirPos, FixedDeltaTime);
            
            playerStateData.playerPos = beginPos.ToPlayerPosData();
            playerStateAndInput.playerstate = playerStateData;
            Vector3 BoxColliderSize;
            Player player = PlayerManager.Instance.GetPlayer(msg.playerId);
            if (player!=null)
            {
                player.SetPosition(beginPos);
                player.SetState(playerStateData);
            }
            return playerStateAndInput;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            ServerInputAndStateData playerStateAndInput = new ServerInputAndStateData();
            playerStateAndInput.playerstate = new PlayerStateData();
            playerStateAndInput.playerstate.playerPos = new PlayerPosData();
            return playerStateAndInput;
        }
    }
    public long ReadLogicFrame()
    {
        return Interlocked.Read(ref CurrentLogicFrame);
    }
    public void UpdateMove(ref Vector3 logicPos, Vector3 dir, float FixedDeltaTime)
    {

        if (dir.x == 0 && dir.y == 0 && dir.z == 0)
            return;
        Vector3 dirNormalized = new Vector3(
            dir.x, dir.y, dir.z
        );

        if (dirNormalized.x * dirNormalized.x + dirNormalized.z * dirNormalized.z > 0.0001f)
        {
            float magnitude = MathF.Sqrt(
                dirNormalized.x * dirNormalized.x +
                dirNormalized.y * dirNormalized.y +
                dirNormalized.z * dirNormalized.z
            );
            dirNormalized.x /= magnitude;
            dirNormalized.y /= magnitude;
            dirNormalized.z /= magnitude;
        }

       
        Vector3 newPos = logicPos + dirNormalized * MoveSpeed * FixedDeltaTime;
        
        logicPos = FixFloat(newPos);
    }
    public Vector3 FixFloat(Vector3 pos)
    {
        int precision = 1000;
        
        return new Vector3((float)Math.Round(pos.x * precision)/precision, (float)Math.Round(pos.y * precision) / precision, (float)Math.Round(pos.z * precision) / precision);
    }

}
