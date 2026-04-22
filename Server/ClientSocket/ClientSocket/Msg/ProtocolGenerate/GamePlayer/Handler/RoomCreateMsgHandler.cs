using ClientSocket;

namespace GamePlayer{
		public class RoomCreateMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GamePlayer.RoomCreateMsg message=msg as  GamePlayer.RoomCreateMsg;
			if (message != null) {
			MainClass.serverSocket.CraeteRoom(message.roomId,message.playerId);
			
			}
		
		}
		}
}