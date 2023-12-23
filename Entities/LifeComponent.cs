using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public interface IHealth : IDamagable, IHealable { }
[RequireComponent(typeof(DebugableObject))]
//[RequireComponent(typeof(PausableObject))]
public class LifeComponent : MonoBehaviour, IHealth
{
    DebugableObject _debug;

    [field: SerializeField]
    public DamageType AffectedBy { get; private set; }

    #region Life
    [field: SerializeField]
    public int CurrentLife { get; private set; }
    [field: SerializeField]
    public int MaxLife { get; private set; }

    [field: SerializeField] public int MaxDamagePerHit { get; private set; }

    [SerializeField] bool ClampDamageTaken = false;

    #region Useful Getters
    public bool IsDead => CurrentLife <= 0;
    public float LifePercentage => NormalizedLifePercentage * 100;
    public float NormalizedLifePercentage => (float)CurrentLife / (float)MaxLife;
    public bool IsInvulnerable { get; private set; }
    #endregion




    [SerializeField, Range(0.1f, 2)]
    float _dmgMultiplier = 1f;

    public int dmgResist = 1;



    [field: SerializeField] public bool CanTakeDamage { get; private set; } = true;
    [SerializeField] public bool CanBeHealed { get; private set; } = true;

    public Transform Transform => transform;

    public bool isCrit;

    #region Events   

    public UnityEvent<HealthData> OnHealthChange = new();
    public UnityEvent<HealthData> OnHeal = new();


    public UnityEvent<DealDamageData> OnTakeDamage, OnInvulnerableHit = new();
    public UnityEvent OnDeath = new UnityEvent();
    public UnityEvent OnBecomeInVulnerable = new();
    #endregion

    //IEnumerator OnPause()
    //{
    //    var x = canTakeDamage;
    //    var z = canBeHealed;
    //    canTakeDamage = canBeHealed = false;
    //    yield return new WaitUntil(ScreenManager.IsPaused);
    //    canBeHealed = z; canTakeDamage = x;
    //}

    private void Awake()
    {

        _debug = GetComponent<DebugableObject>();

        //esto es para encontrar hijos de mi gameobject que sean "hitable"
        //foreach (var item in GetComponentsInChildren<HitableObject>()) item.SetOwner(this);

        #region SetEvents
        OnHeal.AddListener(_ => OnHealthChange?.Invoke(_));
        OnTakeDamage.AddListener(_ => OnHealthChange?.Invoke(this.ToHealthData()));


        #endregion
        enabled = false;

    }

    private void Start()
    {
        OnHealthChange?.Invoke(this.ToHealthData());
    }

    public void SetInvulnerability(float time)
    {
        StopAllCoroutines();
        StartCoroutine(Invulnerability(time));
    }

    public void SetVulnerableUntil(Func<bool> condition)
    {
        StopAllCoroutines();
        StartCoroutine(VulnerableUntil(condition));
    }

    IEnumerator Invulnerability(float t)
    {
        CanTakeDamage = false;
        _debug.Log("No toma daño por" + t);
        yield return new WaitForSecondsRealtime(t);
        _debug.Log("vuelve a tomar daño");
        CanTakeDamage = true;
    }

    IEnumerator VulnerableUntil(Func<bool> condition)
    {
        CanTakeDamage = true;
        yield return new WaitUntil(condition);
        CanTakeDamage = false;
        OnBecomeInVulnerable.Invoke();

    }

    public void SetNewMaxLife(int value) => MaxLife = Mathf.Clamp(value, 1, int.MaxValue);

    public void Initialize()
    {
        CurrentLife = MaxLife;
    }


    #region DamageSide


    public virtual ReceiveDamageData TakeDamage(DealDamageData dmgData)
    {
        ReceiveDamageData data = new ReceiveDamageData();
        if (!CanGetDamage(dmgData))
        {
            Debug.Log("Invulnerable!");
            OnInvulnerableHit.Invoke(dmgData);
            return data;
        }

        int dmgValue = (int)(Mathf.Abs(dmgData.DamageAmount) * _dmgMultiplier) / dmgResist;
        if (ClampDamageTaken)
        {
            dmgValue = Mathf.Clamp(dmgValue, 0, MaxDamagePerHit);
        }



        CurrentLife -= dmgValue;
        dmgData.HealthData = this.ToHealthData();
        OnTakeDamage?.Invoke(dmgData);
        _debug.Log($" recibio {dmgValue} de daño ");

        data = WasKilled(data);

        data.damageDealt = dmgValue;
        return data;
    }


    ReceiveDamageData WasKilled(ReceiveDamageData data)
    {
        if (IsDead)
        {
            OnDeath?.Invoke();
            data.wasKilled = true;
        }
        return data;
    }

    public virtual void AddDamageOverTime(int TotalDamageToDeal, float TimeAmount)
    {
        int damagePerTick = Mathf.Max(1, (int)((TotalDamageToDeal / TimeAmount)));

        //Action action = () =>
        //{
        //    int dmgToDeal = life - damagePerTick > 0 ? damagePerTick : 0;
        //    TakeDamage(dmgToDeal);
        //};


    }
    #endregion

    #region HealingSide
    /// <summary>
    /// añade vida, no supera la vida maxima
    /// </summary>
    /// <param name="HealAmount"></param>
    /// <returns></returns>
    public virtual int Heal(int HealAmount)
    {
        if (!CanBeHealed) return 0;

        _debug.Log($" se curo {HealAmount} de vida ");
        CurrentLife += Mathf.Abs(HealAmount);

        OnHeal?.Invoke(this.ToHealthData());
        if (CurrentLife > MaxLife) CurrentLife = MaxLife;

        return HealAmount;
    }
    /// <summary>
    /// Añade x cantidad de vida al objetivo a lo largo de y segundos(no supera la vida maxima)
    /// </summary>
    /// <param name="totalHeal"></param>
    /// <param name="timeAmount"></param>
    public void AddHealOverTime(int totalHeal, float timeAmount)
    {
        int HealPerTick = (int)(totalHeal / timeAmount);
        //AddHealthEvent(() => Heal(HealPerTick), timeAmount);
    }
    #endregion

    private void OnValidate()
    {
        MaxLife = Mathf.Max(0, MaxLife);
        CurrentLife = MaxLife;
    }

    //TO DO:
    //Refactor Later
    bool CanGetDamage(DealDamageData dmgData)
    {

        Debug.Log(CanTakeDamage);
        return CanTakeDamage && AffectedBy.HasFlag(dmgData.DamageTypes);

    }


    #region Structs





    #endregion
    #endregion
}

public interface IDamagable : ITransform
{
    ReceiveDamageData TakeDamage(DealDamageData DealerData);


    void AddDamageOverTime(int TotalDamageToDeal, float TimeAmount);
}

[Flags]
public enum DamageType
{
    Base = 1, Fire = 2
}
public struct ReceiveDamageData
{
    public int damageDealt;
    public bool wasKilled;
    public bool wasCrit;
    public IDamagable victim;
}


public interface IHealable
{
    int Heal(int HealAmount);
    void AddHealOverTime(int totalHeal, float timeAmount);
}
