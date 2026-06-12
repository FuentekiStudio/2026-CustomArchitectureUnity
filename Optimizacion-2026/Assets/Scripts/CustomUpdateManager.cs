using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomUpdateManager : MonoBehaviour
{
    public IUpdateable[] updateableList;
    public IFixedUpdateable[] fixedUpdateableList;
    public ILateUpdateable[] lateUpdateableList;

    
    private List<IUpdateable> updateables = new List<IUpdateable>();
    private List<IUpdateable> updateablesToRemove = new List<IUpdateable>();

    private List<IFixedUpdateable> fixedUpdateables = new List<IFixedUpdateable>();
    private List<IFixedUpdateable> fixedUpdateablesToRemove = new List<IFixedUpdateable>();

    private List<ILateUpdateable> lateUpdateables = new List<ILateUpdateable>();
    private List<ILateUpdateable> lateUpdateablesToRemove = new List<ILateUpdateable>();


    void Start()
    {
        CLogger.Log("CustomUpdateManager Init");


    }

    
    void Update()
    {
        foreach (IUpdateable updateable in updateables)
        {
            updateable.Update(Time.deltaTime);
        }
        foreach (IUpdateable updateable in updateablesToRemove)
        {
            updateables.Remove(updateable);
        }
        updateablesToRemove.Clear();

    }

    private void FixedUpdate()
    {
        foreach(IFixedUpdateable fixedUpdateable in fixedUpdateables)
        {
            fixedUpdateable.FixedUpdate(Time.fixedDeltaTime);
        }
        foreach(IFixedUpdateable fixedUpdatable in fixedUpdateablesToRemove)
        {
            fixedUpdateables.Remove(fixedUpdatable);
        }
        fixedUpdateablesToRemove.Clear ();
    }

    private void LateUpdate()
    {
        foreach (ILateUpdateable lateUpdateable in lateUpdateables)
        {
            lateUpdateable.LateUpdate(Time.deltaTime);
        }
        foreach(ILateUpdateable lateUpdateable in lateUpdateablesToRemove)
        {
            lateUpdateables.Remove(lateUpdateable);
        }
        lateUpdateablesToRemove.Clear();
    }

    public void RegisterUpdateable( IUpdateable updateable)
    {
        // Logica de registro. Lo hacemos directo por lista o por eventos?
        updateables.Add(updateable);
    }

    public void UnregisterUpdateable(IUpdateable updateable) 
    {
        updateablesToRemove.Add(updateable);
    }

    public void RegisterFixedUpdateable(IFixedUpdateable fixedUpdateable)
    {
        // Logica de registro. Lo hacemos directo por lista o por eventos?
        fixedUpdateables.Add(fixedUpdateable);
    }

    public void UnregisterFixedUpdateable(IFixedUpdateable fixedUpdateable)
    {
        fixedUpdateablesToRemove.Add(fixedUpdateable);
    }

    public void RegisterLateUpdateable(ILateUpdateable lateUpdateable)
    {
        // Logica de registro. Lo hacemos directo por lista o por eventos?
        lateUpdateables.Add(lateUpdateable);
    }

    public void UnregisterLateUpdateable(ILateUpdateable lateUpdateable)
    {
        lateUpdateables.Add(lateUpdateable);
    }


}
