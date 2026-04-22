using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AsyncServer.Tcp
{
    public class ServerSocket
    {
        public Dictionary<int,ClientSocket> ClientSocketDic=new Dictionary<int,ClientSocket>();
        
        public Socket serverSocket;
        private string ip;
        private  int port;
       public ServerSocket(string ip,int port,int clientNum)
        {
            this.serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            this.ip = ip;
            this.port = port;
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ip),port));
            serverSocket.Listen(clientNum);
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.Completed += Accept;
            serverSocket.AcceptAsync(e);
            
        }
        public void Accept(object obj, SocketAsyncEventArgs e)
        {
            try
            {
               
                
                    if (e.SocketError==SocketError.Success)
                    {
                        ClientSocket clientSocket = new ClientSocket(e.AcceptSocket);
                        ClientSocketDic.Add(clientSocket.ID, clientSocket);
                        Console.WriteLine("客户端{0}已经接入",clientSocket.ID);
                    }
                    e.AcceptSocket = null;
                (obj as Socket).AcceptAsync(e);
               
                
            }
            catch(SocketException ex){
                Console.WriteLine(ex.Message);
            }
        }
        public void SendToSpecialOne(BaseMsg baseMsg,int number)
        {
            if(ClientSocketDic.ContainsKey(number))
            {
                ClientSocketDic[number].Send(baseMsg);
            }
        }
       
    }
}
