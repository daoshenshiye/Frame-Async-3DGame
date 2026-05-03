using ClientSocket.Component;
using ClientSocket.Physics;
using ClientSocket.Physics.Colliders;
using ClientSocket.Tools;

namespace ClientSocket.MonoBehaviour;

public class Mono
{
    
    private Dictionary<Type,List<BaseComponent>> components=new Dictionary<Type, List<BaseComponent>>();
    public Position position;
    protected virtual void Start()
    {
        AddComponent<Position>();
        position = GetComponent<Position>() as  Position;
        foreach (var component in components.Values)
        {
            foreach (var v in component)
            {
                v.Start();
            }
        }
    }

    protected virtual void Update()
    {
        foreach (var component in components.Values)
        {
            foreach (var v in component)
            {
                v.Update();
            }
        }
    }

    public virtual  void OnCollisionEnter(BaseCollider collider)
    {
        
    }
    public virtual void OnCollisionExit(BaseCollider collider)
    {
        
    }
    public  BaseComponent GetComponent<T>()
    {
        
       if (components.GetValueOrDefault(typeof(T))!=null)
       {
           return components.GetValueOrDefault(typeof(T))[0];
       }
       return null;
    }
    public  void AddComponent<T>()where T : BaseComponent,new()
    {
        Type type = typeof(T);
        T component = new T();
        component.Owner = this;
        if (typeof(BaseCollider).IsAssignableFrom(type))
        {
            PhysicsWorld.Instance.AddCollider(component as BaseCollider);
        }
        if (!components.ContainsKey(type))
        {
            components[type] = new List<BaseComponent>();
        }
        components[type].Add(component);
    }

    public  void RemoveComponent<T>(BaseComponent component)
    {
        Type type = typeof(T);
        if (typeof(BaseCollider).IsAssignableFrom(type))
        {
            PhysicsWorld.Instance.RemoveCollider(component as BaseCollider);
        }
        if (components.ContainsKey(typeof(T)))
        {
            components.GetValueOrDefault(typeof(T)).Remove(component);
        }

        if (components.GetValueOrDefault(typeof(T)).Count == 0)
        {
            components.Remove(typeof(T));
        }
    }
}