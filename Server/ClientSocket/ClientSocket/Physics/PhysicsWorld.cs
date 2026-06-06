using ClientSocket.Physics.Colliders;

namespace ClientSocket.Physics;

public class PhysicsWorld
{
    private static PhysicsWorld instance;

    public static PhysicsWorld Instance
    {
        get
        {
            if (instance == null)
                instance=new PhysicsWorld();
            return instance;
        }
    }

    private List<ICollider> colliders = new List<ICollider>();
        private List<string> Tags = new List<string>();
        private string[] layers = new string[32];

        public int GetLayers(params string[] s)
        {
            int mask = 0;
            foreach (string name in s)
            {
                int index = System.Array.IndexOf(layers, name);
                if (index < 0) throw new Exception($"无效层级：{name}");
                mask |= 1 << index;
            }
            return mask;
        }
        
        public void AddCollider(ICollider collider)
        {
            colliders.Add(collider);
        }
    
        public void RemoveCollider(ICollider collider)
        {
            colliders.Remove(collider);
        }
    
        public void Tick()
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                for (int j = i + 1; j < colliders.Count; j++)
                {
                    BaseCollider collider = colliders[i] as BaseCollider;
                    BaseCollider otherCollider = colliders[j] as BaseCollider;
                    if (colliders[i].IsColliding(colliders[j]))
                    {
                        if (!collider.ColiddingColliders.ContainsKey(otherCollider.Guid))
                        {
                            collider.Owner.OnCollisionEnter(otherCollider);
                            otherCollider.Owner.OnCollisionEnter(collider);
                            collider.ColiddingColliders.Add(otherCollider.Guid, otherCollider);
                            otherCollider.ColiddingColliders.Add(collider.Guid, collider);
                        }
                        else
                        {
                            collider.Owner.OnCollisionStay(otherCollider);
                            otherCollider.Owner.OnCollisionStay(collider);
                        }
                    }
                    else
                    {
                        if (collider.ColiddingColliders.ContainsKey(otherCollider.Guid))
                        {
                            collider.Owner.OnCollisionExit(otherCollider);
                            otherCollider.Owner.OnCollisionExit(collider);
                            collider.ColiddingColliders.Remove(otherCollider.Guid);
                            otherCollider.ColiddingColliders.Remove(collider.Guid);
                        }
                    }
                }
            }
        }
}