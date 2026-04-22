using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InputRecorder
{
    private static InputRecorder instance=new InputRecorder();
    public static InputRecorder Instance => instance;
    // 存储「帧号-输入」的映射（保证顺序）
    public  List<FixedFrameInput> inputs = new List<FixedFrameInput>();
    
    
    // 引用固定帧管理器
    public FixedFrameManager frameManager;
    public InputRecorder()
    {
        frameManager = FixedFrameManager.Instance;
        // 注册逻辑帧执行的回调（每次执行逻辑帧时记录输入）
       
    }

 

    // 记录当前帧的输入
    public void RecordInput(FixedFrameInput input)
    {
        inputs.Add(input);
       
    }

    // 回放输入（重置游戏状态后执行）
    public void ReplayInput()
    {
        // 重置游戏状态（角色位置、帧号等）
        ResetGameState();

    }
    public void DoRePlay(List<FixedFrameInput> inputs)
    {
       
    }
    // 重置游戏状态（关键：保证回放的初始状态和录制时一致）
    private void ResetGameState()
    {
        FixedFrameManager.Instance.transform.position= new Vector3(0, 0, 0);
        
        
    }

}


    
