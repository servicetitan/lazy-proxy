using System;
using LazyProxy.Core;
using Unity;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Registration;

namespace LazyProxy.Unity
{
    public static class UnityExtensions
    {
        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method execution.
        /// </summary>
        /// <param name="container">The instance of Unity container.</param>
        /// <param name="injectionMembers">The set of injection members.</param>
        /// <typeparam name="TFrom">The binded interface.</typeparam>
        /// <typeparam name="TTo">The binded class.</typeparam>
        /// <returns>The instance of Unity container.</returns>
        public static IUnityContainer RegisterLazy<TFrom, TTo>(this IUnityContainer container,
            params InjectionMember[] injectionMembers)
            where TTo : TFrom where TFrom : class
        {
            return container.RegisterLazy<TFrom, TTo>(() => new TransientLifetimeManager(), injectionMembers);
        }

        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method or property execution.
        /// </summary>
        /// <param name="container">The instance of Unity container.</param>
        /// <param name="getLifetimeManager">The function instance lifetime provides.</param>
        /// <param name="injectionMembers">The set of injection members.</param>
        /// <typeparam name="TFrom">The binded interface.</typeparam>
        /// <typeparam name="TTo">The binded class.</typeparam>
        /// <returns>The instance of Unity container.</returns>
        public static IUnityContainer RegisterLazy<TFrom, TTo>(this IUnityContainer container,
            Func<LifetimeManager> getLifetimeManager,
            params InjectionMember[] injectionMembers)
            where TTo : TFrom where TFrom : class
        {
            var lazyProxyType = LazyProxyBuilder.BuildLazyProxyType<TFrom>();
            var registrationName = Guid.NewGuid().ToString();

            return container
                .RegisterType<TFrom, TTo>(registrationName, getLifetimeManager(), injectionMembers)
                .RegisterType(typeof(TFrom), lazyProxyType,
                    getLifetimeManager(),
                    new InjectionConstructor(
                        new ResolvedParameter<Lazy<TFrom>>(registrationName))
                );
        }
    }
}