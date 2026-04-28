using ClientSocket.TCP;
using ClientSocket.UDP;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClientSocket
{
    internal class MainClass
    {
        public static ServerSocket serverSocket;
        public static UDPServer udpserver;
        public static FrameManager frameManager;
        public static void  Main(string[] args)
        {
             serverSocket = new ServerSocket("0.0.0.0", 9000);
             udpserver =new UDPServer("0.0.0.0", 8250);
              frameManager = new FrameManager(15);
            string input = "";
            while (true)
            {
                Console.WriteLine("请输入指令");
                 input= Console.ReadLine();
                if (input == "Quit")
                {
                    serverSocket.Close();
                }
                else if (input == "SendTo")
                {


                   
                }
            }
           

        }
    }
}
