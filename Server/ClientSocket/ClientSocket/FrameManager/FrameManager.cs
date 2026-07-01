using ClientSocket;
using ClientSocket.Tools;
using ClientSocket.UDP;
using GameMessage;
using GamePlayer;
using GameSystem;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.InteropServices;
using ClientSocket.Physics;
using ClientSocket.Physics.Colliders;
using ClientSocket.ServerPlayer;
using GameMessage;
using Vector3 = ClientSocket.Tools.Vector3;

public class FrameManager
{
    public int FixedFrameRate =15;
    public float FixedDeltaTime= 0;
    public  long CurrentLogicFrame;
    public int DelayBufferFrames = 4;
    public  PlayerInputHandler PlayerInputHandler;
    private bool shouldOpenLogic;
    private DateTime lastTime=DateTime.Now;
    private float MoveSpeed = 2f;
    private int timespan = 1000 / 15;
    private Thread frameThread;
    public FrameManager(int fixedFrameRate)
    {
        CurrentLogicFrame=DelayBufferFrames + 1;
        FixedFrameRate = fixedFrameRate;
        FixedDeltaTime = 1f / FixedFrameRate;
        shouldOpenLogic=true;
        frameThread = new Thread(Update);
        frameThread.Start();
        PlayerInputHandler = new PlayerInputHandler(MoveSpeed, FixedDeltaTime);
    }
    private void Update()
    {
        while (shouldOpenLogic)
        {
                // 计算当前距离上一帧过了多久
                var elapsedMs = (DateTime.Now - lastTime).TotalMilliseconds;
                
                if (elapsedMs < timespan)
                {
                    Thread.Sleep(1);
                    continue;
                }
                
                try{
                // 延迟缓冲：当前推进到帧N，但实际执行帧N-2
                long executeFrame = ReadLogicFrame() - DelayBufferFrames;
                
                ServerFrameAuthenMsg serverLogicFrame = new ServerFrameAuthenMsg();
                serverLogicFrame.serLogicFrame = executeFrame;
                
                PlayerInputHandler.CollectPlayerInputs(executeFrame);
                var inputs = PlayerInputHandler.GetInputs();
                // PhysicsWorld.Instance.Tick();
                serverLogicFrame.ServerInputStateData = inputs.Count > 0 ? inputs : new List<ServerInputAndStateData>();
                MainClass.udpserver.BroadCastMsg(serverLogicFrame, E_UDP_MSG_TYPE.ORDER_STEADY);
                
                List<long> removeList=new List<long>();
                foreach (var frame in PlayerInputHandler.GetBuffer())
                {
                    if (frame.Key <= executeFrame)
                    {
                        removeList.Add(frame.Key);
                    }
                }
                
                foreach (var v in removeList)
                {
                    PlayerInputHandler.GetBuffer().TryRemove(v, out _);
                }
                inputs.Clear();
                Interlocked.Increment(ref CurrentLogicFrame);
                lastTime = DateTime.Now;
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine(e);
                    lastTime=DateTime.Now;
                }
        }
    }
    
    public long ReadLogicFrame()
    {
        return Interlocked.Read(ref CurrentLogicFrame);
    }

}
