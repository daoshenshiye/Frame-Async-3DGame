namespace ClientSocket.Physics.Colliders;

public class BoxCollider:BaseCollider
{
    
    public override bool IsColliding(ICollider other)
    {
       BoxCollider otherCollider= (BoxCollider)other;
       bool noCollision = this.Owner.Position.x + this.Size.x < otherCollider.Owner.Position.x ||
                          this.Owner.Position.x  > otherCollider.Owner.Position.x + otherCollider.Size.x ||
                          this.Owner.Position.y + this.Size.y < otherCollider.Owner.Position.y ||
                          this.Owner.Position.y  > otherCollider.Owner.Position.y + otherCollider.Size.y 
                          || this.Owner.Position.z + this.Size.z < otherCollider.Owner.Position.z ||
                          this.Owner.Position.z  > otherCollider.Owner.Position.z + otherCollider.Size.z;
       return !noCollision;
    }
}
