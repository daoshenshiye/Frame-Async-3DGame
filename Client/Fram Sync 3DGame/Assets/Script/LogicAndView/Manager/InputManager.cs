using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class InputManager
{
    private static InputManager instance;
    public static InputManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new InputManager();
            }
            return instance;
        }
    }
    
    public  Dictionary<long,Dictionary<int,Vector3>> playerInputs=new Dictionary<long, Dictionary<int, Vector3>>();
  
  

    public void AddVisitorInput(long currentLogicFram,int playerId,Vector3 Dir)
    {
        if (Dir==Vector3.zero)
        {
            return;
        }
      if(!playerInputs.ContainsKey(currentLogicFram))
        {

            Dictionary<int, Vector3> inputs=new Dictionary<int, Vector3>();
            

                inputs.Add(playerId, Dir);
            
            playerInputs.Add(currentLogicFram, inputs);
        }
        else
        {
              
            //if (playerInputs[currentLogicFram].ContainsKey(playerId))
            //{
            //    playerInputs[currentLogicFram][playerId] = Dir;
            //    Debug.Log("合并帧");
            //}
            if(!playerInputs[currentLogicFram].ContainsKey(playerId))
            {
                playerInputs[currentLogicFram].Add(playerId, Dir);
            }
         
        }
    }
    public void RemoveFrameVisitorInput(long currentLogicFram)
    {
        if(playerInputs.ContainsKey(currentLogicFram))
        {
            playerInputs.Remove(currentLogicFram);
        }
    }


}
