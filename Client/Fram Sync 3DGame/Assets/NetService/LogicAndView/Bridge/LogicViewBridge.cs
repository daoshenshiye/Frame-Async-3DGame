using System;
using System.Collections;
using System.Collections.Generic;
using GamePlayer;
using GameSystem;
using UnityEngine;
public class LogicAndView { 
public PlayerLogic logic;
public PlayerView view;
    public LogicAndView(PlayerLogic logic, PlayerView view)
    {
        this.logic = logic;
        this.view = view;
    }
}

public class LogicViewBridge
{
    private static LogicViewBridge instance;
    public static LogicViewBridge Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new LogicViewBridge();
            }
            return instance;
        }
    }
    
    public  Dictionary<int, LogicAndView> PlayerLV_Dic = new Dictionary<int, LogicAndView>();
    public  void AddPlayer(int playerId,PlayerLogic Plogic,PlayerView Pview)
    {
        if(!PlayerLV_Dic.ContainsKey(playerId))
        {
            LogicAndView logicAndView= new LogicAndView(Plogic,Pview);
            PlayerLV_Dic.Add(playerId, logicAndView);
            Pview.ConnectLogic(Plogic);
        }
       
    }
    public void DeletePlayer(int playerId)
    {
        if(PlayerLV_Dic.ContainsKey(playerId))
        {
            PlayerLV_Dic[playerId].logic = null;
            PlayerLV_Dic[playerId].view = null;
            PlayerLV_Dic.Remove(playerId);
        }
    }

    public List<ServerInputAndStateData> GetServerInputAndStateData()
    {
        List<ServerInputAndStateData> serverInputAndStateData=new List<ServerInputAndStateData>();
        foreach (var V in PlayerLV_Dic)
        {
            ServerInputAndStateData inputAndStateData=new ServerInputAndStateData();
            if (V.Key==PlayerManager.LocalPlayerID)
            {
               Vector3 input= InputManager.Instance.GetLocalPlayerInput();
               InputData inputData = new InputData();
               inputData.Horizontal = input.x;
               inputData.Vertical = input.z;
               inputData.Jump = input.y>0?true:false;
               inputData.playerId=V.Key;
               PlayerStateData playerStateData=new PlayerStateData();
               playerStateData.hp = V.Value.logic.HP;
               PlayerPosData PosData=new PlayerPosData();
               PosData.x = V.Value.logic.LogicPos.x;
               PosData.y = V.Value.logic.LogicPos.y;
               PosData.z = V.Value.logic.LogicPos.z;
               playerStateData.playerPos = PosData;
               inputAndStateData.playerId=V.Key;
               inputAndStateData.inputdata = inputData;
               inputAndStateData.playerstate=playerStateData;
                serverInputAndStateData.Add(inputAndStateData);
                
            }
            else
            {
                PlayerStateData playerStateData=new PlayerStateData();
                playerStateData.hp = V.Value.logic.HP;
                PlayerPosData PosData=new PlayerPosData();
                PosData.x = V.Value.logic.LogicPos.x;
                PosData.y = V.Value.logic.LogicPos.y;
                PosData.z = V.Value.logic.LogicPos.z;
                playerStateData.playerPos = PosData;
                inputAndStateData.playerId=V.Key;
                inputAndStateData.inputdata = new InputData();
                inputAndStateData.playerstate=playerStateData;
                serverInputAndStateData.Add(inputAndStateData);
            }
            
        }

        if (serverInputAndStateData.Count == 0)
        {
            return null;
        }
        return serverInputAndStateData;
    }

    public LogicAndView GetPlayerLogicAndView(int playerId)
    {
        if(PlayerLV_Dic.ContainsKey(playerId))
        {
            return PlayerLV_Dic[playerId];
        }
        return null;
    }

    public void SyncAllState(ServerFrameAuthenMsg msg)
    {
        foreach (var v in msg.ServerInputStateData)
        {
            LogicAndView logv= GetPlayerLogicAndView(v.playerId);
            
            if (logv != null)
            {
                var ServerPos=new Vector3(v.playerstate.playerPos.x,v.playerstate.playerPos.y,v.playerstate.playerPos.z);
                logv.view.HP = v.playerstate.hp;
                logv.logic.LogicPos =ServerPos;
                if (ServerPos!=Vector3.zero)
                {
                    Debug.Log("玩家移动了");
                }
            }       
        }
    }
    
}
