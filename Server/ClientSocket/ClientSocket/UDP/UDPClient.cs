using GameMessage;
using GameMsg;
using GamePlayer;
using GameSystem;
using System;
using System.Collections.Concurrent;
using ClientSocket.ServerPlayer;

namespace ClientSocket.UDP
{
    public enum E_UDP_MSG_TYPE
    {
        SIMPLE,
        ORDER_STEADY
        
    }
    public class UDPClient
    {

        private static long TimeOutTime=40;
        public long preTime=-1;
        public ConcurrentDictionary<long, BaseHandler> inputsDic;
        public string ipaddr;
        public int playerID=-1;
        private bool shouldOpen=false;
        private Thread CheckTimeOutThread;
        private UdpMsgReceiveHandler  receiveMsgHandler;
        public  UDPClient(string id)
        {
            receiveMsgHandler=new UdpMsgReceiveHandler();
            receiveMsgHandler.OnCompleteReceive += OnProcessMsg;
           inputsDic= new ConcurrentDictionary<long, BaseHandler>();
            ipaddr= id;
            shouldOpen= true;
            CheckTimeOutThread=new Thread(CheckTimeOut);
            CheckTimeOutThread.Start();
        }
        
        public void CheckTimeOut()
        {
            while (shouldOpen)
            {
                Thread.Sleep(200);
               
                if (preTime != -1 && DateTime.Now.Ticks / TimeSpan.TicksPerSecond - preTime >= TimeOutTime)
                {
                    MainClass.udpserver.ClientDic.RemoveClient(ipaddr);
                    
                    Console.WriteLine("删除客户端" + ipaddr);
                    shouldOpen = false;
                }
            }
        }
       
        public void ReceiveMsg(byte[] bytes,int receiveLegth)
        {
            if(bytes!=null&& receiveLegth>0)
            {
                HandleMsg(bytes, receiveLegth);
            }
        }
        public void HandleMsg(byte[] bytes,int receiveLegth)
        {
            try
            {
                receiveMsgHandler.HandleMsg(bytes, receiveLegth);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        public void OnProcessMsg(short type,long nowSeq,BaseHandler baseHandler)
        {
            preTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            if (type == 0)
            {
                if (baseHandler != null)
                {
                    if (baseHandler.msg is HeartMsg)
                    {
                        preTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
                    }
                    else
                    {
                        MainClass.udpserver.simpleMsgQueue.Enqueue(baseHandler);
                    }
                }
            }
            else if (type == 1)
            {
                inputsDic.GetOrAdd(nowSeq, baseHandler);
                if (baseHandler.msg is GameMessage.InputMessage inputMsg)
                {
                    MainClass.udpserver.ClientDic.SetPalyerAddr(inputMsg.PlayerId, ipaddr);
                }
            }
        }
    }
}
