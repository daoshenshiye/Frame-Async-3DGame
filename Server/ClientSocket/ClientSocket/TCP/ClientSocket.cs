using GameMsg;
using GameSystem;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using NetService.Net;

namespace ClientSocket.TCP
{
    public class ClientSocket
    {
        public int ID;
        private int chacheNum;
        private static int Begin_ID=0;
        private Socket socket;
        public bool isConnected=>socket.Connected;
        private  long preTime=-1;
        private static long TimeOutTime = 20;
        private bool shouldRun=false;
        private int Length;
        private TcpMsgReceiveHandler tcpMsgReceiveHandler;
       public ClientSocket(Socket socket)
       {
           tcpMsgReceiveHandler = new TcpMsgReceiveHandler(1024 * 10);
           tcpMsgReceiveHandler.OnMessageParsed=ProcessParsedMessage;
            this.socket = socket;
            ID = Begin_ID;
            ++Begin_ID;
            shouldRun = true;
            ThreadPool.QueueUserWorkItem(ReceiveMessage);
        }
        public IPEndPoint GetClientIPEndPoint()
        {
            if (socket == null)
                return null;
            return socket.RemoteEndPoint as IPEndPoint;
        }
        public void SendMessage(BaseMsg message)
        {

            try
            {
                socket.Send(message.Writting());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                MainClass.serverSocket.CloseClientSocket(this);
            }
         
          
        }
        public void ReceiveMessage(object obj)
        {
            while (shouldRun)
            {
                try
                {
                    byte[] bytes = new byte[1024*5];
                    int Length = socket.Receive(bytes);
                    if (Length <= 0)
                    {
                        break;
                    }
                    DoReceive(bytes, Length);
                    TimeOutCheck();
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    
                    MainClass.serverSocket.CloseClientSocket(this);
                    break;
                }
            }
           
        }
        private void TimeOutCheck()
        {
            if (!isConnected)
                return;
            try
            {
                if (preTime != -1 && DateTime.Now.Ticks / TimeSpan.TicksPerSecond - preTime >= TimeOutTime)
                {
                    Console.WriteLine(this.ID + "断开连接TCP心跳");
                    MainClass.serverSocket.CloseClientSocket(this);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
           
        }
        private void ProcessParsedMessage(BaseHandler handler)
        {
            BaseMsg msg = handler.msg;
            preTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;

            if (msg is QuitMessage)
            {
                MainClass.serverSocket.CloseClientSocket(this);
                return;
            }
            if (msg is HeartMsg)
            {
                preTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
                return;
            }
            handler.HandlerDo();
        }
        public void DoReceive(byte[] bytes, int receiveLength)
        {
            tcpMsgReceiveHandler.HandleReceiveMsg(bytes,receiveLength);
            #region 老版本的消息处理

                        // int nowindex = 0;
            // int ID = 0;
            // int msgLength = 0;
            // if (chacheReceives.Count > 0)
            // {
            //     foreach (var chaches in chacheReceives)
            //     {
            //         int nowindex2 = 0;
            //         int msgLength2 = 0;
            //         int ID2 = 0;
            //         ID2 = BitConverter.ToInt32(chaches.chacheBytes, nowindex2);
            //         nowindex2 += 4;
            //         msgLength2 = BitConverter.ToInt32(chaches.chacheBytes, nowindex2);
            //         nowindex2 += 4;
            //         if (msgLength2 + 8 == chaches.chacheNum + receiveLength)
            //         {
            //             chaches.chacheBytes.CopyTo(chacheBytes, 0);
            //             bytes.CopyTo(chacheBytes, chaches.chacheNum);
            //             chacheNum += receiveLength + chaches.chacheNum;
            //             chacheReceives.Remove(chaches);
            //             break;
            //         }
            //     }
            //
            // }
            // if(chacheNum==0)
            // {
            //     bytes.CopyTo(chacheBytes, chacheNum);
            //     chacheNum += receiveLength;
            // }
            // while (true)
            // {
            //     msgLength = -1;
            //     if (chacheNum - nowindex >= 8)
            //     {
            //         ID = BitConverter.ToInt32(chacheBytes, nowindex);
            //         nowindex += 4;
            //         msgLength = BitConverter.ToInt32(chacheBytes, nowindex);
            //         nowindex += 4;
            //     }
            //
            //     if (chacheNum - nowindex >= msgLength && msgLength != -1)
            //     {
            //         BaseMsg baseMsg=MsgPool.Instance.GetMsg(ID);
            //         preTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            //         if (baseMsg!=null)
            //         {
            //
            //             baseMsg.Reading(chacheBytes, nowindex);
            //             
            //             BaseHandler baseHandler = MsgPool.Instance.GetHandler(ID);
            //             baseHandler.msg = baseMsg;
            //             if (baseMsg is QuitMessage)
            //             {
            //                 MainClass.serverSocket.CloseClientSocket(this);
            //             }
            //             else if (baseMsg is HeartMsg)
            //             {
            //                 
            //                 preTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            //             }
            //             else
            //             {
            //                 if (baseHandler != null)
            //                 {
            //                     baseHandler.HandlerDo();
            //                 }
            //             }
            //            
            //         }
            //         
            //         nowindex += msgLength;
            //
            //         if (nowindex >= chacheNum)
            //         {
            //             chacheNum = 0;
            //             break;
            //         }
            //     }
            //     else
            //     {
            //
            //         if (msgLength != -1)
            //         {
            //             nowindex -= 8;
            //
            //         }
            //         byte[] chacheBytes2 = new byte[100];
            //         int chacheNum2 = 0;
            //         Array.Copy(chacheBytes, nowindex, chacheBytes2, chacheNum2, chacheNum - nowindex);
            //         chacheNum2= chacheNum - nowindex;
            //         chacheNum = 0;
            //         chacheReceives.Add(new ChacheReceive(chacheBytes2,chacheNum2));
            //         break;
            //     }
            //
            // }            

            #endregion
        }

        public void Close()
        {
            try
            {
                if (socket != null)
                {
                    tcpMsgReceiveHandler.ResetReadIndex();
                    shouldRun = false;
                    socket.Shutdown(SocketShutdown.Both);
                    
                    socket.Close();
                }
            }
            catch (Exception e)
            {
               
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
           
        }
    }
}
