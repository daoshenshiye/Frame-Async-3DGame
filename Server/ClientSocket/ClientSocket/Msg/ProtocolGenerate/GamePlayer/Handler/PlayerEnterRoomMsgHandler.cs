using ClientSocket;

namespace GamePlayer{
		public class PlayerEnterRoomMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GamePlayer.PlayerEnterRoomMsg message=msg as  GamePlayer.PlayerEnterRoomMsg;
			if(message!=null)
			MainClass.serverSocket.EnterRoom(message.roomId, message.playerId);
        }
		}
}