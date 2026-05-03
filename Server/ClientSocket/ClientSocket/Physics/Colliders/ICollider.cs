namespace ClientSocket.Physics.Colliders;

public interface ICollider
{
    public bool IsColliding(ICollider other);
}