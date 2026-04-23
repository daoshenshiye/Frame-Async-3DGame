using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerInfo
{
    public int PlayerId;
    public string NickName;
    public bool isLocalPlayer;
    public string username;
    public string password;
    public string PlayerCharacterRes;
    public GameObject player_instance;
    public int nowInRoomId = -1;
    public GameObject GetPlayerInstance()
    {
        if (player_instance != null)
        {
            return player_instance;
        }
        return null;
    }
    public GameObject CreatePlayerInstance()
    {
        if (player_instance != null)
            return player_instance;
        if (PlayerCharacterRes != null)
        {
            player_instance = Object.Instantiate(Resources.Load<GameObject>(PlayerCharacterRes));
            player_instance.name = NickName;
            return player_instance;
        }
        else
        {
            return null;
        }



    }

}

public class PlayerManager
{
    private static PlayerManager instance;
    public static PlayerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new PlayerManager();
            }
            return instance;
        }
    }
    public static int LocalPlayerID=-1;
    public Dictionary<int,PlayerInfo> player_Dic=new Dictionary<int,PlayerInfo>();
    public void RegisterNewPlayer(PlayerInfo playerInfo)
    {
        if(!player_Dic.ContainsKey(playerInfo.PlayerId))
        {
            player_Dic.Add(playerInfo.PlayerId, playerInfo);
        }
    }
    public void CreateNewPlayer(PlayerInfo playerInfo)
    {
        if (player_Dic.ContainsKey(playerInfo.PlayerId))
        {
            GameObject obj = playerInfo.CreatePlayerInstance();
          PlayerView playerView=  obj.AddComponent<PlayerView>();
           PlayerCameraMove playerCameraMove =Camera.main.GetComponent<PlayerCameraMove>();
            playerCameraMove.Target = obj.transform;
            UnityEngine.Debug.Log("创建了玩家实例"+playerInfo.PlayerId);
            LogicViewBridge.Instance.AddPlayer(playerInfo.PlayerId, new PlayerLogic(playerInfo.PlayerId, Vector3.zero,10), playerView);
            
        }

    }
    public void RemovePlayerInstance(int playerId) {
    
    if (player_Dic.ContainsKey(playerId))
        {
            //if(playerId==LocalPlayerID)
            //{
            //    LocalPlayerID = -1;
            //}
            GameObject player = player_Dic[playerId].GetPlayerInstance();
            GameObject.Destroy(player);
            LogicViewBridge.Instance.DeletePlayer(playerId);

            player_Dic[playerId]=null;
            
            player_Dic.Remove(playerId);
            FrameManager.Instance.LocallogicView = null;
        }
    }
}
