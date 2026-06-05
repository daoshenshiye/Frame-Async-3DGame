namespace GameSystem{
		public class TCPConnectionBuildMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GameSystem.TCPConnectionBuildMsg message=msg as  GameSystem.TCPConnectionBuildMsg;
			if (message != null) {
				TCPManager.Instance.BuildTCPConnection = true;
			}
		}
		}
}