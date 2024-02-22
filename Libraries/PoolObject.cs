using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PoolObject<T>
{
    public PoolObject() { }

    [SerializeField] public Stack<T> pool = new Stack<T>();

    Action<T> turnOn;
    Action<T> turnOff;
    Func<T> build;

    int prewarm = 0;

    // al crearse una "PoolObject" se le deberan pasar estas referencias para que funcione correctamente
    public void Intialize(Action<T> _turnOn, Action<T> _turnOff, Func<T> _build, int prewarm = 5)
    {
        turnOn = _turnOn;
        turnOff = _turnOff;
        build = _build;
        this.prewarm = prewarm;
        pool = new();
        AddMore();
    }
    // obtiene el objeto de la lista, si no puede obtener ninguno instancia mas
    public T Get()
    {
        if (pool.Count <= 0) AddMore();
        var obj = pool.Pop();
        turnOn(obj);
        return obj;
    }
    // devuelve el objeto a la lista y llama a su metodo de apagado
    public void Return(T obj)
    {
        pool.Push(obj);
        turnOff(obj);
    }
    // añade mas objetos a la lista
    void AddMore()
    {
        var getName = build.Invoke();
        pool.Push(getName);
        turnOff(getName);

        for (int i = 0; i < prewarm - 1; i++)
        {
            var obj = build.Invoke();
            pool.Push(obj);
            turnOff(obj);
        }
        //Debug.LogWarning($"instancio  {prewarm} {getName} mas para su respectiva pool".ToUpper());
    }

}
