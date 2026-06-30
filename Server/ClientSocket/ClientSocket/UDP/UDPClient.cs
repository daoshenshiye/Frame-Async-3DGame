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
        public  UDPClient(string id)
        {
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

                //HandleMsg((cacheBytes,receiveLegth));
                //ThreadPool.QueueUserWorkItem(HandleMsg, (cacheBytes, receiveLegth));
                HandleMsg((bytes, receiveLegth));
            }
           
        }
        public void HandleMsg(object obj)
        {
            try
            {
                 int nowIndex = 0;
            int msgLength = 0;
            int ID = 0;
            long nowSeq = -1;
            int chacheNum = 0;
            (byte[] bytes, int len) info = ((byte[] bytes, int len))obj;
            byte[] chacheBytes = info.bytes;
            chacheNum = info.len;

            
                int type = -1;
                if (chacheBytes.Length>=sizeof(short))
                {
                    type = BitConverter.ToInt16(chacheBytes, nowIndex);
                }
                else
                {
                    return;
                }
                nowIndex += 2;
                msgLength = -1;
                if (chacheNum >= 18 && type == 1 || chacheNum >= 10 && type == 0)
                {
                    if (type == 1)
                    {
                        nowSeq = BitConverter.ToInt64(chacheBytes, nowIndex);
                        nowIndex += 8;
                    }

                    ID = BitConverter.ToInt32(chacheBytes, nowIndex);
                    nowIndex += 4;
                    msgLength = BitConverter.ToInt32(chacheBytes, nowIndex);
                    nowIndex += 4;

                    if (chacheNum - nowIndex >= msgLength && msgLength != -1)
                    {
                        BaseMsg baseMsg = null;
                        baseMsg = MsgPool.Instance.GetMsg(ID);

                        #region 超时逻辑

                        //else if(baseMsg is UdpAckMsg)
                        //{
                        //    //lock (MainClass.udpserver.AckDic)
                        //    //{
                        //    //    if (baseMsg != null)
                        //    //    {
                        //    //        UdpAckMsg udpAck = (baseMsg as UdpAckMsg);
                        //    //        if (MainClass.udpserver.AckDic.ContainsKey(udpAck.seq))
                        //    //        {
                        //    //            var clientDic = MainClass.udpserver.AckDic[udpAck.seq];
                        //    //            if (clientDic.ContainsKey(ipaddr))
                        //    //            {
                        //    //                clientDic.Remove(ipaddr);
                        //    //                Console.WriteLine($"删除Ack：seq={udpAck.seq}, ipaddr={ipaddr}");


                        //    //                if (clientDic.Count == 0)
                        //    //                {
                        //    //                    MainClass.udpserver.AckDic.Remove(udpAck.seq);
                        //    //                    Console.WriteLine($"AckDic中seq={udpAck.seq}的子字典为空，已删除");
                        //    //                }
                        //    //                if (MainClass.udpserver.needDeleteDic.ContainsKey(udpAck.seq))
                        //    //                {
                        //    //                    MainClass.udpserver.needDeleteDic[udpAck.seq].Remove(ipaddr);
                        //    //                    if (MainClass.udpserver.needDeleteDic[udpAck.seq].Count == 0)
                        //    //                    {
                        //    //                        MainClass.udpserver.needDeleteDic.Remove(udpAck.seq);
                        //    //                    }
                        //    //                }

                        //    //            }
                        //    //        }
                        //    //    }
                        //    //}

                        //}

                        #endregion

                        if (baseMsg != null)
                        {
                            baseMsg.Reading(chacheBytes, nowIndex);
                            BaseHandler baseHandler = MsgPool.Instance.GetHandler(ID);
                            baseHandler.msg = baseMsg;
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
                                //MainClass.udpserver.Counter++;
                                //Console.WriteLine(MainClass.udpserver.Counter);

                                inputsDic.GetOrAdd(nowSeq, baseHandler);
                                //    try
                                //    {
                                //    Console.WriteLine($"UDPClient.HandleMsg: received seq {nowSeq} for player {playerID} at {DateTime.Now:O}");
                                //}
                                //    catch { }

                                // If this message contains a player id (e.g. InputMessage), update the server mapping

                                if (baseHandler.msg is GameMessage.InputMessage inputMsg)
                                {
                                    MainClass.udpserver.ClientDic.SetPalyerAddr(inputMsg.PlayerId, ipaddr);
                                }
                            }
                        }

                        nowIndex += msgLength;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

    }
}
