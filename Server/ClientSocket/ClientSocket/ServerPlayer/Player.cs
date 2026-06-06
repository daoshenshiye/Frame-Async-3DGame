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
        playerState.playerPos.x = Position.x;
        playerState.playerPos.y= Position.y;
        playerState.playerPos.z = Position.z;
    }

    public Player(int id,Vector3 playerPos)
    {
        this.id = id;
        Start();
        SetPosition(playerPos);
        playerState = new PlayerStateData();
        playerState.playerPos = new PlayerPosData();
        playerState.playerPos.x = Position.x;
        playerState.playerPos.y= Position.y;
        playerState.playerPos.z = Position.z;
        PlayerManager.Instance.AddPlayer(this);
        
    }

    public void DestroyPlayer()
    {
        PlayerManager.Instance.DeletePlayer(id);
    }
    protected override void Update()
    {
        base.Update();
    }

    protected override void Start()
    {
        base.Start();
    }

    public void SetPosition(Vector3 pos)
    {
        if (pos!=null)
        this.Position = pos;
    }

    public void SetState(PlayerStateData state)
    {
        if (state!=null)
        playerState = state;
    }
    public override void OnCollisionStay(BaseCollider collider)
    {
        base.OnCollisionStay(collider);
        Console.WriteLine("Player"+id+"正在发生碰撞");
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