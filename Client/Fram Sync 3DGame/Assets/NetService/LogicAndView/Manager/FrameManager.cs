using GameMessage;
using GamePlayer;
using GameSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

//重放采用从Queue队列中取元素的方式进行重放
//流程如下: 玩家输入FixedUpdate获取玩家输入并存入Queue队列中,服务器权威帧下来时进行回滚重放,回滚要保证取出队列中的peak,这一步可以让重放时不会重复执行当前帧操作
//,然后进行重放把queue中所有元素都执行一遍
public class FrameManager:MonoBehaviour
{
    private  int FixedFrameRate = 30;
    private float FixedDeltaTime = 0;
    public  float accumulatedFrames = 0;
    public static long CurrentLogicFrame;
    private Vector3 Localinput;
    public int localExecuteTimes;
    public long preLogicFrame=0;
    public bool shouldSendInput=false;
    private DateTime lastUpdateTime30FPS= DateTime.MinValue;
    private DateTime lastUpdateTime15FPS= DateTime.MinValue;
    private static FrameManager instance;
    public static FrameManager Instance => instance;
    public int Counter;
    public double CurrentRTT;
    private DateTime preSendTime = DateTime.MinValue;
    private float serverframeMs = 1f / 15f;
    private Queue<InputData> inputHistory=new Queue<InputData>();
    private Queue<Dictionary<int,PlayerStateData>> stateHistory=new Queue<Dictionary<int,PlayerStateData>>();
    private Queue<List<ServerInputAndStateData>>  clientFrameInputAndStates=new Queue<List<ServerInputAndStateData>>();
    private int HistoryCount = 80;
    //private int netOffset;
    private void Awake()
    {
        instance= this;
        FixedDeltaTime = 1f / FixedFrameRate;
        DontDestroyOnLoad(this);
       
    }

    //以15FPS的速率与服务器15FPS对齐
    private void FixedUpdate()
    {
        if (PlayerManager.LocalPlayerID != -1 && shouldSendInput)
        {
            //TimeSpan elapsed = DateTime.Now - lastUpdateTime30FPS;
            //if (lastUpdateTime30FPS == DateTime.MinValue || elapsed.TotalSeconds >= FixedDeltaTime)
            //{
            //    GetLocalInputs();
            //    SaveLocalInputHistory(CurrentLogicFrame + 4);
            //    SaveStateSnapshot(CurrentLogicFrame + 4);
            //    CleanHistory();

            //    lastUpdateTime30FPS = DateTime.Now;
            //}
            Localinput= InputManager.Instance.GetLocalPlayerInput();
            print(Localinput);
            SaveStateSnapshot();
            CleanHistory();
            UpdateView();
            SendInputMsgToServer();
            // lastUpdateTime15FPS = DateTime.Now;
            // TimeSpan elapsedtime = DateTime.Now - lastUpdateTime15FPS;
            // if (lastUpdateTime15FPS == DateTime.MinValue || elapsedtime.TotalSeconds >= serverframeMs)
            // {
            //
            //  
            // }

        }
        
    }

    private void UpdateView()
    {
        if (LogicViewBridge.Instance.GetPlayerLogicAndView(PlayerManager.LocalPlayerID)!=null)
        {
            LogicViewBridge.Instance.GetPlayerLogicAndView(PlayerManager.LocalPlayerID).view.UpdateView(Localinput, serverframeMs);
        }
    }

