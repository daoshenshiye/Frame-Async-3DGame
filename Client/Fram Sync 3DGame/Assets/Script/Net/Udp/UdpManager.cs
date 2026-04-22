using GameMessage;
using GameMsg;
using GameSystem;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;
using UnityEngine.XR;


public enum E_UDP_MSG_TYPE
{
    SIMPLE,
    ORDER_STEADY
}
public class DataPackage
{
    public long nowseq;
    public BaseHandler handler;
    public DataPackage(long seq,BaseHandler handle)
    {
        nowseq = seq;
        handler = handle;
    }

}
public class UdpManager: MonoBehaviour 
{
    private static UdpManager instance;
    public static UdpManager Instance=>instance;
    private static string UDPSERVER_IP = "112.17.30.188";
    private static int UDPSERVER_port = 29010;
    public static long nowSequence = 0;
    public static long expectedSequence = 0;
    public Socket socket;
    private EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any,0);
    public Queue<BaseHandler> receiveQueue = new Queue<BaseHandler>();
    private Queue<BaseHandler> SimpleMsgQueue = new Queue<BaseHandler>();
    private Queue<BaseMsg> steadySendQueue = new Queue<BaseMsg>();
    private Queue<BaseMsg> sendQueue = new Queue<BaseMsg>();
    public Dictionary<long, BaseHandler> MesgDic=new Dictionary<long, BaseHandler>();
    private bool isTime_OUT_LOGIC_START = false;
    private DateTime TimeOutStartTime;
    //private bool should_Start_TimeOut_LOGIC=false;
    //private bool TimeOutHasReceivedNowSeq = false;
    private const float _timeoutDuration = 4f;
    private bool shouldOpenUdp= false;
    private List<DataPackage> WillAddBuff = new List<DataPackage>();
    private long preLogicFrame =-1;
    private float timer;
    public int Counter;
    
    private void Awake()
    {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
    }
    public void InitUdp()
    {
        
        if (socket != null) return;

        socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any,8500));
        shouldOpenUdp=true;

        ThreadPool.QueueUserWorkItem(SendSteadyMsg);
        ThreadPool.QueueUserWorkItem(SendSimpleMsg);
        ThreadPool.QueueUserWorkItem(UDPReceive);
        ThreadPool.QueueUserWorkItem(HandleBusinessLogic);
        InvokeRepeating("SendHeartMsg", 0, 1);
    }
    public void SendHeartMsg()
    {
        if (socket == null) return;
        UDPSend(new HeartMsg(),E_UDP_MSG_TYPE.SIMPLE);
        print("发送了心跳消息UDP");
    }
   public void UDPSend(BaseMsg msg,E_UDP_MSG_TYPE type=E_UDP_MSG_TYPE.ORDER_STEADY)
    {
        if(msg!=null)
        {
            switch (type)
            {
                case E_UDP_MSG_TYPE.SIMPLE:
                    sendQueue.Enqueue(msg);
                    break;
                case E_UDP_MSG_TYPE.ORDER_STEADY:
                    steadySendQueue.Enqueue(msg);
                    break;
                default:
                    break;
            }
            
        }
      
    }
    private void SendSteadyMsg(object obj)
    {
        while (shouldOpenUdp)
        {
            try
            {
                
                if (steadySendQueue.Count > 0)
                {
                    byte[] bytes = steadySendQueue.Dequeue().Writting();
                    AddSequenceToData(ref bytes);
                    socket.SendTo(bytes, new IPEndPoint(IPAddress.Parse(UDPSERVER_IP), UDPSERVER_port));
                    ++nowSequence;
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
            catch (Exception e) {

                print(e.Message);
            }

        }
       
    }
    private void SendSimpleMsg(object obj)
    {
        int ID = 0;
        while (shouldOpenUdp)
        {
            
            try
            {
                if (sendQueue.Count > 0)
                {
                   BaseMsg msg= sendQueue.Dequeue();
                   ID= msg.GetID();
                   byte [] bytes= msg.Writting();
                    if(bytes!=null)
                    {
                        AddSimpleHeadToData(ref bytes);
                        socket.SendTo(bytes, new IPEndPoint(IPAddress.Parse(UDPSERVER_IP), UDPSERVER_port));
                    }


                }
                else
                {
                    Thread.Sleep(1);
                }

            }
            catch (Exception e)
            {
                print(e.Message);
                print(ID);
            }
            
        }
    }
    private void UDPReceive(object obj)
    {
        
            while (shouldOpenUdp)
            {


                    byte[] bytes = new byte[8192];

                    int Length = socket.ReceiveFrom(bytes, ref remoteEndPoint);
            if (Length>0)
            {
                byte[] new_bytes = new byte[Length];
                Array.Copy(bytes, 0, new_bytes, 0, Length);
              
                HandleReceive(Length, new_bytes);
                
            }
                   
                
            }
        
   
       
       
    }

    public void HandleReceive(int receiveLength, byte[] bytes)
    {
        int nowIndex = 0;
        int msgLength = 0;
        int ID = 0;
        long nowSeq = -1;
        int chacheNum = 0;

        byte[] chacheBytes = bytes;
        chacheNum = receiveLength;

        while (true)
        {
            int type = BitConverter.ToInt16(chacheBytes, nowIndex);
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
                if(msgLength>=63)
                {
                    System.Diagnostics.Debugger.Break();
                }
                
                //print("ID:" + ID + "len" + msgLength+"nowSeq"+nowSeq);
                if (chacheNum - nowIndex >= msgLength && msgLength != -1)
                {
                    BaseMsg baseMsg = null;
                    baseMsg = MsgPool.Instance.GetMsg(ID);
                    if (baseMsg != null)
                    {
                        
                        baseMsg.Reading(chacheBytes, nowIndex);
                        BaseHandler baseHandler = MsgPool.Instance.GetHandler(ID);
                        baseHandler.msg = baseMsg;
                        if (type == 0)
                        {


                            SimpleMsgQueue.Enqueue(baseHandler);
                            
                        }
                        else if (type == 1)
                        {

                            print("收到了消息");
                                if (!MesgDic.ContainsKey(nowSeq))
                                {
                                    lock (WillAddBuff)
                                    {
                                        
                                        WillAddBuff.Add(new DataPackage(nowSeq, baseHandler));
                                    }

                                }
                                else
                                {
                                    MesgDic[nowSeq] = baseHandler;
                                }
                            //UdpAckMsg msg = new UdpAckMsg();
                            //msg.seq = nowSeq;
                            //UDPSend(msg, E_UDP_MSG_TYPE.SIMPLE);
                        }
                    }
                    nowIndex += msgLength;
                    if (nowIndex >= chacheNum)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
    }
    public void HandleBusinessLogic(object obj)
    {
        while (shouldOpenUdp)
        {
            try
            {
                OrderPackages();
                
                if ( UdpManager.Instance.receiveQueue.TryDequeue(out BaseHandler handler))
                {
                    if (handler != null)
                    {
                        handler.HandlerDo();
                    }
                  
                }
            }
            catch (Exception e)
            {
                print(e.Message);
                print(e.StackTrace);
            }
        }

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
        byte[] new_byte=new byte[bytes.Length+10];
        BitConverter.GetBytes((short)1).CopyTo(new_byte,0);
         BitConverter.GetBytes(nowSequence).CopyTo(new_byte,2);
        Array.Copy(bytes,0, new_byte,10, bytes.Length);
        bytes = new_byte;
    }
    public void OrderPackages()
    {
        lock (WillAddBuff)
        {
            for (int i = WillAddBuff.Count - 1; i >= 0; i--)
            {
                var pkg = WillAddBuff[i];
                if (pkg.nowseq < 0 || pkg.handler == null)
                {
                    WillAddBuff.RemoveAt(i);
                    continue;
                }


                    if (!MesgDic.ContainsKey(pkg.nowseq))
                    {
                    MesgDic.Add(pkg.nowseq, pkg.handler);
                    }
                    else
                    {
                    MesgDic[pkg.nowseq] = pkg.handler;
                    }
                
                WillAddBuff.RemoveAt(i);
            }
        }

        if (MesgDic.Count == 0)
        {
            isTime_OUT_LOGIC_START = false;
            return;
        }
       
        if (!MesgDic.ContainsKey(expectedSequence) && isTime_OUT_LOGIC_START == false)
        {
            isTime_OUT_LOGIC_START = true;
            TimeOutStartTime = DateTime.Now;
        }
        else if (MesgDic.ContainsKey(expectedSequence))
        {
            isTime_OUT_LOGIC_START = false;
        }
        if (isTime_OUT_LOGIC_START)
        {
            TimeSpan elapsed = DateTime.Now - TimeOutStartTime;
            if (elapsed.TotalSeconds <= _timeoutDuration)
            {
                return;
            }
            long minValidSeq = long.MaxValue;
            List<long> sortedKeys = new List<long>();
            sortedKeys.AddRange(MesgDic.Keys);
            sortedKeys.Sort();
            foreach (var item in sortedKeys)
            {

                if (item >= expectedSequence)
                {
                    minValidSeq = item;
                    break;
                }

            }
            if (minValidSeq != long.MaxValue)
            {
                expectedSequence = minValidSeq;
            }
            isTime_OUT_LOGIC_START = false;
        }
        

        //if (!sortedBuffer.ContainsKey(expectedSequence) && isTime_OUT_LOGIC_START==false)
        //{

        //    should_Start_TimeOut_LOGIC=true;
        //    isTime_OUT_LOGIC_START = true;
        //    TimeOutHasReceivedNowSeq = false;
        //}

        if (MesgDic.ContainsKey(expectedSequence))
            {
           
            timer = 0f;
                //TimeOutHasReceivedNowSeq = true;
                List<long> WillDeleteBuff = new List<long>();
                   
                        foreach (var item in MesgDic)
                        {
                            if (item.Key < expectedSequence)
                            {
                                 if (!WillDeleteBuff.Contains(item.Key))
                                        WillDeleteBuff.Add(item.Key);
                            }
                        }
   
                        for (int i = 0; i < WillDeleteBuff.Count; i++)
                        {
                            lock (MesgDic)
                            {
                                if (MesgDic.ContainsKey(WillDeleteBuff[i]))
                                {
                                    MesgDic.Remove(WillDeleteBuff[i]);
                                }
                            }
                           
                        }
                    
                    while (MesgDic.ContainsKey(expectedSequence))
                    {
                        
                        receiveQueue.Enqueue(MesgDic[expectedSequence]);
                        MesgDic.Remove(expectedSequence);
                        ++expectedSequence;
                    }
        }

    }

    //IEnumerator TIME_OUT_UPDATE_NOWSEQUENCE()
    //{
    //    print("协程计数");
    //    yield return new WaitForSeconds(0.7f);
       
    //    if (!TimeOutHasReceivedNowSeq)
    //    {
    //        print("没收到服务器Ack数据");
    //        long minValidSeq = long.MaxValue;
    //        lock (sortedBuffer)
    //        {
    //            foreach (var item in sortedBuffer)
    //            {

    //                if (item.Key >= expectedSequence)
    //                {
    //                    minValidSeq = item.Key;
    //                }
    //                break;
    //            }
    //            if (minValidSeq != long.MaxValue)
    //            {
    //                expectedSequence = minValidSeq;
    //            }
    //        }
    //    }
    //    isTime_OUT_LOGIC_START = false;
    //    TimeOutHasReceivedNowSeq = false;
    //}
    private void Update()
    {
        if (UdpManager.Instance.SimpleMsgQueue.TryDequeue(out BaseHandler handler1))
        {
            if (handler1 != null)
            {

                handler1.HandlerDo();
            }
        }
       



        //if(isTime_OUT_LOGIC_START && should_Start_TimeOut_LOGIC)
        //{
        //    StartCoroutine(TIME_OUT_UPDATE_NOWSEQUENCE());
        //    should_Start_TimeOut_LOGIC = false;
        //}
    }

    private void OnDestroy()
    {
       if(socket!=null)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            shouldOpenUdp = false;
            StopAllCoroutines();
        }
    }
}
