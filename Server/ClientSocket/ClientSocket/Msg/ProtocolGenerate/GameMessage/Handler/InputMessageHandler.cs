using ClientSocket;
using ClientSocket.UDP;
using GamePlayer;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime.InteropServices;
namespace GameMessage{
        public class ClientInput
        {
            public int  playerId;
            public long predictFrame;
            public PlayerStateData state;
            public InputData input;
        }
		public class InputMessageHandler:BaseHandler{
        //private static int player1;
        //private static int player2;
       
		public override void HandlerDo(){
			GameMessage.InputMessage message=msg as  GameMessage.InputMessage;
            if (message != null) {
                ClientInput ClientInput= LoadInputData(message);
                // Console.WriteLine($"===收到输入===");
                // Console.WriteLine($"PlayerId={message.PlayerId}");
                // Console.WriteLine($"PredictFrame={message.PredictFrame}");
                // Console.WriteLine($"H={message.input.Horizontal}, V={message.input.Vertical}");
                // Console.WriteLine($"存入服务端帧: {ClientInput.predictFrame/2}");
                // use thread-safe GetOrAdd to obtain per-player queue and enqueue the input
                
                var frameDict = MainClass.frameManager.frameInputBuffer.GetOrAdd(
                    ClientInput.predictFrame,
                   new ConcurrentDictionary<int, ClientInput>()
                );
                frameDict[message.PlayerId] = ClientInput;
                //Console.WriteLine("有同帧输入");
                // Console.WriteLine($"缓冲区帧{ClientInput.predictFrame/2}现在有{frameDict.Count}个玩家");
            }
            
            
            #region 方案二 用字典存帧号
            //lock (MainClass.frameManager.NetInputsDic)
            //{
            //    if (message != null)
            //    {
            //        if(MainClass.frameManager.NetInputsDic.ContainsKey(message.CurrentLogicFrame))
            //        {

            //            Console.WriteLine("有相同帧号");
            //            MainClass.frameManager.NetInputsDic[message.CurrentLogicFrame].Add(message);
            //        }
            //        else
            //        {
            //            List<BaseMsg> list=new List<BaseMsg> ();
            //            list.Add(message);
            //            MainClass.frameManager.NetInputsDic.Add(message.CurrentLogicFrame, list);
            //        }
            //    }
            //}
            #endregion

            #region 方案一，客户端帧号与服务器当前帧号完全对齐
            //if (message == null) return;
            //Console.WriteLine(message.CurrentLogicFrame);
            //lock (MainClass.frameManager.PlayerFrameInputs)
            //{
            //    long serverFrame = MainClass.frameManager.ReadLogicFrame();

            //    // 只收当前帧，且不存在才添加
            //    if (message.CurrentLogicFrame == serverFrame)
            //    {
            //        if (!MainClass.frameManager.PlayerFrameInputs.ContainsKey(message.PlayerId))
            //        {
            //            MainClass.frameManager.PlayerFrameInputs.Add(message.PlayerId, message);
            //        }

            //    }
            //}
            #endregion

            //long currentLogicFrame = MainClass.frameManager.ReadLogicFrame();
            //long preLogicFrame = MainClass.frameManager.GetPreLogicFrame();
            //    if (preLogicFrame == -1 ||
            //    preLogicFrame != currentLogicFrame)
            //    {
            //        if (message != null)
            //        {
            //            MainClass.udpserver.BroadCastMsg(message,E_UDP_MSG_TYPE.ORDER_STEADY);
            //        }

            //    }
            //    MainClass.frameManager.UpdatePreLogicFrame(currentLogicFrame);

        }
        public ClientInput LoadInputData(InputMessage msg)
        {
            ClientInput clientinput = new ClientInput();
            clientinput.input = msg.input;
            clientinput.playerId = msg.PlayerId;
            clientinput.predictFrame = msg.PredictFrame;
            return clientinput;
        }
       
    }
}