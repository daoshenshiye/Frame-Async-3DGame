using GameMessage;
using GameSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ClientSocket.ServerPlayer;
using ClientSocket.Tools;
using GameMsg;

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

    public class UdpClientDic
    {
        private Dictionary<string, UDPClient> UDP_Client_Dic;
        private Dictionary<int, string> ClientPID_TO_Addr_Dic;
        
        public UdpClientDic()
        {
            UDP_Client_Dic = new();
            ClientPID_TO_Addr_Dic = new();
        }

        public bool AddClient(string IpPort,int playerId)
        {
            bool isNewClient = false;
            
            if (!UDP_Client_Dic.ContainsKey(IpPort))
            {
                lock (ClientPID_TO_Addr_Dic)
                foreach (var v in ClientPID_TO_Addr_Dic)
                {
                    if (v.Key==playerId)
                    {
                        ClientPID_TO_Addr_Dic[v.Key] = IpPort;
                        UDP_Client_Dic[IpPort] = new UDPClient(IpPort);
                        return false;
                    }
                }
                
                
                UDP_Client_Dic[IpPort] = new UDPClient(IpPort);
                ClientPID_TO_Addr_Dic[playerId] = IpPort;
                return true;
                
            }
            return false;
        }

        public bool RemoveClient(string IpPort)
        {
                if (UDP_Client_Dic.ContainsKey(IpPort))
                {
                    if (UDP_Client_Dic[IpPort].playerID!=-1)
                    {
                        lock (ClientPID_TO_Addr_Dic)
                        {
                            if(ClientPID_TO_Addr_Dic.ContainsKey(UDP_Client_Dic[IpPort].playerID))
                            ClientPID_TO_Addr_Dic.Remove(UDP_Client_Dic[IpPort].playerID);
                        }
                    }
                    lock (UDP_Client_Dic)
                    {
                        UDP_Client_Dic.Remove(IpPort);
                    }
                    return true;
                }

                return false;
        }

        public bool HasClient(string key)
        {
            if (UDP_Client_Dic.ContainsKey(key))
            {
                return true;
            }

            return false;
        }

        public bool HasPlayer(int playerId)
        {
            if (ClientPID_TO_Addr_Dic.ContainsKey(playerId))
            {
                return true;
            }

            return false;
        }

        public string GetPlayerAddr(int playerId)
        {
            if (ClientPID_TO_Addr_Dic.ContainsKey(playerId))
            {
                    return ClientPID_TO_Addr_Dic[playerId];
            }
            return null;
        }
        public UDPClient GetClient(string key)
        {
            if (UDP_Client_Dic.ContainsKey(key))
            {
                return  UDP_Client_Dic[key];
            }
            return null;
        }
        public Dictionary<string, UDPClient> GetClients()
        {
            return UDP_Client_Dic;
        }

        public void SetPalyerAddr(int playerId, string playerAddr)
        {
            if (ClientPID_TO_Addr_Dic.ContainsKey(playerId))
            {
                ClientPID_TO_Addr_Dic[playerId] = playerAddr;
            }
        }
    }
    public class UDPServer
    {
        public UdpClientDic ClientDic = new();
        private  Socket socket;
        private bool isRunning = false;
        public  long nowsequence = 0;
        public ConcurrentQueue<BaseHandler> simpleMsgQueue=new ConcurrentQueue<BaseHandler>();
        private byte[] buffer = new byte[8192];
        public UDPServer(string ip, int port)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            isRunning = true;

            Receive();
            ThreadPool.QueueUserWorkItem(DoProcess);
        }
        public void Receive()
        {
                try
                {
                EndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                lock (socket)
                {
                    socket.BeginReceiveFrom(buffer,
                        0,
                        buffer.Length,
                        SocketFlags.None, 
                        ref ipEndPoint,
                        ReceiveCallBack,
                        ipEndPoint);
                }
                        //try { Console.WriteLine($"UDPServer.Receive: packet from {(ipEndPoint as IPEndPoint).Address}:{(ipEndPoint as IPEndPoint).Port} length={Length} at {DateTime.Now:O}"); } catch { }
                   
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
               
            
        }
        private void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {

                EndPoint ipEnd = (EndPoint)ar.AsyncState;
                int length;
                lock (socket)
                {
                    length = socket.EndReceiveFrom(ar, ref ipEnd);
                }
                
                if (length > 0)
                {
                    string key = (ipEnd as IPEndPoint).Address.ToString() + "," + (ipEnd as IPEndPoint).Port.ToString();
                    
                    if (ClientDic.HasClient(key))
                    {
                        ClientDic.GetClient(key)?.ReceiveMsg(buffer, length);
                    }
                    else
                    {
                        BuildClient((buffer,length,key));
                        Console.WriteLine("UDP客户端地址" + key);
                        //try { Console.WriteLine($"UDPServer.Receive: new client {key} created at {DateTime.Now:O}"); } catch { }
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.ErrorCode);
                Console.WriteLine(e.SocketErrorCode);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            catch (Exception e){
                
            Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            finally {
                    EndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    lock (socket)
                    {
                        socket.BeginReceiveFrom(buffer,
                            0,
                            buffer.Length,
                            SocketFlags.None, 
                            ref ipEndPoint,
                            ReceiveCallBack,
                            ipEndPoint);
                    }
                //try { Console.WriteLine($"UDPServer.Receive: packet from {(ipEndPoint as IPEndPoint).Address}:{(ipEndPoint as IPEndPoint).Port} length={Length} at {DateTime.Now:O}"); } catch { }
                   
            }
        }
        private void BuildClient(object obj)
        {
            try
            {
                
            int nowIndex = 0;
            int msgLength = 0;
            int ID = 0;
            long nowSeq = -1;
            int chacheNum = 0;
            (byte[] bytes, int len,string key) info = ((byte[] bytes, int len,string key))obj;
            
            byte[] chacheBytes = info.bytes;
            chacheNum = info.len;
            string key = info.key;
            int type = -1;
            msgLength = -1;
            if (chacheNum >= 2)
            {
                if (chacheBytes.Length>=sizeof(short))
                {
                    type = BitConverter.ToInt16(chacheBytes, nowIndex);
                    nowIndex += 2;
                }
                ID = BitConverter.ToInt32(chacheBytes, nowIndex);
                nowIndex += 4;
                msgLength = BitConverter.ToInt32(chacheBytes, nowIndex);
                nowIndex += 4;

                if (chacheNum - nowIndex >= msgLength && msgLength != -1)
                {
                    BaseMsg baseMsg = null;
                    baseMsg = MsgPool.Instance.GetMsg(ID);
                    if (baseMsg != null)
                    {
                        baseMsg.Reading(chacheBytes, nowIndex);
                        BaseHandler baseHandler = MsgPool.Instance.GetHandler(ID);
                        baseHandler.msg = baseMsg;
                        if (baseMsg is UdpPlayerAddMsg)
                        {
                            UdpPlayerAddMsg playerAddMsg= baseMsg as UdpPlayerAddMsg;
                            ClientDic.AddClient(key,playerAddMsg.playerId);
                            Console.WriteLine("玩家成功加入UDP" + playerAddMsg.playerId);
                            UDPConnectionBuildMsg msg = new UDPConnectionBuildMsg();
                            msg.DelayBufferFrame = MainClass.frameManager.DelayBufferFrames;
                            msg.ServerLogicFrame = MainClass.frameManager.ReadLogicFrame();
                            MainClass.udpserver.SendMessage(msg,
                               GetIPEndPointFromClientDic(key),
                                E_UDP_MSG_TYPE.SIMPLE);
                        }
                    }
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
        public void DoProcess(object obj)
        {
            try
            {
                while (isRunning)
                {
                    ShareinputsEach();
                    DoReceive();
                    // allow other threads to run and avoid tight busy-looping
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
            try
            {
                    var clientDic=  ClientDic.GetClients();
                    foreach (var item in clientDic)
                    {
                        string strID = item.Key;
  

                        List<long> sortedKeys =new List<long>();
                        sortedKeys.AddRange(item.Value.inputsDic.Keys);
                        sortedKeys.Sort();

                        #region  控制每一帧每个玩家只能一个输入
                        // if(MainClass.frameManager.playerInputs.ContainsKey(item.Value.playerID))
                        // {
                        //     if (MainClass.frameManager.playerInputs[item.Value.playerID].Count != 0)
                        //     {
                        //         continue;
                        //     }
                        // }
                        #endregion

                        
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
                        lock(socket)
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
                Dictionary<string, UDPClient> newDIc = ClientDic.GetClients();
                lock (socket)
                {
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
        }
        private void SendCallBack(IAsyncResult ar)
        {
            lock(socket)
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
