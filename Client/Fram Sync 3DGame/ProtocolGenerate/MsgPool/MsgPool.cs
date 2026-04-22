using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GameMsg{
		public class MsgPool{
		 public static Dictionary<int,Type> ID_Dic= new Dictionary<int,Type>();
    public static Dictionary<int,Type> Handler_Dic=new Dictionary<int,Type>();
private static MsgPool instance = new MsgPool();
    public static MsgPool Instance => instance;
public void Register(int id,Type MsgType,Type HandlerType)
    {
        ID_Dic.Add(id,MsgType);
        Handler_Dic.Add(id,HandlerType);
    }
    public  BaseMsg GetMsg(int id)
    {
        if(ID_Dic.ContainsKey(id))
        {
            return (BaseMsg)Activator.CreateInstance(ID_Dic[id]);
        }
        return null;
    }
    public   BaseHandler GetHandler(int id)
    {
        if (Handler_Dic.ContainsKey(id))
        {
            return (BaseHandler)Activator.CreateInstance(Handler_Dic[id]);
        }
        return null;
    } public MsgPool()
    {
     Register(140,typeof(GameMessage.InputMessage),typeof(GameMessage.InputMessageHandler));
    Register(505,typeof(GameSystem.HeartMsg),typeof(GameSystem.HeartMsgHandler));
    Register(404,typeof(GameSystem.UdpAckMsg),typeof(GameSystem.UdpAckMsgHandler));
    Register(450,typeof(GameMessage.PlayerAccessInfoMsg),typeof(GameMessage.PlayerAccessInfoMsgHandler));
    Register(451,typeof(GameMessage.PlayerCharacterCreateMsg),typeof(GameMessage.PlayerCharacterCreateMsgHandler));
    Register(452,typeof(GameMessage.PlayerCharacterDestroyMsg),typeof(GameMessage.PlayerCharacterDestroyMsgHandler));

				}
		}
}