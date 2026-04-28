using GameMessage;
using GamePlayer;
using GameSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;


public class FrameManager:MonoBehaviour
{

    private  int FixedFrameRate = 30;
    private float FixedDeltaTime = 0;
    public  float accumulatedFrames = 0;
    public static long CurrentLogicFrame;
    Vector3 Localinput;
    public int localExecuteTimes;
    public long preLogicFrame=0;
    public bool shouldSendInput=false;
    private DateTime lastUpdateTime30FPS= DateTime.MinValue;
    private DateTime lastUpdateTime15FPS= DateTime.MinValue;
    private static FrameManager instance;
    public static FrameManager Instance => instance;
    
    public int Counter;
    public LogicAndView LocallogicView;
    public double CurrentRTT;
    private DateTime preSendTime = DateTime.MinValue;
    private float serverframeMs = 1f / 15f;
    private Dictionary<long,InputData> inputHistory=new Dictionary<long, InputData>();
    private Dictionary<long,Dictionary<int,PlayerStateData>> stateHistory=new Dictionary<long, Dictionary<int, PlayerStateData>>();
    private int MaxinputSize=100;
    private readonly object _dictLock = new object();
    //private int netOffset;
    private void Awake()
    {
        instance= this;
        FixedDeltaTime = 1f / FixedFrameRate;
        DontDestroyOnLoad(this);
       
    }

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
            GetLocalInputs();
            SaveLocalInputHistory(CurrentLogicFrame + 4);
            SaveStateSnapshot(CurrentLogicFrame + 4);
            CleanHistory();

            TimeSpan elapsedtime = DateTime.Now - lastUpdateTime15FPS;
            if (lastUpdateTime15FPS == DateTime.MinValue || elapsedtime.TotalSeconds >= serverframeMs)
            {
                UpdateView();
                SendInputMsgToServer();
                lastUpdateTime15FPS = DateTime.Now;
             
            }

        }
        
    }
    private void UpdateView()
    {
        if (LocallogicView == null)
        {
            LocallogicView = LogicViewBridge.Instance.GetPlayerLogicAndView(PlayerManager.LocalPlayerID);
        }
        else if (PlayerManager.LocalPlayerID != -1)
        {

            LocallogicView.view.UpdateView(Localinput, serverframeMs);
        }
    }
    private void SaveLocalInputHistory(long serverFrame)
    {
        InputData inputData = new InputData();
        inputData.Horizontal = Localinput.x;
        inputData.Vertical = Localinput.z;
        inputData.playerId = PlayerManager.LocalPlayerID;

        lock (_dictLock)
        {
            if (inputHistory.ContainsKey(serverFrame))
                inputHistory[serverFrame] = inputData;
            else
                inputHistory.Add(serverFrame, inputData);
        }
    }
    private void SaveStateSnapshot(long serverFrame)
    {
        lock (_dictLock)
        {
            Dictionary<int, PlayerStateData> states = new Dictionary<int, PlayerStateData>();
            foreach (var item in LogicViewBridge.Instance.PlayerLV_Dic)
            {
                PlayerStateData playerState = new PlayerStateData();
                playerState.hp = item.Value.logic.HP;
                states.Add(item.Key, playerState);
            }

            if (stateHistory.ContainsKey(serverFrame))
                stateHistory[serverFrame] = states;
            else
                stateHistory.Add(serverFrame, states);
        }
    }
    private void CleanHistory()
    {
        List<long> RemoveList = new List<long>();
        lock (_dictLock)
        {
            foreach (var item in inputHistory)
            {
                if (item.Key < CurrentLogicFrame - MaxinputSize)
                    RemoveList.Add(item.Key);
            }

            foreach (var item in RemoveList)
            {
                inputHistory.Remove(item);
                stateHistory.Remove(item);
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

                logicAndView.view.HP = item.playerstate.hp;
                logicAndView.logic.LogicPos=serverPos;
            
           
        }
        if (Counter == 1)
        {
            print("只有一个玩家移动");
        }
        Counter = 0;
        List<long> Removelist = new List<long>();
        lock (_dictLock)
        {
            var stateKeys = new List<long>(stateHistory.Keys);
            foreach (var item in stateKeys)
            {
                if (serverFrame > item)
                    Removelist.Add(item);
            }

            foreach (var item in Removelist)
                stateHistory.Remove(item);
        }
    }
    private void ReplayUnconfirmedInputs(long serverFrame)
    {

        List<long> allFrames = new List<long>();

        lock (_dictLock)
        {
            allFrames.AddRange(inputHistory.Keys);
        }

        var unconfirmedFrames = new List<long>();
        
        foreach (var frame in allFrames)
        {
            if (frame > serverFrame)
            {
                unconfirmedFrames.Add(frame);
                
            }
               
            
        }

        unconfirmedFrames.Sort();

        foreach (var frame in unconfirmedFrames)
        {
            InputData input = null;
            lock (_dictLock) // 加锁
            {
                inputHistory.TryGetValue(frame, out input);
            }

            if (input != null && LocallogicView != null)
            {
                print("执行了重放");
                LocallogicView.logic.UpdateMove(
                    new Vector3(input.Horizontal, input.Jump ? 1 : 0, input.Vertical),
                    serverframeMs
                );
            }

            SaveStateSnapshot(frame);
        }

    }
    public void GetLocalInputs()
    {

        //if (Localinput.x != 0)
        //{
        //    timer = DateTime.Now;
        //}
        //TimeSpan delta = DateTime.Now - timer;
        //print(delta.TotalSeconds);
        
        Localinput.x = Input.GetAxis("Horizontal");
        Localinput.z = Input.GetAxis("Vertical");

        if (Input.touchCount > 0)
        {
            Localinput.x = 0.6f;
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
        Localinput=Vector3.zero;
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
