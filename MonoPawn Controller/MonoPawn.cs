using UnityEngine;
using UnityEngine.Events;


public abstract class MonoPawn : MonoBehaviour,Iinputable
{
    [Header("Pawn")]
    public UnityEvent<MainController> OnPosses = new();
    public UnityEvent<MainController> OnUnPosses = new();

    [field: SerializeField] public MainController MyController { get; private set; }

    public abstract void SetInputs(MainController newController);
    public abstract void RemoveInputs(MainController oldController);

    public void Posses(MainController newController)
    {
       
        if (newController.ControlingPawn != this) return;
        
        MyController = newController;
        OnPosses.Invoke(newController);
        SetInputs(newController);
    }
    
    public void UnPosses(MainController oldController)
    {
        Debug.Log("Unposses " + gameObject.name);
        RemoveInputs(oldController);
        OnUnPosses.Invoke(oldController);
        if (oldController == MyController)
        {
            MyController = null;

        }
    }

    protected bool RequestPlayerController() => MainController.RequestController(this);
}
public interface Iinputable
{
    public void SetInputs(MainController newController);
    public void RemoveInputs(MainController oldController);
}