    private void SaveStateSnapshot()
    {
        if (Localinput == Vector3.zero)
            return;
        
         List<ServerInputAndStateData> list = new List<ServerInputAndStateData>();
         list=LogicViewBridge.Instance.GetServerInputAndStateData();
         if(list!=null)
         clientFrameInputAndStates.Enqueue(list);      
     
    }
    private void CleanHistory()
    {
        if (clientFrameInputAndStates.Count>=HistoryCount)
        {
            for (int i = 0; i <HistoryCount/2 ; i++)
            {
                clientFrameInputAndStates.TryDequeue(out List<ServerInputAndStateData> list);
            }
        }
    }
    private void RollBack(ServerFrameAuthenMsg msg)
    {
        long serverFrame = msg.serLogicFrame;
        foreach (var item in msg.ServerInputStateData)
        {
            LogicAndView logicAndView = LogicViewBridge.Instance.GetPlayerLogicAndView(item.playerId);
            if (logicAndView == null) continue;

            if (logicAndView.logic.playerId!=PlayerManager.LocalPlayerID)
            {
                print("其他玩家移动了");
                ++Counter;
            }
            if (logicAndView.logic.playerId == PlayerManager.LocalPlayerID)
            {
                print("本地玩家移动了");
                ++Counter;
            }
            Vector3 serverPos = new Vector3(
               item.playerstate.playerPos.x,
               item.playerstate.playerPos.y,
               item.playerstate.playerPos.z
           );

                logicAndView.logic.HP = item.playerstate.hp;
                logicAndView.logic.LogicPos=serverPos;
            
           
        }
        if (Counter == 1)
        {
            print("只有一个玩家移动");
        }
        Counter = 0;
        clientFrameInputAndStates.TryDequeue(out List<ServerInputAndStateData> list);
    }
    private void ReplayUnconfirmedInputs(long serverFrame)
    {
        
        while (clientFrameInputAndStates.TryDequeue(out List<ServerInputAndStateData> list))
        {
            foreach (var v in list)
            {
                if (v.playerId == PlayerManager.LocalPlayerID)
                {
                    print("执行了重放");
                    LogicViewBridge.Instance.GetPlayerLogicAndView(PlayerManager.LocalPlayerID)
                        .logic.UpdateMove(new Vector3(v.inputdata.Horizontal,v.inputdata.Jump?1:0 ,v.inputdata.Vertical),serverframeMs);
                }
            }
        }
    }

    public void UpdateLogicFrame(ServerFrameAuthenMsg msg)
    {
        
        if (msg.serLogicFrame <= preLogicFrame)
        {
            return;
        }
        
        print("进入了");
        if (preSendTime != DateTime.MinValue)
        {
            TimeSpan elapsed = DateTime.Now - preSendTime;
            CurrentRTT = elapsed.TotalMilliseconds;
        }


       
        RollBack(msg);

        CurrentLogicFrame = msg.serLogicFrame;
      
        ExecuteLogic(CurrentLogicFrame);


        ReplayUnconfirmedInputs(msg.serLogicFrame);

        LogicViewBridge.Instance.SyncAllPlayerView();
        //LocallogicView?.view.SyncPosWithServer();
        
        preSendTime = DateTime.Now;

        preLogicFrame = CurrentLogicFrame;
        
    }

    private void SendInputMsgToServer()
    {
        if (TCPManager.Instance.isConnected
         && PlayerManager.LocalPlayerID != -1&&Localinput!=Vector3.zero)
        {
            
            if(Localinput!=Vector3.zero)
            UdpManager.Instance.Counter++;
            print("发送了有效消息");

            InputMessage inputMessage = new InputMessage();
            inputMessage.PlayerId = PlayerManager.LocalPlayerID;

            inputMessage.input = new GamePlayer.InputData();
            inputMessage.input.Horizontal = Localinput.x;
            inputMessage.input.Vertical = Localinput.z;
            
            UdpManager.Instance.UDPSend(inputMessage);
            Localinput=Vector3.zero;   
        }
    }
    public void ExecuteLogic(long LogicFrame)
    {
        if(!InputManager.Instance.playerInputs.ContainsKey(LogicFrame))
        {
            return;
        }
        Dictionary<int, Vector3> otherInput = null;
        otherInput = InputManager.Instance.playerInputs[LogicFrame];
        foreach (var item in otherInput)
        {

           LogicAndView logicAndView= LogicViewBridge.Instance.GetPlayerLogicAndView(item.Key);
            if(logicAndView==null)
            {
                continue;
            }

            logicAndView.logic.UpdateMove(item.Value, serverframeMs);
        }

        InputManager.Instance.RemoveFrameVisitorInput(LogicFrame);
        ++localExecuteTimes;
        print("执行了移动逻辑");
    }
}
