using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using static GenericController1;

public class MainController : ProtectedMonoSingleton<MainController>
{
    // public static Vector2 CurrentSensitivity => _instance.Sensitivity[currentInput];
    // [SerializedDictionary] SerializedDictionary<InputType, Vector2> Sensitivity = new();

    #region  AxisGetters

    public Vector2 LeftAxisInput => movement.ReadValue<Vector2>();
    public Vector2 RightAxisInput => Aim.ReadValue<Vector2>();
    public Vector2 ArrowsInput => Arrows.ReadValue<Vector2>();

 
    #endregion

    [field: SerializeField] public MonoPawn ControlingPawn { get; private set; }

    
    public GenericController1 _controller { get; private set; }
    public ControllerActions ControllerEvents => _controller.Controller;
    InputAction movement, Aim, Arrows;

    #region  EventRefreshers

    public event Action<Vector2> MovementRefresh = delegate { };
    public event Action<Vector2> AimRefresh = delegate { };
    public event Action<Vector2> ArrowsRefresh = delegate { };

    #endregion
   

    Dictionary<string, InputTranslator> _inputs = new();
    [SerializeField] PlayerInput _Input;

    public static bool RequestController(MonoPawn target)
    {
        if (!_instance) return false;
        Debug.Log("Request Controller");
        _instance.SetNewPawn(target);
        return true;
    }

    #region InputGetter

    public bool InputExists(string name) => _inputs.ContainsKey(name.ToUpper());

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

    public IEnumerable<InputTranslator> GetInputs(IEnumerable<string> names)
    {
        foreach (var item in names.Select(x => x.ToUpper()).Where(InputExists))
            yield return GetInput(item);
        //Debug.LogWarning($"Se pidio un input:({names}) que no existe en el diccionario, REVISAR");
        //yield return default;
    }

    #endregion

    #region CoroutineStarter

    void CoroutineStarter(IEnumerator Coroutine)
    {
        StartCoroutine(Coroutine);
    }

    void CoroutineStopper(IEnumerator Coroutine)
    {
        StopCoroutine(Coroutine);
    }

    #endregion


    #region UnityCalls

    protected override void Awake()
    {
        base.Awake();

        _controller = new();


        foreach (var item in _controller /*.Where(x => !x.name.Contains("Axis"))*/)
        {
            _inputs.Add(item.name.ToUpper(), new InputTranslator(item, CoroutineStarter, CoroutineStopper));
        }
    }

    private void OnEnable()
    {
        _controller.Enable();


        movement = _controller.Controller.Axis_1;
        movement.Enable();

        Aim = _controller.Controller.Axis_2;
        Aim.Enable();

        Arrows = _controller.Controller.DPadMovement;
        Arrows.Enable();
    }

    private void OnDisable()
    {
        _controller.Disable();

        movement = _controller.Controller.Axis_1;
        movement.Disable();

        Aim = _controller.Controller.Axis_2;
        Aim.Disable();

        Arrows = _controller.Controller.DPadMovement;
        Arrows.Disable();
    }


    private void OnValidate()
    {
        //if (PawnGO == ControlingPawn as MonoBehaviour) return; 
        if (ControlingPawn == null)
        {
            ControlingPawn = null; /*CurrentActions.Clear();*/
            return;
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
        AimRefresh.Invoke(Aim.ReadValue<Vector2>());
        ArrowsRefresh.Invoke(Arrows.ReadValue<Vector2>());
        //Debug.Log("Aim Value " + X);
    }

    #endregion

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

    public InputTranslator(InputAction input, Action<IEnumerator> CoroutineStarter,
        Action<IEnumerator> CoroutineStopper)
    {
        this.CoroutineStarter = CoroutineStarter;
        this.CoroutineStopper = CoroutineStopper;
        input.started += StartPressing;
        input.canceled += ReleasePress;
        //OnStartPressing += _ => Debug.Log("Invoco Pressed de " + _.Name);
        //OnRelease += _ => Debug.Log("Invoco Release de " + _.Name);
        //OnWait += _ => Debug.Log("Invoco Wait de " + _.Name);
        CurrentInfo = new();
        CurrentInfo.Name = input.name;
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
        Waiting,
        Pressed,
        Holded,
        Released
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