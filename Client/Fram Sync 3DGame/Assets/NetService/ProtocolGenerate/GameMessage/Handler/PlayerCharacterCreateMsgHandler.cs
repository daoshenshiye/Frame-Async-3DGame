namespace GameMessage{
		public class PlayerCharacterCreateMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GameMessage.PlayerCharacterCreateMsg message=msg as  GameMessage.PlayerCharacterCreateMsg;
			if(message!=null)
			{
                if (PlayerManager.Instance.player_Dic.ContainsKey(message.PlayerId))
                {
                    PlayerManager.Instance.player_Dic[message.PlayerId].PlayerCharacterRes = "Prefabs/Cube";
                    PlayerManager.Instance.player_Dic[message.PlayerId].NickName = "Player" + message.PlayerId;
                    PlayerManager.Instance.CreateNewPlayer(PlayerManager.Instance.player_Dic[message.PlayerId]);
                }
                else
                {
                    PlayerInfo playerInfo = new PlayerInfo();
                    playerInfo.PlayerId = message.PlayerId;
                    playerInfo.PlayerCharacterRes = "Prefabs/Cube";
                    playerInfo.NickName = "Player" + message.PlayerId;
                    PlayerManager.Instance.RegisterNewPlayer(playerInfo);
                    PlayerManager.Instance.CreateNewPlayer(PlayerManager.Instance.player_Dic[message.PlayerId]);
                }
               
                UnityEngine.Debug.Log("发送了UDP加入消息");
                UdpPlayerAddMsg udpPlayerAdd = new UdpPlayerAddMsg();
                udpPlayerAdd.playerId = PlayerManager.LocalPlayerID;
                LogicAndView lav = LogicViewBridge.Instance.GetPlayerLogicAndView(PlayerManager.LocalPlayerID);
                udpPlayerAdd.playerstate = new GamePlayer.PlayerStateData();
                udpPlayerAdd.playerstate.playerPos = new GamePlayer.PlayerPosData();
                udpPlayerAdd.playerstate.playerPos.x = lav.logic.LogicPos.x;
                udpPlayerAdd.playerstate.playerPos.y = lav.logic.LogicPos.y;
                udpPlayerAdd.playerstate.playerPos.z = lav.logic.LogicPos.z;
                udpPlayerAdd.playerstate.hp = lav.logic.HP;
                UdpManager.Instance.UDPSend(udpPlayerAdd, E_UDP_MSG_TYPE.SIMPLE);
            }
		}
		}
}