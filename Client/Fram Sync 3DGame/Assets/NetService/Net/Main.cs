using GameSystem;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Main : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (FrameManager.Instance == null)
        {
            GameObject gameObject = new GameObject("FrameManager");
           gameObject.AddComponent<FrameManager>();
        }
        print("开启逻辑执行");

        
        print("网络服务开启");
        if (TCPManager.Instance == null)
        {
            GameObject gameObject = new GameObject("NETTCP");
            gameObject.AddComponent<TCPManager>();
        }
        
        TCPManager.Instance.ConnectWith("119.84.246.217", 36252);
    }
}
