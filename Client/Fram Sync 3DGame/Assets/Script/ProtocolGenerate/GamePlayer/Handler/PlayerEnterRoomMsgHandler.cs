namespace GamePlayer{
		public class PlayerEnterRoomMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GamePlayer.PlayerEnterRoomMsg message=msg as  GamePlayer.PlayerEnterRoomMsg;
			if (message != null) {
                if (message.success)
                {
                    UnityEngine.Debug.Log("玩家" + message.playerId + "进入了房间" + message.roomId);
                    PlayerManager.Instance.player_Dic[message.playerId].nowInRoomId = message.roomId;
                }
                else
                {
                    UnityEngine.Debug.Log("玩家" + message.playerId + "进入房间" + message.roomId + "失败");
                }
            }
			
		}
		}
}
