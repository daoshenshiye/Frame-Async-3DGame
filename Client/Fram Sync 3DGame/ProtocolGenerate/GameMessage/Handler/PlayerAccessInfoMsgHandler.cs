namespace GameMessage{
		public class PlayerAccessInfoMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GameMessage.PlayerAccessInfoMsg message=msg as  GameMessage.PlayerAccessInfoMsg;
			PlayerInfo playerInfo=new PlayerInfo();
			playerInfo.isLocalPlayer = true;
			playerInfo.NickName = "Player" + FrameManager.Instance.CurrentLogicFrame;
			playerInfo.PlayerId=message.PlayerId;
			playerInfo.password = message.password;
			playerInfo.PlayerCharacterRes = "Prefabs/Cube";
			playerInfo.username = message.username;
			PlayerManager.Instance.CreateNewPlayer(playerInfo);
			PlayerCharacterCreateMsg playerCharacterCreate=	new PlayerCharacterCreateMsg();
			playerCharacterCreate.PlayerId = message.PlayerId;
			TCPManager.Instance.Send(playerCharacterCreate);
		}
		}
}