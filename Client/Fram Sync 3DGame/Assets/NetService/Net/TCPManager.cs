using GameMessage;
using GameMsg;
using GameSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
public class ChacheReceive
{
    public byte[] chacheBytes;
    public int chacheNum;
    public ChacheReceive()
    {
        chacheBytes = new byte[100];
        chacheNum = 0;
    }
    public ChacheReceive(byte[] bytes, int num)
    {
        chacheBytes = bytes;
        chacheNum = num;

    }
}
public class TCPManager: MonoBehaviour 
{
    private static TCPManager instance;

    public static TCPManager Instance=>instance;
    private List<ChacheReceive> chacheReceives = new List<ChacheReceive>();
    //private static string TCP_IP = "119.84.246.217";
    //private static int TCP_port = 36252;
    private Socket socket;
    private byte[] chacheBytes=new byte[1024*1024];
    private int chacheNum;
    Queue<BaseHandler> receiveQueue = new Queue<BaseHandler>();
    Queue<BaseMsg> sendQueue= new Queue<BaseMsg>();
    public bool isConnected
    {
        get { return socket != null && socket.Connected; }
    }
    public bool buildTCPConnection;
    private float SEND_HEART_MSG_TIME = 5f;
    private const int RetryDelay_MS = 1000; 
    private const int MaxRetryAttempts = 5;
    private int currentRetryAttempts = 0;   
    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
        InvokeRepeating("SendHeartMsg", 0, SEND_HEART_MSG_TIME);
    }
    private void SendHeartMsg()
    {
        try
        {
            if (isConnected && buildTCPConnection)
            {
                HeartMsg heartMsg = new HeartMsg();

                Send(heartMsg);
                print("发送心跳包TCP");
            }
        }
        catch (Exception e) {

            print(e.Message);
            print(e.StackTrace);

            return;
        }
        
    }
  
    public async Task  ConnectWith(string ip,int port)
    {
        if (isConnected)
        {
            return;
        }
        Close();
        IPEndPoint iPEndPoint=new IPEndPoint(IPAddress.Parse(ip), port);
      socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
        try
        {

            var connectTask = socket.ConnectAsync(ip, port);
            if (await Task.WhenAny(connectTask, Task.Delay(3000)) != connectTask)
            {
                socket.Close();
                throw new Exception("连接超时");
            }
            
           
            currentRetryAttempts = 0;
           
            if (socket.LocalEndPoint==null&& socket.RemoteEndPoint==null&&!socket.Connected)
            {
               
                print("连接失败");
            }
            else
            {
                print("连接成功, local=" + socket.LocalEndPoint + " remote=" + socket.RemoteEndPoint);
                print("连接成功");
                ThreadPool.QueueUserWorkItem(SendMesg);
                ThreadPool.QueueUserWorkItem(ReceiveMesg);
            }
           
        }
        catch(Exception e)
        {

          Debug.Log(e.Message);
            if (currentRetryAttempts<= MaxRetryAttempts)
            {
                currentRetryAttempts++;
                await Task.Delay(RetryDelay_MS);
                await ConnectWith(ip, port);
            }
            else
            {
                Debug.LogError("重连次数用尽，停止重连");
            }

        }
        
    }

    public void Send(BaseMsg message)
    {
        sendQueue.Enqueue(message);
    }
    private void SendMesg(object obj)
    {
      while(isConnected)
        {
            try
            {
                if (sendQueue.Count > 0)
                {
                    byte[] bs = sendQueue.Dequeue().Writting();

                    if (socket != null)
                        socket.Send(bs);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
            catch(Exception e)
            {
                print(e.Message);
                print(e.StackTrace);
            }
           
        }

    }

    private  void Close()
    {
       if(socket!=null)
        {
            try
            {
                if (isConnected&&buildTCPConnection)
                {
                    QuitMessage quit = new QuitMessage();

                    socket.Send(quit.Writting());
                }
                if (isConnected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
               
                socket = null;

            }
            catch (Exception e) {
                print(e.Message);
                print(e.StackTrace);
            }
          
        }
    }
    private void ReceiveMesg(object obj)
    {
        byte[] bytes = new byte[1024 * 1024];
        while (isConnected)
        {
            try
            { 
                    
                    int Length = socket.Receive(bytes);
                if (Length<=0)
                {
                    break;
                }
                    HandleReceive(bytes, Length);

                    #region 老版本的收消息switch-case

                    //int index = 0;
                    //int msgID = BitConverter.ToInt32(dataContainer, 0);
                    //index += 4;
                    //BaseMsg basemsg = new BaseMsg();
                    //switch (msgID)
                    //{
                    //    case 100:
                    //        PlayerMsg playerMsg = new PlayerMsg();
                    //        playerMsg.Reading(dataContainer, index);
                    //        basemsg=playerMsg;

                    //        break;
                    //    default:
                    //        break;
                    //}
                    //if(basemsg==null)
                    //{
                    //    continue;
                    //}
                    //receiveQueue.Enqueue(basemsg);

                    #endregion
                  
                
            }
            catch (Exception e)
            {
                print(e.Message);
                print(e.StackTrace);
            }
        }
    }
    public void HandleReceive(byte[] bytes,int receiveLength)
    {
        try
        {
            int nowindex = 0;
            int ID = 0;
            int msgLength = 0;
            if (chacheReceives.Count > 0)
            {
                for (int i = 0; i < chacheReceives.Count; i++)
                {

                    var chaches = chacheReceives[i];
                    if (chaches.chacheNum < 8)
                        continue;
                    int nowindex2 = 0;
                    int msgLength2 = 0;
                    int ID2 = 0;
                    ID2 = BitConverter.ToInt32(chaches.chacheBytes, nowindex2);
                    nowindex2 += 4;
                    msgLength2 = BitConverter.ToInt32(chaches.chacheBytes, nowindex2);
                    nowindex2 += 4;
                    if (msgLength2 + 8 == chaches.chacheNum + receiveLength)
                    {
                        chaches.chacheBytes.CopyTo(chacheBytes, 0);
                        bytes.CopyTo(chacheBytes, chaches.chacheNum);
                        chacheNum += receiveLength + chaches.chacheNum;
                        chacheReceives.RemoveAt(i);
                        break;
                    }
                }

            }
            if (chacheNum == 0)
            {
                bytes.CopyTo(chacheBytes, chacheNum);
                chacheNum += receiveLength;
            }
            while (true)
            {
                msgLength = -1;
                if (chacheNum - nowindex >= 8)
                {
                    ID = BitConverter.ToInt32(chacheBytes, nowindex);
                    nowindex += 4;
                    msgLength = BitConverter.ToInt32(chacheBytes, nowindex);
                    nowindex += 4;
                }
                if (chacheNum - nowindex >= msgLength && msgLength != -1)
                {
                    BaseMsg baseMsg = MsgPool.Instance.GetMsg(ID);
                    if (baseMsg != null)
                    {
                        baseMsg.Reading(chacheBytes, nowindex);
                        BaseHandler handler = MsgPool.Instance.GetHandler(ID);
                        handler.msg = baseMsg;
                        receiveQueue.Enqueue(handler);
                    }
                    nowindex += msgLength;
                    if (nowindex == chacheNum)
                    {
                        chacheNum = 0;
                        chacheBytes = new byte[1024 * 1024];
                        break;
                    }
                }
                else
                {

                    if (msgLength != -1)
                    {
                        nowindex -= 8;

                    }
                    int remaining = chacheNum - nowindex;
                    byte[] chacheBytes2 = new byte[Math.Max(0, remaining)];
                    int chacheNum2 = 0;
                    if (remaining > 0)
                    {
                        if (remaining > 100)
                        {
                            UnityEngine.Debug.Log("越界了");

                        }

                        Array.Copy(chacheBytes, nowindex, chacheBytes2, 0, remaining);
                        chacheNum2 = remaining;
                    }
                    chacheNum = 0;
                    chacheBytes = new byte[1024 * 1024];
                    chacheReceives.Add(new ChacheReceive(chacheBytes2, chacheNum2));
                    break;
                }
            }
        }
        catch (Exception e) { print(e.Message); print(e.StackTrace); 
        
        }
        

        #region 老版本的收消息switch-case
        //int nowindex = 0;
        //int ID = 0;
        //int msgLength = 0;
        //bytes.CopyTo(chacheBytes, chacheNum);
        //chacheNum += receiveLength;
        //while(true)
        //{
        //    msgLength = -1;
        //    if(chacheNum-nowindex>=8)
        //    {
        //       ID= BitConverter.ToInt32(chacheBytes,nowindex);
        //        nowindex += 4;
        //        msgLength = BitConverter.ToInt32(chacheBytes, nowindex);
        //        nowindex += 4;
        //    }
        //    if (chacheNum-8>=msgLength&&msgLength!=-1)
        //    {
        //        //BaseMsg baseMsg = null;
        //        //switch (ID)
        //        //{
        //        //    case 100:
        //        //        PlayerMsg playerMsg=new PlayerMsg();
        //        //        playerMsg.Reading(chacheBytes,nowindex);
        //        //        baseMsg = playerMsg;

        //        //        break;
        //        //    default:
        //        //        break;
        //        //}
        //        BaseMsg baseMsg = MsgPool.Instance.GetMsg(ID);
        //        if(baseMsg!=null)
        //        {
        //            baseMsg.Reading(chacheBytes, nowindex);
        //            BaseHandler handler = MsgPool.Instance.GetHandler(ID);
        //            handler.msg=baseMsg;
        //            receiveQueue.Enqueue(handler);
        //        }
        //        nowindex += msgLength;

        //        if(nowindex==chacheNum)
        //        {
        //            chacheNum = 0;
        //            break;
        //        }
        //    }
        //    else { 

        //        if(msgLength!=-1)
        //        {
        //            nowindex -= 8;

        //        }
        //        Array.Copy(chacheBytes,nowindex,chacheBytes,0,chacheNum-nowindex);
        //        chacheNum-=nowindex;
        //        break;
        //    }

        //}
        #endregion
    }

    void Update()
    {
        
        if(receiveQueue.Count>0)
        {
            receiveQueue.Dequeue().HandlerDo();
        }

    }
    private void OnDestroy()
    {
        Close();
    }
}
