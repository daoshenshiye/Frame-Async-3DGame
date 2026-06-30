using UnityEngine;

namespace GameSystem{
		public class UDPConnectionBuildMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GameSystem.UDPConnectionBuildMsg message=msg as  GameSystem.UDPConnectionBuildMsg;
			 if(message==null){return;}
				FrameManager.Instance.shouldSendInput=true;
				FrameManager.Instance.CurrentServerLogicFrame = message.ServerLogicFrame;
				FrameManager.Instance.ServerDelayBuffer = message.DelayBufferFrame;
				FrameManager.Instance.LocalPredictLogicFrame = message.ServerLogicFrame + message.DelayBufferFrame + FrameManager.Instance.DelayPredictFrame;
		}
		}
}
