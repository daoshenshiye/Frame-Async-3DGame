using AsyncServer.Tcp;

namespace AsyncServer
{
    internal class Program
    {
        public static ServerSocket serverSocket;
        static void Main(string[] args)
        {
            serverSocket = new ServerSocket("127.0.0.1", 8080, 10);
            while (true) {
                Console.WriteLine("请输入指令");
            string input= Console.ReadLine();   
                if(input=="Send")
                {
                    Console.WriteLine("请选择客户端号");
                    int num = Int32.Parse(Console.ReadLine());
                    PlayerMsg playerMsg = new PlayerMsg();
                    playerMsg.playerId = 55;
                   playerMsg.playerData=new PlayerData();
                    playerMsg.playerData.level = 100;
                    playerMsg.playerData.hp = 500;
                    playerMsg.playerData.name = "无敌了";
                    serverSocket.SendToSpecialOne(playerMsg,num);
                  
                }
            
            }
        }
    }
}
