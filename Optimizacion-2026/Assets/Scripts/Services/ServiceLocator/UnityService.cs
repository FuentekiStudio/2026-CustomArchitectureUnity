using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityServiceLocator
{
    /// <summary>
    /// Registro de servicios creado por GameBootstrap. Permite centralizar dependencias sin usar singletons globales.
    /// </summary>
    public class ServiceLocator
    {
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        public IEnumerable<object> RegisteredServices => services.Values;

        /// <summary>
        /// Obtiene un servicio registrado por tipo o informa error si no existe.
        /// </summary>
        public T Get<T>()
        {
            Type type = typeof(T);
            if (services.TryGetValue(type, out object service))
            {
                return (T)service;
            }

            throw new ArgumentException($"ServiceLocator.Get: Service of type {type.FullName} not registered");
        }

        /// <summary>
        /// Indica si un servicio de tipo T está registrado.
        /// </summary>
        public bool Has<T>()
        {
            return services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Registra una instancia concreta usando su tipo genérico como clave.
        /// </summary>
        public ServiceLocator Register<T>(T service)
        {
            Type type = typeof(T);

            if (!services.TryAdd(type, service))
            {
                Debug.LogError($"ServiceLocator.Register: Service of type {type.FullName} already registered");
            }

            return this;
        }

        /// <summary>
        /// Registra una instancia contra un tipo explícito, útil para interfaces.
        /// </summary>
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

        /// <summary>
        /// Elimina todos los servicios registrados al cerrar o reiniciar la arquitectura.
        /// </summary>
        public void Clear()
        {
            services.Clear();
        }
    }

    /// <summary>
    /// Alias heredado del ServiceLocator para mantener compatibilidad con nombres previos del proyecto.
    /// </summary>
    public sealed class ServiceManager : ServiceLocator
    {
    }
}
