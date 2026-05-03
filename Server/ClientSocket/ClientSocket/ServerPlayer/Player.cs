using ClientSocket.Component;
using ClientSocket.MonoBehaviour;
using ClientSocket.Physics.Colliders;
using ClientSocket.Tools;
using GamePlayer;

namespace ClientSocket.ServerPlayer;

public class Player:Mono
{
    
    public PlayerStateData  playerState;
    public string layer;
    public string tag;
    public int id;
    public Player(string layer, string tag,int id)
    {
        this.layer = layer;
        this.tag = tag;
        this.id = id;
        Start();
        playerState = new PlayerStateData();
        playerState.playerPos = new PlayerPosData();
        playerState.playerPos.x = position.x;
        playerState.playerPos.y= position.y;
        playerState.playerPos.z = position.z;
    }

    public Player(int id)
    {
        this.id = id;
        Start();
        playerState = new PlayerStateData();
        playerState.playerPos = new PlayerPosData();
        playerState.playerPos.x = position.x;
        playerState.playerPos.y= position.y;
        playerState.playerPos.z = position.z;
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void Start()
    {
        base.Start();
    }

    public override void OnCollisionEnter(BaseCollider collider)
    {
        Console.WriteLine("Player"+id+"发生了碰撞");
    }

    public override void OnCollisionExit(BaseCollider collider)
    {
        Console.WriteLine("Player"+id+"刚离开碰撞碰撞");
    }
}