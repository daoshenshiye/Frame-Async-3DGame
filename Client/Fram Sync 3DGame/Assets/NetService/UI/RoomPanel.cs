using GamePlayer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomPanel : MonoBehaviour
{
    public Button ExitBtn;
    public Button EnterRoomBtn;
    public Button CreateRoomBtn;
    public InputField inputField;
    private void Awake()
    {
        ExitBtn.onClick.AddListener(() =>
        {
            if (PlayerManager.Instance.player_Dic[PlayerManager.LocalPlayerID].nowInRoomId!=-1)
            {
                PlayerExitRoomMsg msg = new PlayerExitRoomMsg();
                msg.playerId = PlayerManager.LocalPlayerID;
                msg.roomId = PlayerManager.Instance.player_Dic[PlayerManager.LocalPlayerID].nowInRoomId;
                TCPManager.Instance.Send(msg);
            }
          
        });
        CreateRoomBtn.onClick.AddListener(() =>
        {
            if (Int32.TryParse(inputField.text,out int result))
            {
                if (result>0)
                {

                    RoomCreateMsg msg = new RoomCreateMsg();

                    msg.roomId = result;
                    msg.playerId = PlayerManager.LocalPlayerID;
                    print(result);
                    if (PlayerManager.LocalPlayerID==-1)
                    {
                        print("玩家ID异常");
                        return;
                    }
                    TCPManager.Instance.Send(msg);
                }
            }
          
        });
        EnterRoomBtn.onClick.AddListener(() =>
        {
            if (Int32.TryParse(inputField.text, out int result))
            {
                if (result > 0)
                {
                    PlayerEnterRoomMsg msg = new PlayerEnterRoomMsg();
                    msg.roomId= result;
                    msg.playerId = PlayerManager.LocalPlayerID;
                    TCPManager.Instance.Send(msg);
                }
            }
        });
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
