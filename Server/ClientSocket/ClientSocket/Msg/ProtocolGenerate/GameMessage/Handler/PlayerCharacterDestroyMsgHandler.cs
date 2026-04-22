using System.Collections;
using System.Collections.Generic;

namespace GameMessage
{
    public class PlayerCharacterDestroyMsgHandler : BaseHandler
    {
        public override void HandlerDo()
        {
            GameMessage.PlayerCharacterDestroyMsg message = msg as GameMessage.PlayerCharacterDestroyMsg;
            if (message != null) { 
            
            }
        }
    }
}

