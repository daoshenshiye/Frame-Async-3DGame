using System;
using System.Diagnostics;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameSystem{
		public class ServerFrameAuthenMsgHandler:BaseHandler{
		public override void HandlerDo(){
			GameSystem.ServerFrameAuthenMsg message=msg as  GameSystem.ServerFrameAuthenMsg;
			try
			{
                if (message != null)
                {
                    float x;
                    float z;

                    foreach (var item in message.ServerInputStateData)
                    {
                        x = item.inputdata.Horizontal;
                        z = item.inputdata.Vertical;
                        if (InputManager.Instance.playerInputs.ContainsKey(message.serLogicFrame))
                        {
                            UnityEngine.Debug.Log("有相同的帧");
                        }
                        InputManager.Instance.AddVisitorInput(message.serLogicFrame, item.playerId
                       , new UnityEngine.Vector3(x, 0, z));
                    }

                    
                    FrameManager.Instance.UpdateLogicFrame(message);
                    
                }
            }
			catch (Exception e)
			{
				UnityEngine.Debug.LogException(e);
			}
			
		}
		}
}