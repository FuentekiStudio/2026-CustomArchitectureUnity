using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonoBehaviour encargado de ejecutar Update, FixedUpdate y LateUpdate para sistemas registrados.
/// Lo configura GameBootstrap para evitar callbacks dispersos en entidades individuales.
/// </summary>
public class CustomUpdateManager : MonoBehaviour
{
    private readonly List<IUpdateable> updateables = new List<IUpdateable>();
    private readonly List<IUpdateable> updateablesToRemove = new List<IUpdateable>();

    private readonly List<IFixedUpdateable> fixedUpdateables = new List<IFixedUpdateable>();
    private readonly List<IFixedUpdateable> fixedUpdateablesToRemove = new List<IFixedUpdateable>();

    private readonly List<ILateUpdateable> lateUpdateables = new List<ILateUpdateable>();
    private readonly List<ILateUpdateable> lateUpdateablesToRemove = new List<ILateUpdateable>();

    /// <summary>
    /// Ejecuta los sistemas de lógica por frame y procesa bajas diferidas.
    /// </summary>
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

    /// <summary>
    /// Ejecuta sistemas dependientes de física o simulación fija.
    /// </summary>
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

    /// <summary>
    /// Ejecuta sistemas que deben sincronizarse después de la simulación principal.
    /// </summary>
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

    /// <summary>
    /// Agrega un sistema al loop de Update si todavía no está registrado.
    /// </summary>
    public void RegisterUpdateable(IUpdateable updateable)
    {
        if (updateable != null && !updateables.Contains(updateable))
        {
            updateables.Add(updateable);
        }
    }

    /// <summary>
    /// Agenda la remoción de un sistema del loop de Update.
    /// </summary>
    public void UnregisterUpdateable(IUpdateable updateable)
    {
        if (updateable != null && !updateablesToRemove.Contains(updateable))
        {
            updateablesToRemove.Add(updateable);
        }
    }

    /// <summary>
    /// Agrega un sistema al loop de FixedUpdate si todavía no está registrado.
    /// </summary>
    public void RegisterFixedUpdateable(IFixedUpdateable fixedUpdateable)
    {
        if (fixedUpdateable != null && !fixedUpdateables.Contains(fixedUpdateable))
        {
            fixedUpdateables.Add(fixedUpdateable);
        }
    }

    /// <summary>
    /// Agenda la remoción de un sistema del loop de FixedUpdate.
    /// </summary>
    public void UnregisterFixedUpdateable(IFixedUpdateable fixedUpdateable)
    {
        if (fixedUpdateable != null && !fixedUpdateablesToRemove.Contains(fixedUpdateable))
        {
            fixedUpdateablesToRemove.Add(fixedUpdateable);
        }
    }

    /// <summary>
    /// Agrega un sistema al loop de LateUpdate si todavía no está registrado.
    /// </summary>
    public void RegisterLateUpdateable(ILateUpdateable lateUpdateable)
    {
        if (lateUpdateable != null && !lateUpdateables.Contains(lateUpdateable))
        {
            lateUpdateables.Add(lateUpdateable);
        }
    }

    /// <summary>
    /// Agenda la remoción de un sistema del loop de LateUpdate.
    /// </summary>
    public void UnregisterLateUpdateable(ILateUpdateable lateUpdateable)
    {
        if (lateUpdateable != null && !lateUpdateablesToRemove.Contains(lateUpdateable))
        {
            lateUpdateablesToRemove.Add(lateUpdateable);
        }
    }

    /// <summary>
    /// Limpia todos los sistemas registrados y las listas de remoción pendientes.
    /// </summary>
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
