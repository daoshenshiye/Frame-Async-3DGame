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
                if (PlayerManager.Instance.player_Dic.ContainsKey(message.PlayerId))
                {
                    GameObject.Destroy(PlayerManager.Instance.player_Dic[message.PlayerId].player_instance);
                    PlayerManager.Instance.player_Dic[message.PlayerId]=null;
                    PlayerManager.Instance.player_Dic.Remove(message.PlayerId);
                }
            }
        }
    }
}

