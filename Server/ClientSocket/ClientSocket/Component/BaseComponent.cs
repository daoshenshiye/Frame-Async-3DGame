using ClientSocket.MonoBehaviour;

namespace ClientSocket.Component;

public class BaseComponent
{
    public Mono Owner;
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();
    public static bool operator ==(BaseComponent a, BaseComponent b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Guid == b.Guid;
    }
    public static bool operator !=(BaseComponent a, BaseComponent b)
    {
        return !(a == b);
    }
    public virtual void Start()
    {
        
    }
    public  virtual void Update()
    {
    }
}