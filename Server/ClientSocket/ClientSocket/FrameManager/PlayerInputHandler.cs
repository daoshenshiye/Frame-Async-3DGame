
using System.Collections.Concurrent;
using ClientSocket;
using ClientSocket.ServerPlayer;
using ClientSocket.Tools;
using GameMessage;
using GamePlayer;

public class PlayerInputHandler
{
    private ConcurrentDictionary<long, ConcurrentDictionary<int, ClientInput>> frameInputBuffer = new();
    private List<ServerInputAndStateData> inputs = new();
    private float MoveSpeed = 2f;
    private float fixedDeltaTime;
   

    public PlayerInputHandler(float moveSpeed, float fixedDeltaTime)
    {
        MoveSpeed = moveSpeed;
        this.fixedDeltaTime = fixedDeltaTime;
    }

    public void CollectPlayerInputs(long executeFrame)
    {
                //收集 executeFrame 的所有玩家输入
                List<long> sortedFrameKeys = new();
                lock (frameInputBuffer)
                {
                    sortedFrameKeys = frameInputBuffer.Keys.ToList();
                }
                sortedFrameKeys.Sort();
                if(sortedFrameKeys.Count>0)
                if (sortedFrameKeys[^1]<executeFrame)
                {
                    Console.WriteLine($"警告：当前执行帧{executeFrame}的输入还没有到齐," +
                                      $" 当前缓冲区最大帧是{sortedFrameKeys[^1]}");
                }
                
                if (sortedFrameKeys.Count != 0)
                {
                    foreach (var v in frameInputBuffer[sortedFrameKeys[^1]].Keys)
                    {
                        Console.Write($"玩家{v}:"+ frameInputBuffer[sortedFrameKeys[^1]][v].predictFrame +",");
                    }
                    Console.Write("当前服务器执行帧"+executeFrame);
                    
                }
                
                var frameInputs = new Dictionary<int, ClientInput>();
                if (frameInputBuffer.ContainsKey(executeFrame))
                {
                    Console.WriteLine();
                    Console.Write("当前服务器正在执行的玩家ID为:");
                    foreach (var kvp in frameInputBuffer[executeFrame])
                    {
                        frameInputs[kvp.Key] = kvp.Value;
                        Console.Write(kvp.Key+",");
                    }
                    Console.WriteLine();
                    Console.WriteLine("============================================================");
                }

                var ClientDic = MainClass.udpserver.ClientDic.GetClients();
                // 补空输入：遍历所有在线玩家，没到齐的补空输入
                foreach (var client in ClientDic.Values)
                {
                    if (!frameInputs.ContainsKey(client.playerID) && client.playerID != -1)
                    {
                        frameInputs[client.playerID] = new ClientInput
                        {
                            playerId = client.playerID,
                            predictFrame = executeFrame,
                            input = new InputData
                            {
                                Horizontal = 0,
                                Vertical = 0,
                                Jump = false,
                            }
                        };
                    }
                }
                
                //加入该帧所有玩家的输入
                foreach (var kvp in frameInputs)
                {
                    inputs.Add(CalcPlayerState(kvp.Value));
                }
    }

    public List<ServerInputAndStateData> GetInputs()
    {

        return inputs;
    }

    public ConcurrentDictionary<long, ConcurrentDictionary<int, ClientInput>> GetBuffer()
    {
        return frameInputBuffer;
    }
    public ServerInputAndStateData CalcPlayerState(ClientInput msg)
    {
        try
        {
            ServerInputAndStateData playerStateAndInput = new ServerInputAndStateData();
            playerStateAndInput.playerId = msg.playerId;
            playerStateAndInput.inputdata = msg.input;

            PlayerStateData playerStateData;

            if (PlayerManager.Instance.player_Dic.ContainsKey(msg.playerId))
            {
                    playerStateData = PlayerManager.Instance.player_Dic[msg.playerId].playerState;
            }
            else
            {
                playerStateData = new PlayerStateData();
                playerStateData.hp = 100;
                playerStateData.playerPos = new PlayerPosData();
                playerStateAndInput.playerstate = playerStateData;
                return playerStateAndInput;
            }

            playerStateData.hp += 1;
            
            Vector3 dirPos = new Vector3(
                msg.input.Horizontal,
                msg.input.Jump ? 1 : 0,
                msg.input.Vertical
            );
            
            Vector3 beginPos = new Vector3(playerStateData.playerPos);
            
            UpdateMove(ref beginPos, dirPos,fixedDeltaTime );
            
            playerStateData.playerPos = beginPos.ToPlayerPosData();
            playerStateAndInput.playerstate = playerStateData;
            Player player = PlayerManager.Instance.GetPlayer(msg.playerId);
            if (player!=null)
            {
                player.SetPosition(beginPos);
                player.SetState(playerStateData);
            }
            return playerStateAndInput;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            ServerInputAndStateData playerStateAndInput = new ServerInputAndStateData();
            playerStateAndInput.playerstate = new PlayerStateData();
            playerStateAndInput.playerstate.playerPos = new PlayerPosData();
            return playerStateAndInput;
        }
    }
    public void UpdateMove(ref Vector3 logicPos, Vector3 dir, float FixedDeltaTime)
    {

        if (dir.x == 0 && dir.y == 0 && dir.z == 0)
            return;
        Vector3 dirNormalized = new Vector3(
            dir.x, dir.y, dir.z
        );

        if (dirNormalized.x * dirNormalized.x + dirNormalized.z * dirNormalized.z > 0.0001f)
        {
            float magnitude = MathF.Sqrt(
                dirNormalized.x * dirNormalized.x +
                dirNormalized.y * dirNormalized.y +
                dirNormalized.z * dirNormalized.z
            );
            dirNormalized.x /= magnitude;
            dirNormalized.y /= magnitude;
            dirNormalized.z /= magnitude;
        }

       
        Vector3 newPos = logicPos + dirNormalized * MoveSpeed * FixedDeltaTime;
        
        logicPos = FixFloat(newPos);
    }
    public Vector3 FixFloat(Vector3 pos)
    {
        int precision = 1000;
        
        return new Vector3((float)Math.Round(pos.x * precision)/precision, (float)Math.Round(pos.y * precision) / precision, (float)Math.Round(pos.z * precision) / precision);
    }
}