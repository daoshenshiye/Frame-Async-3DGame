using System.Collections.Generic;

namespace GamePlayer{
		public class PlayerExitRoomMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GamePlayer.PlayerExitRoomMsg message=msg as  GamePlayer.PlayerExitRoomMsg;
			if (message!=null)
			{
				if (message.success)
				{
					UnityEngine.Debug.Log("玩家"+message.playerId+"退出了房间"+message.roomId);
					List<int> ids=new List<int>();
					ids.AddRange(PlayerManager.Instance.player_Dic.Keys);
                    foreach (var item in ids)
                    {
                        PlayerManager.Instance.RemovePlayerInstance(item);
                    }
                    
                }
				else
				{
					UnityEngine.Debug.Log("玩家"+message.playerId+"退出房间"+message.roomId+"失败");
                }
			}
		}
		}
}