using GameMessage;
using GamePlayer;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class FixedFrameManager : MonoBehaviour
{
    private static FixedFrameManager instance;
    public static FixedFrameManager Instance => instance;

//    private float speed = 2f;
//    //public InputMessage PlayerInput;
   
//    private int index = 0;
//    private bool shouldRecode = false;
//    private int RecodeIndex = 0;
    
//    // Start is called before the first frame update
//    void Awake()
//    {
//        instance= this;
//       // PlayerInput=new InputMessage();
//        //PlayerInput.input = new InputData();
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        //HandleMove();
//        CollectData();
//        //if (Input.GetKeyDown(KeyCode.KeypadEnter))
//        //{
//        //    InputRecorder.Instance.ReplayInput();
//        //    shouldRecode = true;
//        //    RecodeIndex = 0;
//        //}
//        //if (Input.GetKeyDown(KeyCode.LeftShift))
//        //{
//        //    shouldRecode = false;
//        //    RecodeIndex = 0;
//        //}

            
           
            
//            //if (InputRecorder.Instance.inputs.Count>0&&shouldRecode&&RecodeIndex< InputRecorder.Instance.inputs.Count)
//            //{
//            //    FixedFrameInput input = InputRecorder.Instance.inputs[RecodeIndex];
//            //    if (input != null) {
//            //    PlayerInput.Horizontal=input.Horizontal;
//            //        PlayerInput.Vertical=input.Vertical;
//            //        PlayerInput.Jump=input.Jump;
//            //    }
//            //    RecodeIndex++;
//            //    if(RecodeIndex>= InputRecorder.Instance.inputs.Count)
//            //    {
//            //        RecodeIndex=0;
//            //        InputRecorder.Instance.inputs.Clear();
//            //    }
//            //}
          
//           //DoLogic(PlayerInput);
            
            
//    }
        
//    public void CollectData()
//    {
//        PlayerInput.input.Horizontal= Input.GetAxis("Horizontal");
//        PlayerInput.input.Vertical = Input.GetAxis("Vertical");
//        PlayerInput.input.Jump = Input.GetKeyDown(KeyCode.Space);
//        if (PlayerInput.input.Horizontal!=0||PlayerInput.input.Vertical!=0||PlayerInput.input.Jump==true)
//        {
//            TCPManager.Instance.Send(PlayerInput);
//            print("ID为"+PlayerInput.GetID());
//        }
//        PlayerInput.input.Horizontal = 0;
//        PlayerInput.input.Vertical=0;
//        PlayerInput.input.Jump= false;
//    }

//    public void DoLogic(FixedFrameInput input)
//    {
//        Vector3 moveDir = new Vector3(input.Horizontal, 0, input.Vertical).normalized;

//        this.transform.position += moveDir * speed * 0.03f;
//        input.Reset();
//        print("执行了逻辑");
//    }
//    //private void HandleMove()
//    //{
//    //    PlayerInput.Horizontal = Input.GetAxis("Horizontal");
//    //    PlayerInput.Vertical = Input.GetAxis("Vertical");
//    //    PlayerInput.Jump = Input.GetKeyDown(KeyCode.Space);

//    //        FixedFrameInput inputd = new FixedFrameInput();
//    //     if(PlayerInput.Vertical==0&&PlayerInput.Horizontal==0)
//    //    {
//    //        return;
//    //    }

//    //        inputd.Vertical = PlayerInput.Vertical;
//    //        inputd.Jump = PlayerInput.Jump;
//    //        inputd.Horizontal= PlayerInput.Horizontal;
//    //        InputRecorder.Instance.RecordInput(inputd);
//    //}
}
