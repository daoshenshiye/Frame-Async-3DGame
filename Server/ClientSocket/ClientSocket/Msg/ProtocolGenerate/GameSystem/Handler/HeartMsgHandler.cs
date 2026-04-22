using ClientSocket;

namespace GameSystem{
		public class HeartMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GameSystem.HeartMsg message=msg as  GameSystem.HeartMsg;
        }
		}
}