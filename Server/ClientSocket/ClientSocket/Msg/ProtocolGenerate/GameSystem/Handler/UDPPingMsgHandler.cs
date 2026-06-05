using ClientSocket;

namespace GameSystem{
		public class UDPPingMsgHandler:BaseHandler{
			public override void HandlerDo()
			{
				GameSystem.UDPPingMsg message=msg as  GameSystem.UDPPingMsg;
				if (MainClass.frameManager.SendTimeBuffer.ContainsKey(message.playerId))
				{
					MainClass.frameManager.SendTimeBuffer[message.playerId] = message.SendTime;
				}
				else
				{
					MainClass.frameManager.SendTimeBuffer.TryAdd(message.playerId,message.SendTime);
				}
			}
		}
}