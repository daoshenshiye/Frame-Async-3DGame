using GameMessage;
using GameMsg;
using GameSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetService.Net;
using UnityEngine;
using UnityEngine.UI;
public class TCPManager: MonoBehaviour 
{
    private static TCPManager instance;

    public static TCPManager Instance=>instance;
    // private List<ChacheReceive> chacheReceives = new List<ChacheReceive>();
    // private byte[] chacheBytes=new byte[1024*5];
    //private static string TCP_IP = "119.84.246.217";
    //private static int TCP_port = 36252;
    private Socket socket;
    
    private int chacheNum;
    Queue<BaseHandler> receiveQueue = new Queue<BaseHandler>();
    Queue<BaseMsg> sendQueue= new Queue<BaseMsg>();
    private bool OpenThread = false;
    private bool isconnected;
    public bool isConnected
    {
        get
        {
            isconnected=socket!=null&&socket.Connected;
            return isconnected;
        }
        private  set    
        {
            isconnected = value;
        }
    }

    private bool buildTCPConnection;

    public bool BuildTCPConnection
    {
        get { return buildTCPConnection; }
        set
        {
            buildTCPConnection = value;
            Debug.Log("buildConnectionSetted");
        }
    }
    private float SEND_HEART_MSG_TIME = 5f;
    private const int RetryDelay_MS = 1000; 
    private const int WaitServerConnection_MS = 10000;
    private const int WaitClientConnection_MS = 8000;
    private const int MaxRetryAttempts = 5;
    private int currentRetryAttempts = 0;   
    private Task ConnectTask;
    private Thread SendThread;
    private Thread ReceiveThread;
    private TcpMsgReceiveHandler _tcpMsgReceiveHandler;
    private CancellationTokenSource _handshakeCts;
    private readonly object _sendQueueLock = new object();
    private void Awake()
    {
        _tcpMsgReceiveHandler=new  TcpMsgReceiveHandler(1024*10);
        _tcpMsgReceiveHandler.OnMessageParsed = ProcessMsg;
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartSendTcpHeartLoop()
    {
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
        }
        
    }
  
