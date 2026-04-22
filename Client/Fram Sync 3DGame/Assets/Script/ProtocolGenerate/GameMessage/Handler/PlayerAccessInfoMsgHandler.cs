using System.Diagnostics;

namespace GameMessage{
		public class PlayerAccessInfoMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GameMessage.PlayerAccessInfoMsg message=msg as  GameMessage.PlayerAccessInfoMsg;
            PlayerInfo playerInfo = new PlayerInfo();
            playerInfo.isLocalPlayer = true;
           
            playerInfo.PlayerId = message.PlayerId;
            playerInfo.password = message.password;
            
            playerInfo.username = message.username;
			playerInfo.isLocalPlayer=true;
            PlayerManager.LocalPlayerID= message.PlayerId;
			PlayerManager.Instance.RegisterNewPlayer(playerInfo);


			//PlayerCharacterCreateMsg playerCharacterCreate=	new PlayerCharacterCreateMsg();
			//playerCharacterCreate.PlayerId = message.PlayerId;
			//TCPManager.Instance.Send(playerCharacterCreate);
        }
		}
}