using ClientSocket;
using ClientSocket.UDP;
using GamePlayer;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime.InteropServices;
namespace GameMessage{
        public class ClientInput
        {
            public int  playerId;
            public long predictFrame;
            public PlayerStateData state;
            public InputData input;
        }
		public class InputMessageHandler:BaseHandler{
        //private static int player1;
        //private static int player2;
       
		public override void HandlerDo(){
			GameMessage.InputMessage message=msg as  GameMessage.InputMessage;
            if (message != null) {
                ClientInput ClientInput= LoadInputData(message);
                // use thread-safe GetOrAdd to obtain per-player queue and enqueue the input
                
                var frameDict = MainClass.frameManager.frameInputBuffer.GetOrAdd(
                    ClientInput.predictFrame,
                   new ConcurrentDictionary<int, ClientInput>()
                );
                frameDict[message.PlayerId] = ClientInput;
            }
        }
        public ClientInput LoadInputData(InputMessage msg)
        {
            ClientInput clientinput = new ClientInput();
            clientinput.input = msg.input;
            clientinput.playerId = msg.PlayerId;
            clientinput.predictFrame = msg.PredictFrame;
            
            return clientinput;
        }
       
    }
}