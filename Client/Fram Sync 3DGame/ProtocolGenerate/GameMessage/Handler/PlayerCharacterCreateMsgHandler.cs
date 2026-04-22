namespace GameMessage{
		public class PlayerCharacterCreateMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GameMessage.PlayerCharacterCreateMsg message=msg as  GameMessage.PlayerCharacterCreateMsg;
			if(message!=null)
			{
				PlayerInfo playerInfo = new PlayerInfo();
				playerInfo.PlayerId = message.PlayerId;
				playerInfo.PlayerCharacterRes = "Prefabs/Cube";
                playerInfo.NickName = "Player" + FrameManager.Instance.CurrentLogicFrame;
                PlayerManager.Instance.CreateNewPlayer(playerInfo);
			}
		}
		}
}