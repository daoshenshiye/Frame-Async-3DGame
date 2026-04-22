using ClientSocket;

namespace GameMessage{
		public class PlayerCharacterCreateMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GameMessage.PlayerCharacterCreateMsg message=msg as  GameMessage.PlayerCharacterCreateMsg;
            //if (message != null)
            //{
            //    lock (MainClass.serverSocket.inRoomPlayers)
            //    {
            //        foreach (var item in MainClass.serverSocket.inRoomPlayers)
            //        {
            //            if (item.Key==message.PlayerId)
            //            {
            //                continue;

            //            }
            //            PlayerCharacterCreateMsg playerCharacterCreateMsg = new PlayerCharacterCreateMsg();
            //            playerCharacterCreateMsg.PlayerId =item.Key;
            //            MainClass.serverSocket.clientSockets[message.PlayerId].SendMessage(playerCharacterCreateMsg);
                        
            //        }
            //        if(!MainClass.serverSocket.inRoomPlayers.ContainsKey(message.PlayerId))
            //        {
            //            MainClass.serverSocket.inRoomPlayers.Add(message.PlayerId, 
            //                MainClass.serverSocket.clientSockets[message.PlayerId]);
            //        }
            //    }
               
            //}
        }
		}
}