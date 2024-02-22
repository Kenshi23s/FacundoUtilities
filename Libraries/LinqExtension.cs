using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public static class LinqExtension
{
    public static T Minimum<T>(this IEnumerable<T> col, Func<T, float> GetValue)
    {
        float minimum = float.MaxValue;
        T returnItem = default;
        foreach (var item in col)
        {
            float newValue = GetValue(item);
            if (minimum > newValue)
            {
                minimum = newValue;
                returnItem = item;
            }
        }
        return returnItem;
    }

    public static T Maximum<T>(this IEnumerable<T> col, Func<T, float> GetValue)
    {
        float maximum = float.MinValue;
        T returnItem = default;
        foreach (var item in col)
        {
            float newValue = GetValue(item);
            if (newValue > maximum)
            {
                maximum = newValue;
                returnItem = item;
            }
        }
        return returnItem;
    }

    public static IEnumerable<T> NotOfType<T, K>(this IEnumerable<T> col)
    {
        foreach (var item in col)
        {
            if (!(item.GetType() == typeof(K)))
                yield return item;
        }
    }

    public static IEnumerable<K> CustomGetComponents<T, K>(this IEnumerable<T> col) where T : MonoBehaviour
    {
        foreach (var item in col.Select(x => x.GetComponent<K>()).Where(x => x != null))
        {
            yield return item;
        }
    }

    public static IEnumerable<K> CustomGetComponentsGO<K>(this IEnumerable<GameObject> col)
    {
        foreach (var item in col.Select(x => x.GetComponent<K>()))
        {
            if (item == null || item.Equals((K)default)) continue;

            yield return item;
        }
    }

    public static Dictionary<K, IEnumerable<V>> ToDictionary<K, V>(this IEnumerable<V> col, Func<V, K> predicate)
    {

        return col.Aggregate(new Dictionary<K, IEnumerable<V>>(), (dic, currentItem) =>
        {
            K key = predicate(currentItem);
            if (!dic.ContainsKey(key))
                dic.Add(key, Enumerable.Empty<V>());

            dic[key] = dic[key].Append(currentItem);
            return dic;
        });
    }

    public static void CustomForEach<T>(this IEnumerable<T> col, Action<T> action)
    {
        if (!col.Any()) return;

        foreach (var item in col)
        {
            action(item);
        }
    }

    public static IEnumerable<Tuple<T, int>> ZipWithIndex<T>(this IEnumerable<T> col)
    {
        int infinite = col.Count();
        return col.Zip(Enumerable.Range(0, infinite), (x, y) => Tuple.Create(x, y));
    }




    //public static IEnumerable<T> GetRandomAmount<T>(this IEnumerable<T> col,int quantity = 1)
    //{
    //    HashSet<T> list = new HashSet<T>();
    //    while (quantity>0)
    //    {
    //        yield return col.Skip(Random.Range(0, col.Count())).Take(1);
    //        quantity--;
    //    }
    //}


}

