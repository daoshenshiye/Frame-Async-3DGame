using System.Diagnostics;
using Unity.VisualScripting.FullSerializer;

namespace GameSystem{
		public class UDPPingMsgHandler:BaseHandler{
			public override void HandlerDo()
			{
				GameSystem.UDPPingMsg message=msg as  GameSystem.UDPPingMsg;
			FrameManager.Instance.CurrentRTT=(double)(Stopwatch.GetTimestamp()-message.SendTime) / Stopwatch.Frequency;
			FrameManager.Instance.CurrentRTT *= 1000;
			// UnityEngine.Debug.LogWarning($"UDP RTT: {FrameManager.Instance.CurrentRTT*1000} +计算结果:+ {(Stopwatch.GetTimestamp()-message.SendTime) / Stopwatch.Frequency}" +
			//                              $"UDP PingMsg SendTime: {message.SendTime} +当前时间戳:{Stopwatch.GetTimestamp()}");
			}
		}
}