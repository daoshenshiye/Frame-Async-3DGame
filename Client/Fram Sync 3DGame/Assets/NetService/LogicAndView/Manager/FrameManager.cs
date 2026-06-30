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
    private static FrameManager instance;
    public static FrameManager Instance => instance;
    public int Counter;
    public double CurrentRTT;
    private float serverframeSeconds = 1f / 15f;
    private int serverframeMs = 1000 / 15;
    private Dictionary<long,Vector3> PredictPlayerInput=new Dictionary<long,Vector3>();
    private float rollback_tolerance = 1.3f;
    private int HistoryCount = 80;

    public int DelayPredictFrame = 1;
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
                // print("当前预测帧"+LocalPredictLogicFrame);
            }

        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Debug.LogError(e.StackTrace);
            throw;
        }
    }

    private void UpdateView()
    {
        try
        {
            if (LogicViewBridge.Instance.GetPlayerLogicAndView(PlayerManager.LocalPlayerID)!=null)
            {
                LogicViewBridge.Instance.GetPlayerLogicAndView(PlayerManager.LocalPlayerID).view.UpdateView(Localinput, serverframeSeconds);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Debug.LogError(e.StackTrace);
            throw;
        }
    }
    
    private void SaveInputHistory()
    {
        PredictPlayerInput[LocalPredictLogicFrame] = Localinput;
    }
    private void CleanHistory()
    {
        try
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
        catch (Exception e)
        {
            Debug.LogError(e);
            Debug.LogError(e.StackTrace);
            throw;
        }
    }
    private void RollBack(ServerFrameAuthenMsg msg)
    {
        try
        {
            long serverFrame = msg.serLogicFrame;

            if (CheckStateCorrect(msg))
            {
                return;
            }

            print("执行了回滚");
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
        catch (Exception e)
        {
            Debug.LogError(e);
            Debug.LogError(e.StackTrace);
            throw;
        }
    }

    private bool CheckStateCorrect(ServerFrameAuthenMsg msg)
    {
        try
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
        catch (Exception e)
        {
            Debug.LogError(e);
            Debug.LogError(e.StackTrace);
            throw;
        }
    }
    private void ReplayUnconfirmedInputs(long serverFrame)
    {
        try
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
        catch (Exception e)
        {
            Debug.LogError(e);
            Debug.LogError(e.StackTrace);
            throw;
        }
    }
    
    public void UpdateLogicFrame(ServerFrameAuthenMsg msg)
    {

        try
        {
            if (msg.serLogicFrame <= preServerLogicFrame)
            {
                return;
            }
        
            print("进入了");
            LogicViewBridge.Instance.SyncAllState(msg);
            RollBack(msg);
        
            int rttFrames = Mathf.CeilToInt((float)CurrentRTT/ serverframeMs);
            Debug.LogWarning($"当前RTT: {CurrentRTT:F3}毫秒 ({CurrentRTT:F1}ms), 约{rttFrames}帧");
            LocalPredictLogicFrame = CurrentServerLogicFrame + ServerDelayBuffer + DelayPredictFrame;
        
            CurrentServerLogicFrame = msg.serLogicFrame;
            //LocallogicView?.view.SyncPosWithServer();
        
            preServerLogicFrame = CurrentServerLogicFrame;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Debug.LogError(e.StackTrace);
            throw;
        }
        
    }

    private void SendInputMsgToServer()
    {
        try
        {
            if (PlayerManager.LocalPlayerID != -1)
            {
                if (Localinput != Vector3.zero)
                {
                    // UdpManager.Instance.Counter++;
                    print("发送了有效消息");
                    InputMessage inputMessage = new InputMessage();
                    inputMessage.PlayerId = PlayerManager.LocalPlayerID;
            
                    inputMessage.input = new GamePlayer.InputData();
                    inputMessage.input.Horizontal = Localinput.x;
                    inputMessage.input.Vertical = Localinput.z;
                    inputMessage.PredictFrame = LocalPredictLogicFrame;
                    inputMessage.input.Jump = Localinput.y > 0;
                    UdpManager.Instance.UDPSend(inputMessage);
                }
          
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Debug.LogError(e.StackTrace);
            throw;
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
