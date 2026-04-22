using ClientSocket;
using ClientSocket.Tools;
using ClientSocket.UDP;
using GameMessage;
using GamePlayer;
using GameSystem;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

using System.Numerics;
using System.Runtime.InteropServices;



public class FrameManager
{
    public int FixedFrameRate =15;
    public float FixedDeltaTime= 0;
    private static long CurrentLogicFrame=1;
   
    private bool shouldOpenLogic = false;
    //public Dictionary<int,BaseMsg> PlayerFrameInputs= new Dictionary<int, BaseMsg>();
    //public Dictionary<long, List<BaseMsg>> NetInputsDic = new Dictionary<long, List<BaseMsg>>();
    public System.Collections.Concurrent.ConcurrentDictionary<int, ConcurrentQueue<ServerInputAndStateData>> playerInputs =
        new System.Collections.Concurrent.ConcurrentDictionary<int, ConcurrentQueue<ServerInputAndStateData>>();
    private List<ServerInputAndStateData> inputs=new List<ServerInputAndStateData>();
    private DateTime lastTime = DateTime.MinValue;
    private float MoveSpeed = 2f;
    private int timespan = 1000 / 15;
    public FrameManager()
    {
        FixedDeltaTime = 1f / FixedFrameRate;
        shouldOpenLogic=true;

        ThreadPool.QueueUserWorkItem(Update);
        
    }
    private void Update(object obj)
    {
        while (shouldOpenLogic)
        {
            try
            {

                Thread.Sleep(timespan);
                ServerFrameAuthenMsg serverLogicFrame = new ServerFrameAuthenMsg();
                    serverLogicFrame.serLogicFrame = ReadLogicFrame();
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
                 foreach (var item in playerInputs)
                    {
                        var q = item.Value;
                        if (q != null && q.TryDequeue(out ServerInputAndStateData serverInputAndState))
                        {
                            inputs.Add(CalcPlayerState(serverInputAndState));
                        }
                    }
                    if (inputs.Count == 0)
                    {
                        serverLogicFrame.ServerInputStateData = new List<ServerInputAndStateData>();

                    }
                    else
                    {

                        serverLogicFrame.ServerInputStateData = inputs;
                    }
                //foreach (var item in inputs)
                //{
                //    if (item.playerId==1)
                //    {
                       
                //        ++MainClass.udpserver. Counter;
                //        Console.WriteLine(MainClass.udpserver.Counter);
                //    }
                    
                //}
                MainClass.udpserver.BroadCastMsg(serverLogicFrame, E_UDP_MSG_TYPE.ORDER_STEADY);
                    inputs.Clear();
                    Interlocked.Increment(ref CurrentLogicFrame);
                    lastTime = DateTime.Now;
                
            }
            catch (Exception e) {
            
            Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            

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

    public ServerInputAndStateData CalcPlayerState(ServerInputAndStateData msg)
    {
        try
        {
            ServerInputAndStateData playerStateAndInput = new ServerInputAndStateData();
            playerStateAndInput.playerId = msg.playerId;
            playerStateAndInput.inputdata = msg.inputdata;

            PlayerStateData playerStateData;


            if (MainClass.udpserver.ClientPID_TO_Addr_Dic.ContainsKey(msg.playerId))
            {
                UDPClient clientInfo;
                lock (MainClass.udpserver.UDP_Client_Dic)
                {
                    if (MainClass.udpserver.UDP_Client_Dic.ContainsKey(MainClass.udpserver.ClientPID_TO_Addr_Dic[msg.playerId]))
                    {
                        clientInfo = MainClass.udpserver.UDP_Client_Dic[MainClass.udpserver.ClientPID_TO_Addr_Dic[msg.playerId]];
                        if (clientInfo.playerStateData==null)
                        {
                            playerStateAndInput.playerstate = new PlayerStateData();
                            playerStateAndInput.playerstate.playerPos = new PlayerPosData();
                            return playerStateAndInput;
                        }
                    }
                    else
                    {
                        playerStateAndInput.playerstate = new PlayerStateData();
                        playerStateAndInput.playerstate.playerPos = new PlayerPosData();
                        return playerStateAndInput;
                    }

                }

                playerStateData = clientInfo.playerStateData;


                playerStateData.hp += 1;


                PlayerPos dirPos = new PlayerPos(
                    msg.inputdata.Horizontal,
                    msg.inputdata.Jump ? 1 : 0,
                    msg.inputdata.Vertical
                );


                PlayerPos beginPos = new PlayerPos(playerStateData.playerPos);


                UpdateMove(ref beginPos, dirPos, FixedDeltaTime);


                playerStateData.playerPos = beginPos.ToPlayerPosData();


                clientInfo.playerStateData = playerStateData;
            }
            else
            {

                playerStateData = new PlayerStateData();
                playerStateData.hp = 100;
                playerStateData.playerPos = new PlayerPosData();
            }


            playerStateAndInput.playerstate = playerStateData;
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
    public void UpdateMove(ref PlayerPos logicPos, PlayerPos dir, float FixedDeltaTime)
    {

        if (dir.x == 0 && dir.y == 0 && dir.z == 0)
            return;

        
        PlayerPos dirNormalized = new PlayerPos(
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

       
        PlayerPos newPos = logicPos + dirNormalized * MoveSpeed * FixedDeltaTime;

       
        logicPos = FixFloat(newPos);
    }
    public PlayerPos FixFloat(PlayerPos pos)
    {
        int precision = 1000;
        
        return new PlayerPos((float)Math.Round(pos.x * precision)/precision, (float)Math.Round(pos.y * precision) / precision, (float)Math.Round(pos.z * precision) / precision);
    }

}
