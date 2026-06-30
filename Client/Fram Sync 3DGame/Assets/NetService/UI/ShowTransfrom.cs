using GamePlayer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowTransfrom : MonoBehaviour
{
  public  Text text;
    public Text localText;
    public Text localInputTimes;
    public Text ReceiveTimes;
    public Text AddInputTimes;
    public Text CurrentRttText;
    public Button ExitRoom;
    PlayerCameraMove camera;
    // Start is called before the first frame update
    private void Awake()
    {


    }
    void Start()
    {
       camera=Camera.main.GetComponent<PlayerCameraMove>();
    }

    // Update is called once per frame
    void Update()
    {
        //if (PlayerManager.LocalPlayerID != -1)
        //{
        //    localText.text = PlayerManager.Instance.player_Dic[PlayerManager.LocalPlayerID].player_instance.transform.position.ToString();
        //}
        if (camera.Target != null)
            //text.text = camera.Target.position.ToString()+camera.Target.position.z;
            text.text = PlayerManager.LocalPlayerID.ToString();
        localInputTimes.text=FrameManager.Instance.localExecuteTimes.ToString();
        // ReceiveTimes.text = "发送数据量"+UdpManager.Instance.Counter.ToString();
        AddInputTimes.text = "加入输入字典的次数" + FrameManager.Instance.Counter;
        CurrentRttText.text = FrameManager.Instance.CurrentRTT.ToString();
    }
}
