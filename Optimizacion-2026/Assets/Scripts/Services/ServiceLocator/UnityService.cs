using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityServiceLocator
{
    public class ServiceLocator
    {
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        public IEnumerable<object> RegisteredServices => services.Values;

        public T Get<T>()
        {
            Type type = typeof(T);
            if (services.TryGetValue(type, out object service))
            {
                return (T)service;
            }

            throw new ArgumentException($"ServiceLocator.Get: Service of type {type.FullName} not registered");
        }

        public bool Has<T>()
        {
            return services.ContainsKey(typeof(T));
        }

        public ServiceLocator Register<T>(T service)
        {
            Type type = typeof(T);

            if (!services.TryAdd(type, service))
            {
                Debug.LogError($"ServiceLocator.Register: Service of type {type.FullName} already registered");
            }

            return this;
        }

        public ServiceLocator Register(Type type, object service)
        {
            if (!type.IsInstanceOfType(service))
            {
                throw new ArgumentException("Type of service does not match type of service interface", nameof(service));
            }

            if (!services.TryAdd(type, service))
            {
                Debug.LogError($"ServiceLocator.Register: Service of type {type.FullName} already registered");
            }

            return this;
        }

        public void Clear()
        {
            services.Clear();
        }
    }

    public sealed class ServiceManager : ServiceLocator
    {
    }
}
