using System;
using System.Collections;
using System.Collections.Generic;
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
    public LogicAndView GetPlayerLogicAndView(int playerId)
    {
        if(PlayerLV_Dic.ContainsKey(playerId))
        {
            return PlayerLV_Dic[playerId];
        }
        return null;
    }

    public void SyncAllPlayerView()
    {
        foreach (LogicAndView item in PlayerLV_Dic.Values)
        {
            item.view.SyncHP();
        }
    }
    
}
