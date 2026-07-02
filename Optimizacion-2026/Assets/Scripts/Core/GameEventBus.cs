using System;
using System.Collections.Generic;

/// <summary>
/// Contrato del bus de eventos usado por sistemas para comunicarse sin referencias directas.
/// </summary>
public interface IGameEventBus
{
    void Register<T>(EventBinding<T> binding);
    void Deregister<T>(EventBinding<T> binding);
    void Raise<T>(T eventData);
}

/// <summary>
/// Marcador común para almacenar bindings de distintos tipos en una misma colección.
/// </summary>
public interface IEventBinding
{
}

/// <summary>
/// Suscripción tipada a un evento del juego. La crean sistemas como UISystem y PlayerSystem.
/// </summary>
public sealed class EventBinding<T> : IEventBinding
{
    private readonly Action<T> action;

    /// <summary>
    /// Guarda la acción que se ejecutará cuando el EventBus publique el evento T.
    /// </summary>
    public EventBinding(Action<T> action)
    {
        this.action = action;
    }

    /// <summary>
    /// Ejecuta la acción suscripta con los datos del evento recibido.
    /// </summary>
    public void Invoke(T eventData)
    {
        action?.Invoke(eventData);
    }
}

/// <summary>
/// Implementación central del bus de eventos. La registra GameBootstrap y la usan sistemas de gameplay y UI.
/// </summary>
public sealed class GameEventBus : IGameEventBus
{
    private readonly Dictionary<Type, List<IEventBinding>> bindings = new Dictionary<Type, List<IEventBinding>>();

    /// <summary>
    /// Registra un listener para eventos del tipo T.
    /// </summary>
    public void Register<T>(EventBinding<T> binding)
    {
        Type eventType = typeof(T);
        if (!bindings.TryGetValue(eventType, out List<IEventBinding> eventBindings))
        {
            eventBindings = new List<IEventBinding>();
            bindings[eventType] = eventBindings;
        }

        if (!eventBindings.Contains(binding))
        {
            eventBindings.Add(binding);
        }
    }

    /// <summary>
    /// Quita un listener previamente registrado para eventos del tipo T.
    /// </summary>
    public void Deregister<T>(EventBinding<T> binding)
    {
        Type eventType = typeof(T);
        if (bindings.TryGetValue(eventType, out List<IEventBinding> eventBindings))
        {
            eventBindings.Remove(binding);
        }
    }

    /// <summary>
    /// Publica un evento y notifica a todos los listeners suscriptos a su tipo.
    /// </summary>
    public void Raise<T>(T eventData)
    {
        Type eventType = typeof(T);
        if (!bindings.TryGetValue(eventType, out List<IEventBinding> eventBindings))
        {
            return;
        }

        for (int i = 0; i < eventBindings.Count; i++)
        {
            if (eventBindings[i] is EventBinding<T> binding)
            {
                binding.Invoke(eventData);
            }
        }
    }

    /// <summary>
    /// Elimina todas las suscripciones activas.
    /// </summary>
    public void Clear()
    {
        bindings.Clear();
    }
}
