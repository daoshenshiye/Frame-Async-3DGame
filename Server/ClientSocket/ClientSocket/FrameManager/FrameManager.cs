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

public class FrameManager
{
    public int FixedFrameRate =15;
    public float FixedDeltaTime= 0;
    private static long CurrentLogicFrame=DelayBufferFrames + 1;
    private float accumulatedFrameTime=0;
    private bool shouldOpenLogic = false;
    public const int DelayBufferFrames = 2;
    //public Dictionary<int,BaseMsg> PlayerFrameInputs= new Dictionary<int, BaseMsg>();
    //public Dictionary<long, List<BaseMsg>> NetInputsDic = new Dictionary<long, List<BaseMsg>>();
    public ConcurrentDictionary<long, ConcurrentDictionary<int, ClientInput>> frameInputBuffer = new();
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
                // Console.WriteLine($"frameInputBuffer == MainClass.frameManager.frameInputBuffer: {ReferenceEquals(frameInputBuffer, MainClass.frameManager?.frameInputBuffer)}");
                lastTime = DateTime.Now;
                try{
                // 延迟缓冲：当前推进到帧N，但实际执行帧N-2
                long executeFrame = ReadLogicFrame() - DelayBufferFrames;
                // Console.WriteLine($"===执行帧{executeFrame}===");
                // Console.WriteLine($"CurrentLogicFrame={CurrentLogicFrame}");
                // Console.WriteLine($"缓冲区所有keys: {string.Join(",", frameInputBuffer.Keys)}");
                ServerFrameAuthenMsg serverLogicFrame = new ServerFrameAuthenMsg();
                serverLogicFrame.serLogicFrame = executeFrame;
                
                // 收集 executeFrame 的所有玩家输入
               List<long> sortedFrameKeys = frameInputBuffer.Keys.ToList();
                sortedFrameKeys.Sort();
                if(sortedFrameKeys.Count>0)
                if (sortedFrameKeys[^1]<executeFrame)
                {
                    Console.WriteLine($"警告：当前执行帧{executeFrame}的输入还没有到齐," +
                                      $" 当前缓冲区最大帧是{sortedFrameKeys[^1]}");
                }
                var frameInputs = new Dictionary<int, ClientInput>();
                if (frameInputBuffer.ContainsKey(executeFrame))
                {
                    // Console.WriteLine($"找到帧{executeFrame}, 里面有{frameInputBuffer[executeFrame].Count}个玩家");
                    foreach (var kvp in frameInputBuffer[executeFrame])
                    {
                        // Console.WriteLine($"  玩家{kvp.Key}: H={kvp.Value.input.Horizontal}, V={kvp.Value.input.Vertical}");
                        frameInputs[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    if (frameInputBuffer.Count > 0)
                    {
                        // Console.WriteLine($"没有找到帧{executeFrame}的输入, 当前缓冲区所有keys: {string.Join(",", frameInputBuffer.Keys)}");
                    }
                }

                // 补空输入：遍历所有在线玩家，没到齐的补空输入
                foreach (var client in MainClass.udpserver.UDP_Client_Dic.Values)
                {
                    if (!frameInputs.ContainsKey(client.playerID)&&client.playerID!=-1)
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
                                ColliderBoxSize = new PlayerPosData { x = 0, y = 0, z = 0 }
                            }
                        };
                        // Console.WriteLine($"  补空输入给玩家{client.playerID}");
                    }
                }
                // Console.WriteLine($"最终执行{frameInputs.Count}个玩家:");
                // 执行该帧的所有玩家输入
                foreach (var kvp in frameInputs)
                {
                    inputs.Add(CalcPlayerState(kvp.Value));
                    // Console.WriteLine($"  玩家{kvp.Key}: H={kvp.Value.input.Horizontal}, V={kvp.Value.input.Vertical}");
                }
                PhysicsWorld.Instance.Tick();
                serverLogicFrame.ServerInputStateData = inputs.Count > 0 ? inputs : new List<ServerInputAndStateData>();
                
