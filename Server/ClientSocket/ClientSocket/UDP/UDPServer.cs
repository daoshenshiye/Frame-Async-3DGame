using GameMessage;
using GameSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClientSocket.UDP
{
    public class AckPackage
    {
        public BaseMsg baseMsg;
        public int counter=0;
        
        public AckPackage(BaseMsg Msg)
        {
            baseMsg = Msg;
        }
    }
    public class AckSendPackage {
    
        public byte[] data;
        public string strID;
       public AckSendPackage(byte[] data,string str) {
            
            this.data = data;
            this.strID = str;
        }
    }

    public class UDPServer
    {
        public Dictionary<string, UDPClient> UDP_Client_Dic = new Dictionary<string, UDPClient>();
        public Dictionary<int,string> ClientPID_TO_Addr_Dic= new Dictionary<int,string>();
        public  Dictionary<long, Dictionary<string,AckPackage>> AckDic= new Dictionary<long, Dictionary<string, AckPackage>>();
        private  Socket socket;
        private bool isRunning = false;
        public  long nowsequence = 0;
        public ConcurrentDictionary<int,ConcurrentQueue<BaseHandler>> playerInputsQ_Dic=new ConcurrentDictionary<int, ConcurrentQueue<BaseHandler>>();
        public ConcurrentQueue<BaseHandler> simpleMsgQueue=new ConcurrentQueue<BaseHandler>();
        public Dictionary<long,List<string>> needDeleteDic=new Dictionary<long, List<string>>();
        private List<AckSendPackage> needSendList=new List<AckSendPackage>();
        public int Counter=0;
       


        public UDPServer(string ip, int port)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            isRunning = true;

            Receive();
            ThreadPool.QueueUserWorkItem(DoProcess);
            //ThreadPool.QueueUserWorkItem(CheckTimeOutSendBack);

        }
        public void Receive()
        {
                try
                {
                byte[] buffer = new byte[8192];
                EndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                socket.BeginReceiveFrom(buffer,
                            0,
                            buffer.Length,
                            SocketFlags.None, 
                            ref ipEndPoint,
                            new AsyncCallback(ReceiveCallBack),
                            buffer);
                        //try { Console.WriteLine($"UDPServer.Receive: packet from {(ipEndPoint as IPEndPoint).Address}:{(ipEndPoint as IPEndPoint).Port} length={Length} at {DateTime.Now:O}"); } catch { }
                   
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
               
            
        }
        private void ReceiveCallBack(IAsyncResult ar)
        {
            try {
                EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] buffer = (byte[])ar.AsyncState;
                int length = socket.EndReceiveFrom(ar, ref remote);
                byte[] data = new byte[length];
                Buffer.BlockCopy(buffer, 0, data, 0, length);
                if (length > 0)
                {
                    string key = (remote as IPEndPoint).Address.ToString() + "," + (remote as IPEndPoint).Port.ToString();
                    
                    if (UDP_Client_Dic.ContainsKey(key))
                    {
                        UDP_Client_Dic[key].ReceiveMsg(data, length);

                    }
                    else
                    {
                        Console.WriteLine("UDP客户端地址" + key);

                        UDP_Client_Dic.Add(key, new UDPClient(key));
                        
                        UDP_Client_Dic[key].ReceiveMsg(data, length);
                        //try { Console.WriteLine($"UDPServer.Receive: new client {key} created at {DateTime.Now:O}"); } catch { }
                    }
                }
            }
            catch (Exception e){
            Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            finally {
                Receive(); 
            }
        }
        public void DoProcess(object obj)
        {
            try
            {
                while (isRunning)
                {
                    ShareinputsEach();
                    DoReceive();
                    // allow other threads to run and avoid tight busy-looping
                    Thread.Sleep(1);
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
           
        }
        public void ShareinputsEach()
        {
            ShareInputs();
        }

        #region  废弃的超时重发机制
        //public void CheckTimeOutSendBack(object obj)
        //{

        //    while (isRunning) {
        //        Thread.Sleep(200);
        //        needSendList.Clear();
        //        lock (AckDic)
        //            lock (needDeleteDic)
        //            {

        //                var needRemoveSeq = new List<long>();
        //                foreach (var item in needDeleteDic)
        //                {
        //                    if (!AckDic.ContainsKey(item.Key))
        //                    {
        //                        needRemoveSeq.Add(item.Key);
        //                        continue;
        //                    }

        //                    var clientDic = AckDic[item.Key];

        //                    foreach (var ipaddr in item.Value)
        //                    {
        //                        if (clientDic.ContainsKey(ipaddr))
        //                        {
        //                            clientDic.Remove(ipaddr);
        //                        }
        //                    }


        //                    if (clientDic.Count == 0)
        //                    {
        //                        needRemoveSeq.Add(item.Key);
        //                    }
        //                }


        //                foreach (var seq in needRemoveSeq)
        //                {
        //                    needDeleteDic.Remove(seq);
        //                    AckDic.Remove(seq);
        //                }
        //            }
        //        lock (AckDic)
        //        {
        //            foreach (var item in AckDic)
        //            {
        //                foreach (var item1 in item.Value)
        //                {
        //                    ++item1.Value.counter;
        //                    byte[] bytes = item1.Value.baseMsg.Writting();
        //                    byte[] new_byte = new byte[bytes.Length + 10];
        //                    BitConverter.GetBytes((short)1).CopyTo(new_byte, 0);
        //                    BitConverter.GetBytes(item.Key).CopyTo(new_byte, 2);
        //                    Array.Copy(bytes, 0, new_byte, 10, bytes.Length);
        //                    bytes = new_byte;
        //                    needSendList.Add(new AckSendPackage(bytes, item1.Key));
        //                    if (item1.Value.counter >= 3)
        //                    {
        //                        lock (needDeleteDic)
        //                        {
        //                            if (needDeleteDic.ContainsKey(item.Key))
        //                            {
        //                                needDeleteDic[item.Key].Add(item1.Key);
        //                            }
        //                            else
        //                            {
        //                                List<string> list = new List<string>();
        //                                list.Add(item1.Key);
        //                                needDeleteDic.Add(item.Key, list);
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        foreach (var item in needSendList)
        //        {
        //            ++Counter;
        //            Console.WriteLine("发送了延迟数据包"+ Counter);
        //            lock (socket)
        //            {
        //                socket.SendTo(item.data,GetIPEndPointFromClientDic(item.strID));
        //            }
        //        }
        //    }
        //}
        #endregion
        private void ShareInputs()
        {
            try {
                lock (UDP_Client_Dic)
                {
                    foreach (var item in UDP_Client_Dic)
                    {
                        string strID = item.Key;
  

                            List<long> sortedKeys =new List<long>();
                            sortedKeys.AddRange(item.Value.inputsDic.Keys);
                            sortedKeys.Sort();
                           
                        if(MainClass.frameManager.playerInputs.ContainsKey(item.Value.playerID))
                        {
                            if (MainClass.frameManager.playerInputs[item.Value.playerID].Count != 0)
                            {
                                continue;
                            }
                        }
                        
                        List<long> keysToRemove = new List<long>();
                        foreach (var item1 in sortedKeys)
                        {
                            item.Value.inputsDic.TryGetValue(item1, out BaseHandler handler);
                            if (handler != null)
                            {
                                handler.HandlerDo();
                                //var q = playerInputsQ_Dic.GetOrAdd(item.Value.playerID, _ => new ConcurrentQueue<BaseHandler>());
                                //q.Enqueue(handler);


                                // High-frequency logging disabled to reduce IO contention and avoid blocking.
                                // try { Console.WriteLine($"ShareInputs: moved seq {item1.Key} for player {item.Value.playerID} from {strID} at {DateTime.Now:O}"); } catch { }
                                keysToRemove.Add(item1);
                            }
                            break;
                        }
                        
                        foreach (var item1 in keysToRemove)
                        {
                          
                               item.Value.inputsDic.TryRemove(item1,out _);
                            
                            
                        }

                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
           
        }
        public void DoReceive()
        {
            
            if (simpleMsgQueue.TryDequeue(out BaseHandler result))
            {
                if (result!=null)
                {
                    result.HandlerDo();
                }
            }

                //foreach (var item in playerInputsQ_Dic)
                //{
                //    try
                //    {
                //    // Always try to dequeue and handle input handlers from the per-player queue.
                //    // Previously this was gated by whether frameManager.playerInputs contained the key,
                //    // which could cause handlers to be left in the queue and introduce delays.
                //    int process = 0;
                //        while (process<5&&item.Value.TryDequeue(out BaseHandler handler))
                //        {
                //            if (handler != null)
                //            {
                //                // High-frequency logging disabled to reduce IO contention.
                //                // Console.WriteLine($"DoReceive: dequeued handler for player {item.Key} at {DateTime.Now:O}");
                //                handler.HandlerDo();
                //            }
                //            ++process;
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        Console.WriteLine($"DoReceive: exception processing player {item.Key}: {ex.Message}");
                //        Console.WriteLine(ex.StackTrace);
                //    }
                //}
            
        }
        public  void SendMessage(BaseMsg baseMsg,IPEndPoint iPEnd,E_UDP_MSG_TYPE e_UDP_MSG_)
        {
            if(baseMsg!=null)
            {

                if(socket!=null)
                {
                    
                        byte[] buff = baseMsg.Writting();
                        switch (e_UDP_MSG_)
                        {
                            case E_UDP_MSG_TYPE.SIMPLE:
                                
                               AddSimpleHeadToData(ref buff);
                                
                                
                                break;
                            case E_UDP_MSG_TYPE.ORDER_STEADY:
                                AddSequenceToData(ref buff);
                                break;
                            default:
                                break;
                        }
                        socket.SendTo(buff, iPEnd);
                    
                }
               
               
            }
        }
        public void BroadCastMsg(BaseMsg baseMsg,E_UDP_MSG_TYPE e_UDP_MSG_)
        {
            byte[] bytes = baseMsg.Writting();
            switch (e_UDP_MSG_)
            {
                case E_UDP_MSG_TYPE.SIMPLE:
                    AddSimpleHeadToData(ref bytes);
                    break;
                case E_UDP_MSG_TYPE.ORDER_STEADY:
                    AddSequenceToData(ref bytes);
                    ++MainClass.udpserver.nowsequence;
                    break;
                default:
                    break;
            }
           
            if (bytes != null)
            {
                Dictionary<string, UDPClient> newDIc = UDP_Client_Dic;
                foreach (var item in newDIc)
                {
                    
                       socket.BeginSendTo(bytes, 
                           0, 
                           bytes.Length,
                           SocketFlags.None, 
                           GetIPEndPointFromClientDic(item.Key), 
                           SendCallBack, socket);
                    
                        
                    #region 废弃的超时重发机制
                    //lock (AckDic)
                    //{
                    //    if (!AckDic.ContainsKey(nowsequence))
                    //    {
                    //        Dictionary<string, AckPackage> dic = new Dictionary<string, AckPackage>();
                    //        dic.Add(item.Key,new AckPackage(baseMsg));
                    //        AckDic.Add(nowsequence, dic);
                    //    }
                    //    else if (AckDic.ContainsKey(nowsequence))
                    //    {
                    //        if (AckDic[nowsequence].ContainsKey(item.Key))
                    //        {
                    //            AckDic[nowsequence][item.Key] = new AckPackage(baseMsg);
                    //        }
                    //        else
                    //        {
                    //            AckDic[nowsequence].Add(item.Key, new AckPackage(baseMsg));
                    //        }
                    //    }
                    //}
                    #endregion
                }

            }
        }
        private void SendCallBack(IAsyncResult ar)
        {
            ((Socket)ar.AsyncState).EndSendTo(ar);
        }
        public void AddSimpleHeadToData(ref byte[] bytes)
        {
            byte[] new_byte = new byte[bytes.Length + 2];
            BitConverter.GetBytes((short)0).CopyTo(new_byte, 0);
            bytes.CopyTo(new_byte, 2);
            bytes = new_byte;
        }
        public void AddSequenceToData(ref byte[] bytes)
        {
            byte[] new_byte = new byte[bytes.Length + 10];
             BitConverter.GetBytes((short)1).CopyTo(new_byte,0);
            BitConverter.GetBytes(nowsequence).CopyTo(new_byte, 2);
            Array.Copy(bytes, 0, new_byte, 10, bytes.Length);
            bytes = new_byte;
        }
        public IPEndPoint GetIPEndPointFromClientDic(string s)
        {
            string[] strs = s.Split(",");
            return new IPEndPoint(IPAddress.Parse(strs[0]), Int32.Parse(strs[1]));
        }
        public void Close()
        {
            if (socket != null)
            {
                socket.Shutdown(SocketShutdown.Both);   
                socket.Close();
                isRunning = false;
            }

        }
    }
}
