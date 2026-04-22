using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace AsyncServer.Tcp
{
    public class Chache {
        public byte[] chacheBytes;
        public int chacheNum;
        public Chache(byte[] bytes,int num)
        {
            this.chacheBytes = bytes;
            this.chacheNum = num;

        }
    
    }

    public class ClientSocket
    {
        public Socket socket;
        public int ID=0;
        public static int Begin_ID = 0;
        private byte[] chacheBytes=new byte[1024*1024];
        private int chacheNum=0;
        private List<Chache> chacheList=new List<Chache>();
        private bool isConnected =>socket.Connected;
        public ClientSocket(Socket socket) {
            this.socket = socket;
            ID = Begin_ID;
            ++Begin_ID;
            Receive();
        }
        public void Send(BaseMsg baseMsg)
        {
            if (!isConnected)
            {
                return;
            }
            try
            {
               SocketAsyncEventArgs e=new SocketAsyncEventArgs();
                byte[] bytes = baseMsg.Writting();
                e.SetBuffer(bytes,0,bytes.Length);
                e.Completed += (sock, args) =>
                {
                    if(args.SocketError==SocketError.Success)
                    {

                    }
                    else
                    {

                    }
                };
                socket.SendAsync(e);
            }
            catch(SocketException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void Receive()
        {
            if(!isConnected)
            {
                return; 
            }
            SocketAsyncEventArgs e=new SocketAsyncEventArgs();
            byte[] bytes=new byte[1024*100];
            e.SetBuffer(bytes, 0, bytes.Length);
            e.Completed += (sockets, args) =>
            {
                if(args.SocketError==SocketError.Success)
                {
                    HandleReceive(args.Buffer,args.BytesTransferred);
                    args.SetBuffer(0,args.Buffer.Length);
                    (sockets as Socket).ReceiveAsync(args);

                }
                else
                {

                }
                
            };
            socket.ReceiveAsync(e);
        }
        public void HandleReceive(byte[] bytes,int receiveLength)
        {
            int ID = 0;
            int msgLength = 0;
            int nowindex = 0;
            if (chacheList.Count>0)
            {
                foreach (var item in chacheList)
                {
                    int ID2 = 0;
                    int msgLength2 = 0;
                    int nowindex2 = 0;
                    ID2 = BitConverter.ToInt32(item.chacheBytes,nowindex2);
                    nowindex2 += 4;
                    msgLength2 = BitConverter.ToInt32(item.chacheBytes, nowindex2);
                    nowindex2 += 4;
                    if (msgLength2+8==item.chacheNum+receiveLength)
                    {
                        item.chacheBytes.CopyTo(chacheBytes,0);
                        bytes.CopyTo(chacheBytes,item.chacheNum);
                        chacheNum = item.chacheNum + receiveLength;
                        chacheList.Remove(item);
                        break;
                    }
                    
                }
            }
            if (chacheNum==0)
            {
                bytes.CopyTo(chacheBytes, chacheNum);
                chacheNum += receiveLength;
            }
            while (true) {
                msgLength = -1;
                if (chacheNum-nowindex>=8)
                {
                  ID=  BitConverter.ToInt32(chacheBytes,nowindex);
                    nowindex += 4;
                    msgLength=BitConverter.ToInt32(chacheBytes, nowindex);
                    nowindex += 4;
                }
                if (msgLength!=-1&&chacheNum-nowindex>=msgLength) 
                {
                    BaseMsg baseMsg = null;
                    switch (ID)
                    {
                        case 100:

                            baseMsg = new PlayerMsg();
                            baseMsg.Reading(chacheBytes, nowindex);

                            break;
                        case 404:
                            baseMsg = new QuitMessage();
                            break;
                        case 505:
                            baseMsg = new HeartMsg();
                            break;
                        default:
                            break;
                    }
                    if(baseMsg!=null)
                    {
                        ThreadPool.QueueUserWorkItem(HandleReceiveMsg,(socket,baseMsg));
                    }
                    nowindex += msgLength;
                    if(nowindex==chacheNum)
                    {
                        chacheNum = 0;
                        break;
                    }

                }
                else
                {
                    if (msgLength!=-1)
                    {
                        nowindex -= 8;
                    }
                    byte[] chacheByte2=new byte[100];
                    int num = 0;
                    Array.Copy(chacheBytes,nowindex,chacheByte2,0,chacheNum-nowindex);
                    num=chacheNum-nowindex;
                    chacheNum = 0;
                    chacheList.Add(new Chache(chacheByte2,num));
                    break;
                }


            }
        }
        public void HandleReceiveMsg(object obj)
        {
            (Socket s, BaseMsg bm) info = ((Socket s, BaseMsg bm))obj;
            if (info.bm is PlayerMsg)
            {
                PlayerMsg playerMsg = (PlayerMsg)info.bm;
                Console.WriteLine("客户端{0}:",ID);
                Console.WriteLine(playerMsg.playerId);
                Console.WriteLine(playerMsg.playerData.name);
                Console.WriteLine(playerMsg.playerData.hp);
                Console.WriteLine(playerMsg.playerData.level);

            }else if (info.bm is HeartMsg)
            {
                
            }
            else if (info.bm is QuitMessage)
            {

            }
        }
    }
}
