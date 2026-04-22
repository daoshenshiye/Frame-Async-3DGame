using ClientSocket;
using ClientSocket.UDP;
using GamePlayer;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime.InteropServices;
namespace GameMessage{
		public class InputMessageHandler:BaseHandler{
        //private static int player1;
        //private static int player2;
		public override void HandlerDo(){
			GameMessage.InputMessage message=msg as  GameMessage.InputMessage;
            if (message != null) {
                ServerInputAndStateData servermsg= SaveInputData(message);

                // use thread-safe GetOrAdd to obtain per-player queue and enqueue the input
                var q = MainClass.frameManager.playerInputs.GetOrAdd(message.PlayerId, _ => new ConcurrentQueue<ServerInputAndStateData>());
                q.Enqueue(servermsg);
                //Console.WriteLine("有同帧输入");
                
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
        public ServerInputAndStateData SaveInputData(InputMessage msg)
        {
            ServerInputAndStateData playerStateAndInput = new ServerInputAndStateData();
            playerStateAndInput.inputdata = msg.input;
            playerStateAndInput.playerId = msg.PlayerId;
            return playerStateAndInput;
        }
       
    }
}