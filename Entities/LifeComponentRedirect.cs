using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Collider))]
public class LifeComponentRedirect : MonoBehaviour ,IHealth
{
    [field: SerializeField] public LifeComponent Owner { get; private set; }

    public Transform Transform => Owner.transform;

    public void AddDamageOverTime(int TotalDamageToDeal, float TimeAmount)
    {
        Owner.AddDamageOverTime(TotalDamageToDeal, TimeAmount);
    }

    public void AddHealOverTime(int totalHeal, float timeAmount)
    {
        Owner.AddHealOverTime(totalHeal, timeAmount);
    }

    public int Heal(int HealAmount)
    {
      return Owner.Heal(HealAmount);
    }

    public ReceiveDamageData TakeDamage(DealDamageData DealerData)
    {
        return Owner.TakeDamage(DealerData);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, Owner.transform.position);
    }

}
