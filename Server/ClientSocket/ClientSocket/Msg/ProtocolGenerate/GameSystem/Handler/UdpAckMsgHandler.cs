using ClientSocket;
using System.Diagnostics.Metrics;

namespace GameSystem{
		public class UdpAckMsgHandler:BaseHandler{
		public override void HandlerDo()
		{
			GameSystem.UdpAckMsg message = msg as GameSystem.UdpAckMsg;


		}
	}
}