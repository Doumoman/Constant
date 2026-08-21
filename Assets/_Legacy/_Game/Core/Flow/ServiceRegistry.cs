#if LEGACY_DISABLED
using System;
using System.Collections.Generic;

namespace StarNight.Core.Flow
{
    public sealed class ServiceRegistry : IDisposable
    {
        private readonly Dictionary<Type, object> services = new();
        private readonly List<object> registrationOrder = new();

        public int Count => services.Count;

        public void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Type key = typeof(T);
            if (services.ContainsKey(key))
            {
                throw new InvalidOperationException($"Service '{key.FullName}' is already registered.");
            }

            services.Add(key, service);
            registrationOrder.Add(service);
        }

        public bool TryGet<T>(out T service) where T : class
        {
            if (services.TryGetValue(typeof(T), out object value))
            {
                service = value as T;
                return service != null;
            }

            service = null;
            return false;
        }

        public T GetRequired<T>() where T : class
        {
            if (TryGet(out T service))
            {
                return service;
            }

            throw new KeyNotFoundException($"Service '{typeof(T).FullName}' is not registered.");
        }

        public void Dispose()
        {
            for (int index = registrationOrder.Count - 1; index >= 0; index--)
            {
                if (registrationOrder[index] is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            registrationOrder.Clear();
            services.Clear();
        }
    }
}

#endif
