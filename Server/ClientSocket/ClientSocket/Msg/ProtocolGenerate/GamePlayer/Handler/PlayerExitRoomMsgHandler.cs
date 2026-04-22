using ClientSocket;

namespace GamePlayer{
		public class PlayerExitRoomMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GamePlayer.PlayerExitRoomMsg message=msg as  GamePlayer.PlayerExitRoomMsg;
			if (message!=null)
			{
				MainClass.serverSocket.ExitRoom(message.roomId,message.playerId);
            }
        }
		}
}