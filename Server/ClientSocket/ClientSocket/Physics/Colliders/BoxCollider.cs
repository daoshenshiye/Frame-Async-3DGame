namespace ClientSocket.Physics.Colliders;

public class BoxCollider:BaseCollider
{
    
    public override bool IsColliding(ICollider other)
    {
       BoxCollider otherCollider= (BoxCollider)other;
       bool noCollision = this.Pos.x + this.Size.x < otherCollider.Pos.x ||
                          this.Pos.x  > otherCollider.Pos.x + otherCollider.Size.x ||
                          this.Pos.y + this.Size.y < otherCollider.Pos.y ||
                          this.Pos.y  > otherCollider.Pos.y + otherCollider.Size.y 
                          || this.Pos.z + this.Size.z < otherCollider.Pos.z ||
                          this.Pos.z  > otherCollider.Pos.z + otherCollider.Size.z;
       
       return !noCollision;
    }
}
