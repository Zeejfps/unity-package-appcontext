using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AppContextModule
{
    public sealed class Context
    {
        private readonly HashSet<Context> m_ChildContexts = new();

        public void AddChildContext(Context childContext)
        {
            m_ChildContexts.Add(childContext);
        }
        
        public void RemoveChildContext(Context context)
        {
            m_ChildContexts.Remove(context);
        }

        public T Get<T>()
        {
            var type = typeof(T);
            return (T)Get(type);
        }
        
        public object Get(Type type)
        {
            if (m_TypeToFactoryDictionary.TryGetValue(type, out var factory))
                return factory.Create();

            foreach (var childContext in m_ChildContexts)
            {
                if (childContext.TryGet(type, out var obj))
                    return obj;
            }
            
            throw new Exception($"Could not get object of type: {type}");
        }

        public bool TryGet(Type type, out object obj)
        {
            if (m_TypeToFactoryDictionary.TryGetValue(type, out var factory))
            {
                obj = factory.Create();
                return true;
            }
            obj = default;
            return false;
        }

        public void RegisterSingleton<T>(Func<Context, T> factoryMethod)
        {
            var type = typeof(T);
            if (!m_TypeToFactoryDictionary.TryAdd(type, new SingletonFactoryMethodFactory<T>(this, factoryMethod)))
                throw new Exception($"Singleton for {type} type already registered");
        }
        
        public void RegisterSingleton<TConcrete>(TConcrete singleton)
        {
            var interfaceType = typeof(TConcrete);
            try
            {
                m_TypeToFactoryDictionary.Add(interfaceType, new SingletonFactory<TConcrete>(this, singleton));
            }
            catch (ArgumentException)
            {
                throw new Exception($"Singleton for {interfaceType} type already registered");
            }
        }
        
        public void RegisterSingleton<TConcrete>()
        {
            var type = typeof(TConcrete);
            try
            {
                m_TypeToFactoryDictionary.Add(type, new SingletonFactory<TConcrete>(this));
            }
            catch (ArgumentException)
            {
                throw new Exception($"Singleton for {type} type already registered");
            }
        }
        
        public void RegisterSingleton<TInterface, TConcrete>() where TConcrete : TInterface
        {
            var type = typeof(TInterface);
            try
            {
                m_TypeToFactoryDictionary.Add(type, new SingletonFactory<TConcrete>(this));
            }
            catch (ArgumentException)
            {   
                throw new Exception($"Singleton for {type} type already registered");
            }
        }

        public void Unregister<T>()
        {
            var interfaceType = typeof(T);
            m_TypeToFactoryDictionary.Remove(interfaceType);
        }
        
        #region Private

        private readonly Dictionary<Type, IFactory> m_TypeToFactoryDictionary = new();

        private T New<T>()
        {
            var type = typeof(T);
            return (T)New(type);
        }
        
        private object New(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var paramValues = new List<object>();
            foreach (var constructor in constructors)
            {
                var isValidConstructor = true;
                var parameters = constructor.GetParameters();
                foreach (var parameter in parameters)
                {
                    var paramType = parameter.ParameterType;
                    if (!m_TypeToFactoryDictionary.TryGetValue(paramType, out var factory))
                    {
                        Debug.LogError($"No Factory for: {paramType}");
                        isValidConstructor = false;
                        break;
                    }
            
                    paramValues.Add(factory.Create());
                }
        
                if (isValidConstructor)
                    return Activator.CreateInstance(type, paramValues.ToArray());
        
                paramValues.Clear();
            }

            throw new Exception($"Could not instantiate object of type {type}");
        }
        
        private sealed class SingletonFactory<T> : IFactory
        {
            private readonly Context m_Context;
            private T m_Singleton;

            public SingletonFactory(Context context, T singleton = default)
            {
                m_Context = context;
                m_Singleton = singleton;
            }
    
            public object Create()
            {
                if (m_Singleton == null)
                {
                    if (m_Context.m_TypeToFactoryDictionary.TryGetValue(typeof(T), out var factory) && factory != this)
                    {
                        m_Singleton = (T)factory.Create();
                    }
                    else
                    {
                        m_Singleton = m_Context.New<T>();
                    }
                }
                return m_Singleton;
            }
        }

        private sealed class SingletonFactoryMethodFactory<T> : IFactory
        {
            private readonly Context m_Context;
            private readonly Func<Context, T> m_FactoryMethod;
            
            private T m_Singleton;

            public SingletonFactoryMethodFactory(Context context, Func<Context, T> factoryMethod)
            {
                m_Context = context;
                m_FactoryMethod = factoryMethod;
            }

            public object Create()
            {
                if (m_Singleton == null)
                {
                    m_Singleton =  m_FactoryMethod.Invoke(m_Context);
                }
                return m_Singleton;
            }
        }
    
        private interface IFactory
        {
            object Create();
        }
        #endregion
    }
}