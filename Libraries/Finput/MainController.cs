using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using static GenericController1;

public enum InputType
{
    Mouse, Controller
}

public class MainController : ProtectedMonoSingleton<MainController>
{

    public static Vector2 CurrentSensitivity => _instance.Sensitivity[currentInput];

    public static InputType currentInput { get; private set; }

    [field: SerializeField] public MonoPawn ControlingPawn { get; private set; }

    [SerializedDictionary] SerializedDictionary<InputType, Vector2> Sensitivity = new();


    public GenericController1 _controller { get; private set; }
    public ControllerActions ControllerEvents => _controller.Controller;
    InputAction movement, Aim;



    public event Action<Vector2> MovementRefresh = delegate { };
    public event Action<Vector2> AimRefresh = delegate { };
    
    Dictionary<string, InputTranslator> _inputs = new();
    [SerializeField] PlayerInput _Input;

    public bool TryGetInput(string name, out InputTranslator input)
    {
        name = name.ToUpper();
        input = default(InputTranslator);
        if (_inputs.TryGetValue(name, out var x))
            input = x;

        return input != default;
    }

    public InputTranslator GetInput(string name)
    {
        name = name.ToUpper();
        if (_inputs.TryGetValue(name, out var x))
            return x;

        Debug.LogWarning($"Se pidio un input:({name}) que no existe en el diccionario, REVISAR");
        return default;
    }

    void CoroutineStarter(IEnumerator Coroutine)
    {
        StartCoroutine(Coroutine);
    }

    void CoroutineStopper(IEnumerator Coroutine)
    {
        StopCoroutine(Coroutine);
    }

    private void Awake()
    {
        _controller = new();
        foreach (var item in _controller/*.Where(x => !x.name.Contains("Axis"))*/)
        {
            _inputs.Add(item.name.ToUpper(), new InputTranslator(item, CoroutineStarter, CoroutineStopper));
        }

        InputSystem.onDeviceChange += ChangeSensitivity;
       
    }

    private void ChangeSensitivity(InputDevice newdevice, InputDeviceChange arg2)
    {
        
    }


    private void OnEnable()
    {
        _controller.Enable();


        movement = _controller.Controller.Axis_1;
        movement.Enable();

        Aim = _controller.Controller.Axis_2;
        Aim.Enable();
    }

    private void OnDisable()
    {
        _controller.Disable();

        movement = _controller.Controller.Axis_1;
        movement.Disable();

        Aim = _controller.Controller.Axis_2;
        Aim.Disable();
    }



    private void OnValidate()
    {
        //if (PawnGO == ControlingPawn as MonoBehaviour) return; 
        if (ControlingPawn == null)
        {
            ControlingPawn = null; /*CurrentActions.Clear();*/ return;
        }
    }


    private void Start()
    {
        if (ControlingPawn == null || ControlingPawn == default) return;

        SetNewPawn(ControlingPawn);

    }

    private void Update()
    {
        MovementRefresh.Invoke(movement.ReadValue<Vector2>());
        Vector2 X = Aim.ReadValue<Vector2>();
        AimRefresh.Invoke(X);
        //Debug.Log("Aim Value " + X);

    }


    public void SetNewPawn(MonoPawn pawn)
    {
        if (ControlingPawn != null)
        {
            ControlingPawn.UnPosses(this);
            //_controller.CustomForEach(x => x.RemoveAction());
        }
        ControlingPawn = pawn;

        ControlingPawn.Posses(this);


    }



    #region Editor
#if UNITY_EDITOR
    [ContextMenu("Set As CurrentPawn")]

    void SelectCurrentPawn()
    {
        var x = Selection.gameObjects
            .Select(x => x.GetComponent<MonoPawn>())
            .Where(x => x != null)
            .First();

        if (x == null) return;

        SetNewPawn(x);
    }

#endif
    #endregion
}
public class InputTranslator
{
    //esto tendria que ser una fsm no?
    public string Name { get; private set; }
    InputInfo CurrentInfo;
    public event Action<InputInfo> OnStartPressing, WhileHolding, OnRelease, OnWait = delegate { };
    Action<IEnumerator> CoroutineStarter, CoroutineStopper;

    public InputInfo GetInfo() => CurrentInfo;
    public InputTranslator(InputAction input, Action<IEnumerator> CoroutineStarter, Action<IEnumerator> CoroutineStopper)
    {
        this.CoroutineStarter = CoroutineStarter;
        this.CoroutineStopper = CoroutineStopper;
        input.started += StartPressing;
        input.canceled += ReleasePress;
        //OnStartPressing += _ => Debug.Log("Invoco Pressed de " + _.Name);
        //OnRelease += _ => Debug.Log("Invoco Release de " + _.Name);
        //OnWait += _ => Debug.Log("Invoco Wait de " + _.Name);
        CurrentInfo = new(); CurrentInfo.Name = input.name;


    }


    void StartPressing(InputAction.CallbackContext unityContext)
    {
        if (unityContext.phase != InputActionPhase.Started) return;

        if (CurrentInfo.CurrentPhase != CustomPhase.Waiting) return;


        CurrentInfo.StartTime = Time.time;
        CurrentInfo.CurrentPhase = CustomPhase.Pressed;
        CurrentInfo.callback = unityContext;

        OnStartPressing?.Invoke(CurrentInfo);
        CoroutineStarter.Invoke(WhileHolded());
    }

    IEnumerator WhileHolded()
    {
        CurrentInfo.CurrentPhase = CustomPhase.Holded;
        while (CurrentInfo.CurrentPhase == CustomPhase.Holded)
        {
            CurrentInfo.CurrentDuration += Time.deltaTime;

            WhileHolding?.Invoke(CurrentInfo);
            yield return null;
        }
        bool aña = true;
        aña |= inp
    }

    void ReleasePress(InputAction.CallbackContext unityContext)
    {
        CurrentInfo.CurrentPhase = CustomPhase.Released;
        CurrentInfo.StopTime = Time.time;
        CurrentInfo.callback = unityContext;
        CoroutineStopper.Invoke(WhileHolded());

        OnRelease?.Invoke(CurrentInfo);

        CurrentInfo.CurrentDuration = 0f;
        CurrentInfo.CurrentPhase = CustomPhase.Waiting;
        OnWait?.Invoke(CurrentInfo);
    }


    public enum CustomPhase
    {
        Waiting, Pressed, Holded, Released
    }
    public struct InputInfo
    {
        public string Name;

        public float StartTime;
        public float StopTime;
        public float FinalDuration => StopTime - StartTime;
        public float CurrentDuration;

        public InputAction.CallbackContext callback;
        //TO DO: poner forma de que vuelva a default

        public CustomPhase CurrentPhase;

    }
}

