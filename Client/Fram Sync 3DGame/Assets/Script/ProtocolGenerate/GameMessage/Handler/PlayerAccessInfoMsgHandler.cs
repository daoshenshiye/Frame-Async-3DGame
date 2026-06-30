using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameMessage{
		public class PlayerAccessInfoMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GameMessage.PlayerAccessInfoMsg message=msg as  GameMessage.PlayerAccessInfoMsg;
            PlayerInfo playerInfo = new PlayerInfo();
            playerInfo.isLocalPlayer = true;
			
            playerInfo.PlayerId = message.PlayerId;
            playerInfo.password = message.password;
            
            
            if (UdpManager.Instance == null)
            {
	            Debug.Log("网络同步启动UDP");
	            GameObject gameObject = new GameObject("NETUDP");
	            gameObject.AddComponent<UdpManager>();
	            UdpManager.Instance.InitUdp();
            }
            playerInfo.username = message.username;
			playerInfo.isLocalPlayer=true;
            PlayerManager.LocalPlayerID= message.PlayerId;
			PlayerManager.Instance.RegisterNewPlayer(playerInfo);
			TCPManager.Instance.BuildTCPConnection = true;

			//PlayerCharacterCreateMsg playerCharacterCreate=	new PlayerCharacterCreateMsg();
			//playerCharacterCreate.PlayerId = message.PlayerId;
			//TCPManager.Instance.Send(playerCharacterCreate);
		}
		}
}