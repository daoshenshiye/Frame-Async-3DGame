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

namespace ClientSocket.TCP
{
    public class ServerSocket
    {
        public string ip;
        public int port;
        public Socket socket;
        public  ConcurrentDictionary<int,ClientSocket> clientSockets;
        
        public Dictionary<int, Dictionary<int, ClientSocket>> rooms;
        private List<ClientSocket> DelClientSockets;
        public bool ShouldOpenThread;
        public  ServerSocket(string ip,int port)
        {
            rooms= new Dictionary<int, Dictionary<int, ClientSocket>>();
            clientSockets =new ConcurrentDictionary<int, ClientSocket> ();
            DelClientSockets=new List<ClientSocket> ();
            this.ip=ip;
            this.port=port;
            ShouldOpenThread = true;
            
            ThreadPool.QueueUserWorkItem(Accept);
            
        }
        public void Accept( object obj)
        {

            socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            //socket.Bind(new IPEndPoint(IPAddress.Parse(ip),port));
            socket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            socket.Listen(100);
           
            while (ShouldOpenThread)
            {
                try
                {
                   
                   Socket client= socket.Accept();
                    Console.WriteLine("客户端接入，远端：" + client.RemoteEndPoint);
                    ClientSocket clientSocket = new ClientSocket(client);
                    clientSockets.GetOrAdd(clientSocket.ID, clientSocket);
                    Console.WriteLine("客户端" + clientSocket.ID + "接入");
                    ThreadPool.QueueUserWorkItem((obj) =>
                    {
                        PlayerAccessInfoMsg playerAccess = new PlayerAccessInfoMsg();
                        playerAccess.PlayerId = clientSocket.ID;
                        playerAccess.PlayerNickName = "saberalter";
                        playerAccess.username = "saber";
                        playerAccess.password = "alter";
                        clientSocket.SendMessage(playerAccess);
                        TCPConnectionBuildMsg tcpConnect = new TCPConnectionBuildMsg();
                        clientSocket.SendMessage(tcpConnect);
                    });
                }
                catch(Exception e)
                {
                  Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);

                }
            }
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
                        foreach (var item in rooms[roomId])
                        {
                            if (item.Key != playerID)
                            {

                                PlayerCharacterCreateMsg playerCharacterCreateMsg = new PlayerCharacterCreateMsg();
                                playerCharacterCreateMsg.PlayerId = playerID;

                                PlayerEnterRoomMsg enterRoomMsg1 = new PlayerEnterRoomMsg();
                                enterRoomMsg1.playerId = playerID;
                                enterRoomMsg1.roomId = roomId;
                                enterRoomMsg1.success = true;

                                clientSockets[item.Key].SendMessage(playerCharacterCreateMsg);
                                clientSockets[item.Key].SendMessage(enterRoomMsg1);
                                PlayerCharacterCreateMsg playermsg = new PlayerCharacterCreateMsg();
                                playermsg.PlayerId = item.Key;

                                enterRoomMsg1.playerId = item.Key;
                                clientSockets[playerID].SendMessage(playermsg);
                                clientSockets[playerID].SendMessage(enterRoomMsg1);
                            }
                        }
                        PlayerCharacterCreateMsg playermsg1 = new PlayerCharacterCreateMsg();
                        playermsg1.PlayerId = playerID;
                        clientSockets[playerID].SendMessage(playermsg1);
                        PlayerEnterRoomMsg enterRoomMsg = new PlayerEnterRoomMsg();
                        enterRoomMsg.success = true;
                        enterRoomMsg.playerId = playerID;
                        enterRoomMsg.roomId = roomId;
                        clientSockets[playerID].SendMessage(enterRoomMsg);
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
                socket.Close();
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
