
namespace GamePlayer{
		public class RoomCreateMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GamePlayer.RoomCreateMsg message=msg as  GamePlayer.RoomCreateMsg;
			if (message!=null)
			{
                if (message.success)
                {
                    PlayerManager.Instance.player_Dic[message.playerId].nowInRoomId = message.roomId;

                }
                else
                {
                    UnityEngine.Debug.Log("创建房间失败");
                }
            }
			
		}
		}
}