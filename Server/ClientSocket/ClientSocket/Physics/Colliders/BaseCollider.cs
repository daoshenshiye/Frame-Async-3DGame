using ClientSocket.Component;
using ClientSocket.Tools;

namespace ClientSocket.Physics.Colliders;

public abstract class BaseCollider:BaseComponent,ICollider
{
    private Vector3 center;
    private Vector3 size;
    public Dictionary<string,BaseCollider> ColiddingColliders=new Dictionary<string, BaseCollider>();
    public Vector3 Size
    {
        get { return size; }
        set { size = value; }
    }

    public Vector3 Center
    {
        get { return center; }
        set { center = value; }
    }
   public BaseCollider(Vector3 center,Vector3 size)
    {
        this.center = center;
        this.size = size;
    }
    
   public BaseCollider()
    {
        this.center = new Vector3();
        this.size = new Vector3(1,1,1);
    }
    
    public abstract bool IsColliding(ICollider other);
    public override void Update()
    {
    }
}