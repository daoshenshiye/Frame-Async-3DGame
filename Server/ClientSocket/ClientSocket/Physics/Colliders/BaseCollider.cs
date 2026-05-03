using ClientSocket.Component;
using ClientSocket.Tools;

namespace ClientSocket.Physics.Colliders;

public abstract class BaseCollider:BaseComponent,ICollider
{
    public Position center;
    public Position size;
    public bool Collding;
    public Position Size
    {
        get { return size; }
        set { size = value; }
    }
    public Position Pos
    {
        get { return center; }
    }
   public BaseCollider(Position pos,Position size)
    {
        this.center = pos;
        this.size = size;
    }
    
   public BaseCollider()
    {
        this.center = new Position();
        this.size = new Position();
    }
    
    public abstract bool IsColliding(ICollider other);
    public override void Update()
    {
        center =Owner.position;
    }
}