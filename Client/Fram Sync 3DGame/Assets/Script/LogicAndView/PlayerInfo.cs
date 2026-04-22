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
    public int nowInRoomId=-1;
    public GameObject GetPlayerInstance()
    {
        if (player_instance!=null)
        {
            return player_instance;
        }
        return null;
    }
    public GameObject CreatePlayerInstance()
    {
        if (player_instance!=null)
            return player_instance;
        if (PlayerCharacterRes!=null)
        {
            player_instance = Object.Instantiate(Resources.Load<GameObject>(PlayerCharacterRes));
            player_instance.name = NickName;
            return player_instance;
        }
        else{
            return null;
        }
        

        
    }
    
}