    public async Task ConnectWith(string ip,int port)
    {
        // 取消上一轮残留的握手计时，杜绝多任务并发
        _handshakeCts?.Cancel();
        _handshakeCts = new CancellationTokenSource();
        var token = _handshakeCts.Token;

        if (socket==null)
        {
            socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            socket.ReceiveTimeout = 500;
            socket.SendTimeout = 500;
        }

        try
        {
            if (buildTCPConnection)
            {
                return;
            }
            ConnectTask = socket.ConnectAsync(ip, port);
            if (await Task.WhenAny(ConnectTask, Task.Delay(WaitClientConnection_MS, token)) != ConnectTask)
            {
                if (!socket.Connected)
                {
                    throw new Exception("连接超时,进行尝试");
                }
            }

            print("连接成功, local=" + socket.LocalEndPoint + " remote=" + socket.RemoteEndPoint);
            print("连接成功");
            
            OpenThread = true;
            if (SendThread == null || !SendThread.IsAlive)
            {
                SendThread = new Thread(SendMesg);
                SendThread.IsBackground = true;
                SendThread.Start();
            }
            if (ReceiveThread == null || !ReceiveThread.IsAlive)
            {
                ReceiveThread = new Thread(ReceiveMesg);
                ReceiveThread.IsBackground = true;
                ReceiveThread.Start();
            }
            _ = CheckBusinessHandshakeTimeout(ip, port, token);
        }
        catch (OperationCanceledException)
        {
            // 任务被主动取消，正常忽略
            Debug.LogWarning("上一轮握手计时已取消");
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            await SafeClose();
            if (currentRetryAttempts <= MaxRetryAttempts)
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
    /// <summary>等待业务确认包TCPconnectionBuild，超时自动断开重连</summary>
    private async Task CheckBusinessHandshakeTimeout(string ip, int port, CancellationToken token)
    {
        int waitMs = WaitServerConnection_MS;
        int step = 100;
        int totalWait = 0;
    
        while (totalWait < waitMs)
        {
            await Task.Delay(step, token);
            totalWait += step;

            if (buildTCPConnection)
            {
                OpenThread = true;
                if (ReceiveThread == null || !ReceiveThread.IsAlive)
                {
                    ReceiveThread = new Thread(ReceiveMesg);
                    ReceiveThread.IsBackground = true;
                    ReceiveThread.Start();
                }
                if (SendThread == null || !SendThread.IsAlive)
                {
                    SendThread = new Thread(SendMesg);
                    SendThread.IsBackground = true;
                    SendThread.Start();
                }
                return;
            }
            if (!isConnected)
            {
                break;
            }
        }
        Debug.LogWarning($"业务握手超时{waitMs}ms，断开重连");
        await Task.Delay(RetryDelay_MS);
        if (buildTCPConnection)
        {
            return;
        }
        await SafeClose();
        _ = ConnectWith(ip, port);
    }
    


    public void Send(BaseMsg message)
    {
        lock (_sendQueueLock)
        {
            sendQueue.Enqueue(message);
        }
    }
    private void SendMesg(object obj)
    {
        while (OpenThread)
        {
            BaseMsg msg=null;
            try
            {
                lock (_sendQueueLock)
                {
                    if (buildTCPConnection && sendQueue.Count > 0)
                    {
                        msg = sendQueue.Dequeue();
                    }
                }
                if (msg != null)
                {
                    byte[] bs = msg.Writting();
                    if (socket != null)
                        socket.Send(bs);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
            catch (SocketException e)
            {
                Debug.LogError($"发送Socket错误：{e.SocketErrorCode}");
            }
            catch (Exception e)
            {
                Debug.LogError($"发送异常：{e.Message}");
            }

        }
    }
    private async Task SafeClose()
    {
        OpenThread = false;
        buildTCPConnection = false;
        // 等待线程完全退出，延长等待时间
        if (ReceiveThread != null && ReceiveThread.IsAlive)
        {
            ReceiveThread.Join(500);
        }
        if (SendThread != null && SendThread.IsAlive)
        {
            SendThread.Join(500);
        }
        ReceiveThread = null; 
        SendThread = null;
        if (socket != null)
        {
         
            if (isConnected)
            {
                QuitMessage quit = new QuitMessage();
                socket.Send(quit.Writting());
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            socket = null;
        }
        _tcpMsgReceiveHandler.ResetReadIndex();
    }

    private void ReceiveMesg()
    {
        byte[] bytes = new byte[1024 * 5];
        while (OpenThread)
        {
            try
            { 
                print("进入了接收TCP");
                
                int Length = socket.Receive(bytes);
                if (Length<=0)
                {
                    Thread.Sleep(1);
                    continue;
                }
                
                HandleReceive(bytes, Length);
       
                Thread.Sleep(1);
             
            }
            catch (SocketException e)
            {
                switch (e.SocketErrorCode)
                {
                    case SocketError.TimedOut:
                        continue;
                    case SocketError.Interrupted:
                    case SocketError.ConnectionReset:
                    case SocketError.ConnectionAborted:
                    case SocketError.NotConnected:
                        buildTCPConnection = false;
                        OpenThread = false;
                        Debug.LogError($"连接断开，错误码：{e.SocketErrorCode}");
                        break;
                    default:
                        Debug.LogError($"未知Socket错误：{e.SocketErrorCode} {e.Message}");
                        break;
                }
                print("Socket异常断开：" + e.Message);
                break;
            }
        }
        
    }
    public void HandleReceive(byte[] bytes,int receiveLength)
    {
        _tcpMsgReceiveHandler.HandleReceiveMsg(bytes, receiveLength);
        
        #region 老版本的消息处理
        // try
        // {
               //     print("进入了TCP消息处理");
        //     int nowindex = 0;
        //     int ID = 0;
        //     int msgLength = 0;
        //     if (chacheReceives.Count > 0)
        //     {
        //         for (int i = 0; i < chacheReceives.Count; i++)
        //         {
        //
        //             var chaches = chacheReceives[i];
        //             if (chaches.chacheNum < 8)
        //                 continue;
        //             int nowindex2 = 0;
        //             int msgLength2 = 0;
        //             int ID2 = 0;
        //             ID2 = BitConverter.ToInt32(chaches.chacheBytes, nowindex2);
        //             nowindex2 += 4;
        //             msgLength2 = BitConverter.ToInt32(chaches.chacheBytes, nowindex2);
        //             nowindex2 += 4;
        //             if (msgLength2 + 8 == chaches.chacheNum + receiveLength)
        //             {
        //                 chaches.chacheBytes.CopyTo(chacheBytes, 0);
        //                 bytes.CopyTo(chacheBytes, chaches.chacheNum);
        //                 chacheNum += receiveLength + chaches.chacheNum;
        //                 chacheReceives.RemoveAt(i);
        //                 break;
        //             }
        //         }
        //
        //     }
        //     if (chacheNum == 0)
        //     {
        //         bytes.CopyTo(chacheBytes, chacheNum);
        //         chacheNum += receiveLength;
        //     }
        //     while (true)
        //     {
        //         msgLength = -1;
        //         if (chacheNum - nowindex >= 8)
        //         {
        //             ID = BitConverter.ToInt32(chacheBytes, nowindex);
        //             nowindex += 4;
        //             msgLength = BitConverter.ToInt32(chacheBytes, nowindex);
        //             nowindex += 4;
        //         }
        //         if (chacheNum - nowindex >= msgLength && msgLength != -1)
        //         {
        //             BaseMsg baseMsg = MsgPool.Instance.GetMsg(ID);
        //             if (baseMsg != null)
        //             {
        //                 print("收到了TCP消息");
        //                 baseMsg.Reading(chacheBytes, nowindex);
        //                 BaseHandler handler = MsgPool.Instance.GetHandler(ID);
        //                 handler.msg = baseMsg;
        //                 receiveQueue.Enqueue(handler);
        //             }
        //             nowindex += msgLength;
        //             if (nowindex == chacheNum)
        //             {
        //                 chacheNum = 0;
        //                 break;
        //             }
        //         }
        //         else
        //         {
        //             if (msgLength != -1)
        //             {
        //                 nowindex -= 8;
        //
        //             }
        //             int remaining = chacheNum - nowindex;
        //             byte[] chacheBytes2 = new byte[Math.Max(0, remaining)];
        //             int chacheNum2 = 0;
        //             if (remaining > 0)
        //             {
        //                 if (remaining > 100)
        //                 {
        //                     UnityEngine.Debug.Log("越界了");
        //
        //                 }
        //
        //                 Array.Copy(chacheBytes, nowindex, chacheBytes2, 0, remaining);
        //                 chacheNum2 = remaining;
        //             }
        //             chacheNum = 0;
        //             chacheBytes = new byte[1024];
        //             chacheReceives.Add(new ChacheReceive(chacheBytes2, chacheNum2));
        //             break;
        //         }
        //     }
        // }
        // catch (Exception e) { print(e.Message); print(e.StackTrace); 
        //
        // }

            #endregion
     
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

    public void ProcessMsg(BaseHandler handler)
    {
        if (handler.msg is TCPConnectionBuildMsg)
        {
            BuildTCPConnection = true;
            currentRetryAttempts = 0;
            _handshakeCts?.Cancel();
        }
        receiveQueue.Enqueue(handler);
    }
    private void Update()
    {
        while(receiveQueue.Count > 0)
        {
            receiveQueue.Dequeue().HandlerDo();
        }
    }

    private void OnDestroy()
    {
        _handshakeCts?.Cancel();
        _handshakeCts?.Dispose();
        _ = SafeClose();
        OpenThread = false;
        _tcpMsgReceiveHandler = null;
    }
}
