using System;
using System.Collections.Generic;

public interface IGameEventBus
{
    void Register<T>(EventBinding<T> binding);
    void Deregister<T>(EventBinding<T> binding);
    void Raise<T>(T eventData);
}

public interface IEventBinding
{
}

public sealed class EventBinding<T> : IEventBinding
{
    private readonly Action<T> action;

    public EventBinding(Action<T> action)
    {
        this.action = action;
    }

    public void Invoke(T eventData)
    {
        action?.Invoke(eventData);
    }
}

public sealed class GameEventBus : IGameEventBus
{
    private readonly Dictionary<Type, List<IEventBinding>> bindings = new Dictionary<Type, List<IEventBinding>>();

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

    public void Deregister<T>(EventBinding<T> binding)
    {
        Type eventType = typeof(T);
        if (bindings.TryGetValue(eventType, out List<IEventBinding> eventBindings))
        {
            eventBindings.Remove(binding);
        }
    }

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

    public void Clear()
    {
        bindings.Clear();
    }
}
