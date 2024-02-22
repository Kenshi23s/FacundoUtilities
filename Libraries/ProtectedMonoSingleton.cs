using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProtectedMonoSingleton<T> : MonoBehaviour where T : ProtectedMonoSingleton<T>
{
    protected static T _instance { get; private set; }
    //no se debe crear un awake nuevo, en caso de querer inicializar usar el metodo ArtificialAwake

    public ProtectedMonoSingleton()
    {
        //if (_instance != null)
        //{
        //    throw new System.Exception(typeof(ProtectedMonoSingleton<T>) + "already present at scene");
        //}
        //_instance = (T)this;
    }
    private void Awake()
    {
        if (_instance != null)
        {
            throw new System.Exception(typeof(ProtectedMonoSingleton<T>) + "already present at scene");
        }
        _instance = (T)this;
    }
}
