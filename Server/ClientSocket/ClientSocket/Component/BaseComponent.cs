using ClientSocket.MonoBehaviour;

namespace ClientSocket.Component;

public class BaseComponent
{
    public Mono Owner;
    
    public virtual void Start() { }
    public  virtual void Update()
    {
    }
}