                MainClass.udpserver.BroadCastMsg(serverLogicFrame, E_UDP_MSG_TYPE.ORDER_STEADY);
                
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
                    lastTime=DateTime.Now;
                }
                #region 补发帧逻辑
                //lock (MainClass.udpserver.UDP_Client_Dic)
                //{
                //    foreach (var item in MainClass.udpserver.UDP_Client_Dic)
                //    {
                //        if (!playerInputs.ContainsKey(item.Value.playerID))
                //        {
                //            ServerInputAndStateData inputAndStateData = new ServerInputAndStateData();
                //            inputAndStateData.inputdata = new InputData();
                //            inputAndStateData.inputdata.Horizontal = 0;
                //            inputAndStateData.inputdata.Jump = false;
                //            inputAndStateData.inputdata.Vertical = 0;
                //            inputAndStateData.playerId = item.Value.playerID;
                //            playerInputs.Add(item.Value.playerID,inputAndStateData);
                //        }

                //    }
                //}
                //lock (MainClass.udpserver.playerInputsQ_Dic)
                //{
                //    foreach (var item in MainClass.udpserver.playerInputsQ_Dic)
                //    {
                //        if (!playerInputs.ContainsKey(item.Key))
                //        {


                //            if (MainClass.udpserver.playerInputsQ_Dic[item.Key].TryDequeue(out BaseHandler handler))
                //            {
                //                if (handler != null)
                //                {
                //                    ServerInputAndStateData inputAndStateData = new ServerInputAndStateData();
                //                    InputMessage input = handler.msg as InputMessage;
                //                    inputAndStateData.inputdata = input.input;
                //                    inputAndStateData.playerId = input.PlayerId;
                //                    Console.WriteLine("服务器发权威帧前补入输入");
                //                    playerInputs.Add(item.Key, inputAndStateData);
                //                }

                //            }


                //        }
                //    }
                //}
                #endregion 
                
        }
    }
    #region  方案2:帧号发送和输入发送分开
    //private void DealInputsFrame()
    //{
    //    lock (NetInputsDic)
    //    {
    //        List<long> indexs = new List<long>();

    //        foreach (var item in NetInputsDic)
    //        {

    //            if(item.Key>=CurrentLogicFrame)
    //            {
    //                foreach (var item1 in item.Value)
    //                {
    //                    MainClass.udpserver.BroadCastMsg(item1, E_UDP_MSG_TYPE.ORDER_STEADY);
    //                }
    //                indexs.Add(item.Key);
    //            }
    //            if(item.Key< ReadLogicFrame())
    //            {
    //                Console.WriteLine("旧的帧号删除");
    //                indexs.Add(item.Key);
    //            }

    //        }
    //        foreach (var item in indexs)
    //        {
    //            if (NetInputsDic.ContainsKey(item))
    //            {
    //                NetInputsDic.Remove(item);
    //            }
    //        }
    //    }
    //}
    #endregion
    #region 方案一,等待全部玩家的输入都到再发帧号
    //private void Update(object obj)
    //{

    //    while (shouldOpenLogic)
    //    {
    //        try
    //        {
    //            // 一帧正在跑 → 直接跳过
    //            if (_isFrameRunning)
    //            {

    //                continue;
    //            }

    //            int clientCount = MainClass.udpserver.UDP_Client_Dic.Count;
    //            if (clientCount == 0)
    //            {
    //                PlayerFrameInputs.Clear();
    //                lastTime = DateTime.MinValue;

    //                continue;
    //            }

    //            bool allReady = false;
    //            lock (PlayerFrameInputs)
    //            {
    //                allReady = PlayerFrameInputs.Count >= clientCount;
    //            }

    //            if (allReady)
    //            {
    //                _isFrameRunning = true; 

    //                lock (PlayerFrameInputs)
    //                {
    //                    foreach (var item in PlayerFrameInputs)
    //                    {
    //                        MainClass.udpserver.BroadCastMsg(item.Value, E_UDP_MSG_TYPE.ORDER_STEADY);
    //                    }
    //                }

    //                serverLogicFrameMsg serverLogic = new serverLogicFrameMsg();
    //                serverLogic.serLogicFrame = ReadLogicFrame();
    //                MainClass.udpserver.BroadCastMsg(serverLogic, E_UDP_MSG_TYPE.SIMPLE);

    //                Interlocked.Increment(ref CurrentLogicFrame);

    //                lock (PlayerFrameInputs)
    //                {
    //                    PlayerFrameInputs.Clear();
    //                }

    //                lastTime = DateTime.Now;
    //                _isFrameRunning = false; 
    //            }
    //            else
    //            {
    //                NetDelayForceGo();
    //            }

    //        }
    //        catch (Exception e)
    //        {
    //            Console.WriteLine(e.Message);
    //            Console.WriteLine(e.StackTrace);
    //        }

    //    }
    //}
    #endregion
    #region 方案一,玩家网络延迟服务器往前走
    //private void NetDelayForceGo()
    //{
    //    if (MainClass.udpserver.UDP_Client_Dic.Count != 0 && PlayerFrameInputs.Count < MainClass.udpserver.UDP_Client_Dic.Count)
    //    {
    //        if (lastTime == DateTime.MinValue)
    //            lastTime = DateTime.Now;
    //        TimeSpan elapsed = DateTime.Now - lastTime;
    //        if (lastTime != DateTime.MinValue && elapsed.TotalSeconds >= 0.04f)
    //        {
    //            List<int> addList = new List<int>();

    //            if(PlayerFrameInputs.Count < MainClass.udpserver.UDP_Client_Dic.Count)
    //            {
    //                InputMessage inputMessage = new InputMessage();

    //                inputMessage.CurrentLogicFrame = ReadLogicFrame();
    //                lock (MainClass.udpserver.UDP_Client_Dic)
    //                {
    //                    foreach (var item in MainClass.udpserver.UDP_Client_Dic)
    //                    {

    //                        bool shouldAdd = true;
    //                        InputMessage input = new InputMessage();
    //                        lock (PlayerFrameInputs)
    //                            foreach (var item1 in PlayerFrameInputs)
    //                            {

    //                                if (item.Value.playerID == item1.Key)
    //                                {
    //                                    shouldAdd = false;
    //                                    break;
    //                                }
    //                            }


    //                        if (shouldAdd)
    //                        {
    //                            input.PlayerId = item.Value.playerID;
    //                            input.PlayerAddr = "sddf";
    //                            input.input = new GamePlayer.InputData();
    //                            input.input.Horizontal = 0;
    //                            input.input.Vertical = 0;
    //                            input.CurrentLogicFrame = ReadLogicFrame();
    //                            if (!PlayerFrameInputs.ContainsKey(item.Value.playerID))
    //                            {
    //                                PlayerFrameInputs.Add(item.Value.playerID, input);

    //                            }

    //                        }
    //                    }

    //                }
    //            }

    //            lastTime = DateTime.MinValue;
    //        }
    //    }
    //}
    #endregion

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
                Console.WriteLine("非法玩家ID,没有TCP注册");
                playerStateData = new PlayerStateData();
                playerStateData.hp = 100;
                playerStateData.playerPos = new PlayerPosData();
                playerStateAndInput.playerstate = playerStateData;
                playerStateAndInput.inputdata.ColliderBoxSize = new PlayerPosData();
                return playerStateAndInput;
            }

            playerStateData.hp += 1;
            
            Position dirPos = new Position(
                msg.input.Horizontal,
                msg.input.Jump ? 1 : 0,
                msg.input.Vertical
            );
            
            Position beginPos = new Position(playerStateData.playerPos);
            
            UpdateMove(ref beginPos, dirPos, FixedDeltaTime);
            
            playerStateData.playerPos = beginPos.ToPlayerPosData();
            playerStateAndInput.playerstate = playerStateData;
            Position BoxColliderSize;
            if (playerStateAndInput.inputdata.ColliderBoxSize == null)
            {
                Console.WriteLine("碰撞盒大小为空");
            }
            else
            {
                BoxColliderSize= new Position(playerStateAndInput.inputdata.ColliderBoxSize);
                if ((PlayerManager.Instance.GetPlayer(msg.playerId).GetComponent<BoxCollider>() as BoxCollider)!=null)
                {
                    (PlayerManager.Instance.GetPlayer(msg.playerId).GetComponent<BoxCollider>() as BoxCollider).Size = BoxColliderSize;    
                }
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
    public void UpdateMove(ref Position logicPos, Position dir, float FixedDeltaTime)
    {

        if (dir.x == 0 && dir.y == 0 && dir.z == 0)
            return;
        Position dirNormalized = new Position(
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

       
        Position newPos = logicPos + dirNormalized * MoveSpeed * FixedDeltaTime;
        
        logicPos = FixFloat(newPos);
    }
    public Position FixFloat(Position pos)
    {
        int precision = 1000;
        
        return new Position((float)Math.Round(pos.x * precision)/precision, (float)Math.Round(pos.y * precision) / precision, (float)Math.Round(pos.z * precision) / precision);
    }

}
