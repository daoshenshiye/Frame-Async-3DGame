using GameMessage;
using GamePlayer;
using GameSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using Debug = UnityEngine.Debug;
using Update = UnityEngine.PlayerLoop.Update;


public class FrameManager:MonoBehaviour
{
    private  int FixedFrameRate = 30;
    private float FixedDeltaTime = 0;
    public  float accumulatedFrames = 0;
    public  long CurrentServerLogicFrame;
    public int ServerDelayBuffer;
    public  long LocalPredictLogicFrame;
    private Vector3 Localinput;
    public int localExecuteTimes;
    public long preServerLogicFrame=0;
    public bool shouldSendInput=false;
    private DateTime lastUpdateTime30FPS= DateTime.MinValue;
    private DateTime lastUpdateTime15FPS= DateTime.MinValue;
    private static FrameManager instance;
    public static FrameManager Instance => instance;
    public int Counter;
    public double CurrentRTT;
    private float serverframeSeconds = 1f / 15f;
    private int serverframeMs = 1000 / 15;
    private Dictionary<long,Vector3> PredictPlayerInput=new Dictionary<long,Vector3>();
    private float rollback_tolerance = 1.3f;
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
        try
        {
            if (PlayerManager.LocalPlayerID != -1 && shouldSendInput)
            {
                // TimeSpan elapsed = DateTime.Now - lastUpdateTime30FPS;
                // if (lastUpdateTime30FPS == DateTime.MinValue || elapsed.TotalSeconds >= FixedDeltaTime)
                // {
                //     lastUpdateTime30FPS = DateTime.Now;
                // }
                Localinput= InputManager.Instance.GetLocalPlayerInput();

                UpdateView();
                SaveInputHistory();
                CleanHistory();
                SendInputMsgToServer();
                Localinput=Vector3.zero;
                ++LocalPredictLogicFrame;
                print("当前预测帧"+LocalPredictLogicFrame);
            }
        }
        catch (Exception e)
        {
            print(e.Message);
            print(e.StackTrace);
        }

        
    }

    private void UpdateView()
    {
        if (LogicViewBridge.Instance.GetPlayerLogicAndView(PlayerManager.LocalPlayerID)!=null)
        {
            LogicViewBridge.Instance.GetPlayerLogicAndView(PlayerManager.LocalPlayerID).view.UpdateView(Localinput, serverframeSeconds);
        }
    }
    
    private void SaveInputHistory()
    {
        PredictPlayerInput[LocalPredictLogicFrame] = Localinput;
    }
    private void CleanHistory()
    {
        if (PredictPlayerInput.Count>=HistoryCount)
        {
            List<long> sortedKeys =new List<long>();
            sortedKeys.AddRange(PredictPlayerInput.Keys.ToList());
            sortedKeys.Sort();
            for (int i=0;i<sortedKeys.Count/2;i++)
            {
                PredictPlayerInput.Remove(sortedKeys[i]);
            }
        }
    }
    private void RollBack(ServerFrameAuthenMsg msg)
    {
        long serverFrame = msg.serLogicFrame;

        if (CheckStateCorrect(msg))
        {
            return;
        }

        print("执行了重放");
        foreach (var item in msg.ServerInputStateData)
        {
            if (item.playerId==PlayerManager.LocalPlayerID)
            {
                var logicView = LogicViewBridge.Instance.GetPlayerLogicAndView(item.playerId);
                if (logicView == null) break;
            
                Vector3 serverPos = new Vector3(
                    item.playerstate.playerPos.x,
                    item.playerstate.playerPos.y,
                    item.playerstate.playerPos.z
                );
                logicView.logic.LogicPos=serverPos;
                logicView.view.HP = item.playerstate.hp;
                ReplayUnconfirmedInputs(serverFrame);
                break;
            }
        }
    }

    private bool CheckStateCorrect(ServerFrameAuthenMsg msg)
    {
        foreach (var item in msg.ServerInputStateData)
        {
            if (item.playerId==PlayerManager.LocalPlayerID)
            {
                var playerPos = LogicViewBridge.Instance.GetPlayerLogicAndView(PlayerManager.LocalPlayerID).logic.LogicPos;
                var serverPos = item.playerstate.playerPos;
                Vector3 serverVector3=new Vector3(serverPos.x,serverPos.y,serverPos.z);
              float dist=  Vector3.Distance(serverVector3, playerPos);
              return dist <=rollback_tolerance;
            }
        }
        return true;
    }
    private void ReplayUnconfirmedInputs(long serverFrame)
    {
        var confirmedKeys = PredictPlayerInput.Keys.Where(k => k <= serverFrame).ToList();
        foreach (var key in confirmedKeys)
        {
            PredictPlayerInput.Remove(key);
        }
        foreach (var input in PredictPlayerInput)
        {
            if (input.Key>serverFrame)
            {
                print("执行了重放");
                
                LogicViewBridge.Instance.GetPlayerLogicAndView(PlayerManager.LocalPlayerID)
                    .logic.UpdateMove(input.Value,serverframeSeconds);
            }
        }
    }
    
    public void UpdateLogicFrame(ServerFrameAuthenMsg msg)
    {
        
        if (msg.serLogicFrame <= preServerLogicFrame)
        {
            return;
        }
        
        print("进入了");
        int rttFrames = Mathf.CeilToInt((float)(CurrentRTT * 1000) / serverframeMs);
        Debug.LogWarning($"当前RTT: {CurrentRTT:F3}秒 ({CurrentRTT*1000:F1}ms), 约{rttFrames}帧");
        long targetPredict = CurrentServerLogicFrame + rttFrames +1;
        LocalPredictLogicFrame = targetPredict;
        LogicViewBridge.Instance.SyncAllState(msg);
        RollBack(msg);
        
        CurrentServerLogicFrame = msg.serLogicFrame;
        //LocallogicView?.view.SyncPosWithServer();
        
        preServerLogicFrame = CurrentServerLogicFrame;
        
    }

    private void SendInputMsgToServer()
    {
        if (TCPManager.Instance.isConnected
         && PlayerManager.LocalPlayerID != -1)
        {
            
            if(Localinput!=Vector3.zero)
            UdpManager.Instance.Counter++;
            print("发送了有效消息");
            InputMessage inputMessage = new InputMessage();
            inputMessage.PlayerId = PlayerManager.LocalPlayerID;
            
            inputMessage.input = new GamePlayer.InputData();
            inputMessage.input.Horizontal = Localinput.x;
            inputMessage.input.Vertical = Localinput.z;
            Vector3 size;
            if (PlayerManager.Instance.GetPlayerInfo(PlayerManager.LocalPlayerID)!=null)
            {
                size= PlayerManager.Instance.GetPlayerInfo(PlayerManager.LocalPlayerID)
                    .player_instance.GetComponent<BoxCollider>().size;      
            }
            else
            {
                size=Vector3.zero;
                print("无法获取玩家信息");
            }
          
           inputMessage.input.ColliderBoxSize = new PlayerPosData();
           inputMessage.input.ColliderBoxSize.x = size.x;
           inputMessage.input.ColliderBoxSize.y = size.y;
           inputMessage.input.ColliderBoxSize.z = size.z;
           inputMessage.PredictFrame = LocalPredictLogicFrame;
           UDPPingMsg pingMsg = new UDPPingMsg();
           pingMsg.SendTime = Stopwatch.GetTimestamp();
            UdpManager.Instance.UDPSend(inputMessage);
            UdpManager.Instance.UDPSend(pingMsg,E_UDP_MSG_TYPE.SIMPLE);
        }
    }
    
    #region 执行输入.目前版本网络同步不需要客户端计算位置
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

            logicAndView.logic.UpdateMove(item.Value, serverframeSeconds);
        }

        InputManager.Instance.RemoveFrameVisitorInput(LogicFrame);
        ++localExecuteTimes;
        print("执行了移动逻辑");
    }
    #endregion
  
}
