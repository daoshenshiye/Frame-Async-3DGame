using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GameMessage
{
    public class PlayerCharacterDestroyMsgHandler : BaseHandler
    {
        public override void HandlerDo()
        {
            GameMessage.PlayerCharacterDestroyMsg message = msg as GameMessage.PlayerCharacterDestroyMsg;
            if(message!=null)
            {
                PlayerManager.Instance.RemovePlayerInstance(message.PlayerId);
                
            }
        }
    }
}

