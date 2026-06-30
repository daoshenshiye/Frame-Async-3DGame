using ClientSocket.UDP;
using GameMessage;
using GamePlayer;
using GameSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using ClientSocket.Physics.Colliders;
using ClientSocket.ServerPlayer;
using ClientSocket.Tools;

namespace ClientSocket.TCP
{
    public class ServerSocket
    {
        private string ip;
        private int port;
        private  ConcurrentDictionary<int,ClientSocket> clientSockets;
        private Dictionary<int, Dictionary<int, ClientSocket>> rooms;
        private List<ClientSocket> DelClientSockets;
        private bool ShouldOpenThread;
        private Thread AcceptThread;
        public  ServerSocket(string ip,int port)
        {
            rooms= new Dictionary<int, Dictionary<int, ClientSocket>>();
            clientSockets =new ConcurrentDictionary<int, ClientSocket> ();
            DelClientSockets=new List<ClientSocket> ();
            this.ip=ip;
            this.port=port;
            ShouldOpenThread = true;
            AcceptThread = new Thread(Accept);
            AcceptThread.Start();
        }
        public void Accept()
        {
            while (ShouldOpenThread)
            {
                Socket listenSocket = null;
                try
                {
                    listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    
                    IPAddress bindAddr = ip == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(ip);
                    IPEndPoint bindEp = new IPEndPoint(bindAddr, port);
                    listenSocket.Bind(bindEp);
                    listenSocket.Listen(512);
                    Console.WriteLine($"服务端监听启动 {bindAddr}:{port}");
                    
                    while (ShouldOpenThread)
                    {
                        IAsyncResult ar = listenSocket.BeginAccept(null, null);
                       
                        if (!ar.AsyncWaitHandle.WaitOne(100))
                        {
                            continue;
                        }
                        // 拿到客户端连接
                        Socket client = listenSocket.EndAccept(ar);
                        Console.WriteLine("客户端接入，远端：" + client.RemoteEndPoint);
                        ClientSocket clientSocket = new ClientSocket(client);
                        clientSockets.GetOrAdd(clientSocket.ID, clientSocket);
                        Console.WriteLine("客户端" + clientSocket.ID + "接入");

                       
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            try
                            {
                                if (!clientSocket.isConnected) return;
                                TCPConnectionBuildMsg tcpConnect = new TCPConnectionBuildMsg();
                                clientSocket.SendMessage(tcpConnect);
                                PlayerAccessInfoMsg playerAccess = new PlayerAccessInfoMsg();
                                playerAccess.PlayerId = clientSocket.ID;
                                playerAccess.PlayerNickName = "saberalter";
                                playerAccess.username = "saber";
                                playerAccess.password = "alter";
                                clientSocket.SendMessage(playerAccess);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"下发初始消息失败：{ex.Message}");
                            }
                        });
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"监听异常，3秒后重启监听：{e.Message}");
                }
                finally
                {
                    listenSocket?.Close();
                    listenSocket = null;
                }
                Thread.Sleep(3000);
            }
            Console.WriteLine("监听线程完全退出");
        }
       
        public void CraeteRoom(int roomId,int playerId)
        {
            try
            {
                if (!rooms.ContainsKey(roomId))
                {

                    rooms.Add(roomId, new Dictionary<int, ClientSocket>());
                    RoomCreateMsg roomCreateMsg = new RoomCreateMsg();
                    roomCreateMsg.success = true;
                    roomCreateMsg.roomId = roomId;
                    roomCreateMsg.playerId = playerId;
                    if (clientSockets.ContainsKey(playerId))
                    {
                        clientSockets[playerId].SendMessage(roomCreateMsg);
                        Console.WriteLine("玩家" + playerId + "创建了房间,房间号为" + roomId);
                    }
                    else
                    {
                        Console.WriteLine("玩家" + playerId + "创建房间失败,房间号为" + roomId);
                    }

                }
                else
                {
                    Console.WriteLine("玩家" + playerId + "创建房间失败,房间号为" + roomId);
                    RoomCreateMsg roomCreateMsg = new RoomCreateMsg();
                    roomCreateMsg.success = false;
                    roomCreateMsg.roomId = roomId;
                    roomCreateMsg.playerId = playerId;
                    if (clientSockets.ContainsKey(playerId))
                    {
                        clientSockets[playerId].SendMessage(roomCreateMsg);
                    }
                    else
                    {
                        Console.WriteLine("玩家" + playerId + "创建房间失败,房间号为" + roomId);
                    }

                }
            }
            catch (Exception e) { 
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            
        }
        public void ForceExitRoom(int playerID)
        {
            try
            {
                Console.WriteLine("玩家" + playerID + "被强制离开了房间");
                foreach (var room in rooms)
                {
                    if (room.Value.ContainsKey(playerID))
                    {
                        PlayerExitRoomMsg playerExitRoomMsg = new PlayerExitRoomMsg();

                        ExitRoom(room.Key, playerID);

                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
           
        }   
        public void EnterRoom(int roomId,int playerID)
        {
            try
            {
                if (rooms.ContainsKey(roomId))
                {

                    if (rooms[roomId].ContainsKey(playerID))
                    {
                        PlayerEnterRoomMsg enterRoomMsg = new PlayerEnterRoomMsg();
                        enterRoomMsg.success = false;
                        enterRoomMsg.playerId = playerID;
                        enterRoomMsg.roomId = roomId;
                        if (clientSockets.ContainsKey(playerID))
                        {
                            clientSockets[playerID].SendMessage(enterRoomMsg);
                        }
                    }
                    else
                    {
                        rooms[roomId].Add(playerID, clientSockets[playerID]);
                        Console.WriteLine("玩家" + playerID + "进入了房间,房间号为" + roomId);
                        
                        Vector3 birthPos = new Vector3(0, 0, 0);
                        Player player = new Player(playerID,birthPos);
                        player.AddComponent<BoxCollider>();
                        
                        PlayerCharacterCreateMsg playermsg1 = new PlayerCharacterCreateMsg();
                        playermsg1.PlayerId = playerID;
                        playermsg1.BirthPos = player.Position.ToPlayerPosData();
                        
                        clientSockets[playerID].SendMessage(playermsg1);
                        PlayerEnterRoomMsg enterRoomMsg = new PlayerEnterRoomMsg();
                        enterRoomMsg.success = true;
                        enterRoomMsg.playerId = playerID;
                        enterRoomMsg.roomId = roomId;
                        clientSockets[playerID].SendMessage(enterRoomMsg);
                        foreach (var item in rooms[roomId])
                        {
                            if (item.Key != playerID)
                            {
                               Player enterRoomPlayer= PlayerManager.Instance.GetPlayer(playerID);
                               Player inRoomPlayer = PlayerManager.Instance.GetPlayer(item.Key);
                                PlayerCharacterCreateMsg playerCharacterCreateMsg = new PlayerCharacterCreateMsg();
                                playerCharacterCreateMsg.PlayerId = playerID;
                                playerCharacterCreateMsg.BirthPos = enterRoomPlayer.Position.ToPlayerPosData();
                            
                                
                                clientSockets[item.Key].SendMessage(playerCharacterCreateMsg);
                                
                                
                                PlayerCharacterCreateMsg playermsg = new PlayerCharacterCreateMsg();
                                playermsg.PlayerId = item.Key;
                                playermsg.BirthPos = inRoomPlayer.Position.ToPlayerPosData();
                                
                                
                                clientSockets[playerID].SendMessage(playermsg);
                            }
                        }
                    }
                }
                else
                {
                    PlayerEnterRoomMsg enterRoomMsg = new PlayerEnterRoomMsg();
                    enterRoomMsg.success = false;
                    enterRoomMsg.playerId = playerID;
                    enterRoomMsg.roomId = roomId;
                    clientSockets[playerID].SendMessage(enterRoomMsg);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            
            
        }

        public void ExitRoom(int roomId,int playerID)
        {
            try
            {
                if (rooms.ContainsKey(roomId))
                {
                    Console.WriteLine("玩家" + playerID + "离开了房间,房间号为" + roomId);
                    foreach (var item in rooms[roomId])
                    {

                        if (item.Key != playerID)
                        {
                            PlayerCharacterDestroyMsg playerCharacterDestroyMsg = new PlayerCharacterDestroyMsg();
                            playerCharacterDestroyMsg.PlayerId = playerID;
                            clientSockets[item.Key].SendMessage(playerCharacterDestroyMsg);
                        }
                    }
                    rooms[roomId].Remove(playerID);
                    PlayerExitRoomMsg exitRoomMsg = new PlayerExitRoomMsg();
                    exitRoomMsg.success = true;
                    exitRoomMsg.playerId = playerID;
                    exitRoomMsg.roomId = roomId;
                    PlayerManager.Instance.DeletePlayer(playerID);
                    if (clientSockets.ContainsKey(playerID))
                    {
                        clientSockets[playerID].SendMessage(exitRoomMsg);
                    }
                }
                else
                {
                    PlayerExitRoomMsg exitRoomMsg = new PlayerExitRoomMsg();
                    exitRoomMsg.success = false;
                    if (clientSockets.ContainsKey(playerID))
                    {
                        clientSockets[playerID].SendMessage(exitRoomMsg);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
        public void DestroyRoom(int roomId)
        {
            try
            {
                if (rooms.ContainsKey(roomId))
                {
                    foreach (var item in rooms[roomId])
                    {
                        PlayerExitRoomMsg playerExitRoomMsg = new PlayerExitRoomMsg();
                        SendToSpecialOne(item.Key, playerExitRoomMsg);
                    }
                    rooms.Remove(roomId);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
           
        }

        public void SendToSpecialOne(int ID,BaseMsg message)
        {
            try
            {

                    if (ID < 0 || ID >= clientSockets.Count)
                    {
                        Console.WriteLine("ID不合法");
                        return;
                    }
                    clientSockets[ID].SendMessage(message);
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
           
           
        }

        public void Close()
        {
            try
            {
                for (int i = 0; i < clientSockets.Count; i++)
                {
                    clientSockets[i].Close();
                }
                clientSockets.Clear();
                ShouldOpenThread = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
           
        }
        public void CloseClientSocket(ClientSocket clientSocket)
        {
            try
            {
           
                    if (clientSockets.ContainsKey(clientSocket.ID))
                    {
                        clientSockets.TryRemove(clientSocket.ID, out _);
                        Console.WriteLine("客户端{0}主动断开了", clientSocket.ID);
                        ForceExitRoom(clientSocket.ID);
                    }
                    clientSocket.Close();
                
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
          
          
        }

        
    }
}
