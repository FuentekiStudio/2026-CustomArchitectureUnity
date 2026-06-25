using System.Collections.Generic;
using UnityEngine;

public class CustomUpdateManager : MonoBehaviour
{
    private readonly List<IUpdateable> updateables = new List<IUpdateable>();
    private readonly List<IUpdateable> updateablesToRemove = new List<IUpdateable>();

    private readonly List<IFixedUpdateable> fixedUpdateables = new List<IFixedUpdateable>();
    private readonly List<IFixedUpdateable> fixedUpdateablesToRemove = new List<IFixedUpdateable>();

    private readonly List<ILateUpdateable> lateUpdateables = new List<ILateUpdateable>();
    private readonly List<ILateUpdateable> lateUpdateablesToRemove = new List<ILateUpdateable>();

    private void Update()
    {
        for (int i = 0; i < updateables.Count; i++)
        {
            updateables[i].Update(Time.deltaTime);
        }

        for (int i = 0; i < updateablesToRemove.Count; i++)
        {
            updateables.Remove(updateablesToRemove[i]);
        }

        updateablesToRemove.Clear();
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < fixedUpdateables.Count; i++)
        {
            fixedUpdateables[i].FixedUpdate(Time.fixedDeltaTime);
        }

        for (int i = 0; i < fixedUpdateablesToRemove.Count; i++)
        {
            fixedUpdateables.Remove(fixedUpdateablesToRemove[i]);
        }

        fixedUpdateablesToRemove.Clear();
    }

    private void LateUpdate()
    {
        for (int i = 0; i < lateUpdateables.Count; i++)
        {
            lateUpdateables[i].LateUpdate(Time.deltaTime);
        }

        for (int i = 0; i < lateUpdateablesToRemove.Count; i++)
        {
            lateUpdateables.Remove(lateUpdateablesToRemove[i]);
        }

        lateUpdateablesToRemove.Clear();
    }

    public void RegisterUpdateable(IUpdateable updateable)
    {
        if (updateable != null && !updateables.Contains(updateable))
        {
            updateables.Add(updateable);
        }
    }

    public void UnregisterUpdateable(IUpdateable updateable)
    {
        if (updateable != null && !updateablesToRemove.Contains(updateable))
        {
            updateablesToRemove.Add(updateable);
        }
    }

    public void RegisterFixedUpdateable(IFixedUpdateable fixedUpdateable)
    {
        if (fixedUpdateable != null && !fixedUpdateables.Contains(fixedUpdateable))
        {
            fixedUpdateables.Add(fixedUpdateable);
        }
    }

    public void UnregisterFixedUpdateable(IFixedUpdateable fixedUpdateable)
    {
        if (fixedUpdateable != null && !fixedUpdateablesToRemove.Contains(fixedUpdateable))
        {
            fixedUpdateablesToRemove.Add(fixedUpdateable);
        }
    }

    public void RegisterLateUpdateable(ILateUpdateable lateUpdateable)
    {
        if (lateUpdateable != null && !lateUpdateables.Contains(lateUpdateable))
        {
            lateUpdateables.Add(lateUpdateable);
        }
    }

    public void UnregisterLateUpdateable(ILateUpdateable lateUpdateable)
    {
        if (lateUpdateable != null && !lateUpdateablesToRemove.Contains(lateUpdateable))
        {
            lateUpdateablesToRemove.Add(lateUpdateable);
        }
    }

    public void Clear()
    {
        updateables.Clear();
        updateablesToRemove.Clear();
        fixedUpdateables.Clear();
        fixedUpdateablesToRemove.Clear();
        lateUpdateables.Clear();
        lateUpdateablesToRemove.Clear();
    }
}
