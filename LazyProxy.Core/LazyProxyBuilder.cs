using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace LazyProxy.Core
{
    /// <summary>
    /// Is used to create at runtime a lazy proxy type or an instance of this type.
    /// </summary>
    public static class LazyProxyBuilder
    {
        private const string DynamicAssemblyName = "LazyProxy.DynamicTypes";
        private const string LazyProxyTypeSuffix = "LazyProxyImpl";
        private const string ServiceFieldName = "_service";

        private static readonly AssemblyBuilder AssemblyBuilder = AssemblyBuilder
            .DefineDynamicAssembly(new AssemblyName(DynamicAssemblyName), AssemblyBuilderAccess.Run);

        private static readonly ModuleBuilder ModuleBuilder = AssemblyBuilder
            .DefineDynamicModule(DynamicAssemblyName);

        private static readonly ConcurrentDictionary<Type, Lazy<Type>> ProxyTypes =
            new ConcurrentDictionary<Type, Lazy<Type>>();

        /// <summary>
        /// Defines at runtime a class that implements interface T
        /// and proxis all invocations to <see cref="Lazy{T}"/> of this interface.
        /// </summary>
        /// <typeparam name="T">The interface proxy type implements.</typeparam>
        /// <returns>The lazy proxy type.</returns>
        public static Type BuildLazyProxyType<T>()
        {
            // Lazy is used to guarantee the valueFactory is invoked only once.
            // More info: http://reedcopsey.com/2011/01/16/concurrentdictionarytkeytvalue-used-with-lazyt/
            var lazy = ProxyTypes.GetOrAdd(typeof(T), type => new Lazy<Type>(DefineProxyType<T>));
            return lazy.Value;
        }

        /// <summary>
        /// Creates a lazy proxy type instance using a value factory.
        /// </summary>
        /// <param name="valueFactory">The function real value returns.</param>
        /// <typeparam name="T">The interface proxy type implements.</typeparam>
        /// <returns>The lazy proxy type instance.</returns>
        public static T CreateLazyProxyInstance<T>(Func<T> valueFactory)
        {
            var lazy = new Lazy<T>(valueFactory);
            var proxyType = BuildLazyProxyType<T>();
            return (T) Activator.CreateInstance(proxyType, lazy);
        }

        private static Type DefineProxyType<T>()
        {
            // Add a guid to avoid problems with defining generic types with different type parameters.
            var guid = Guid.NewGuid().ToString();

            var type = typeof(T);
            var typeName = $"{type.Namespace}.{guid}_{type.Name}{LazyProxyTypeSuffix}";

            return ModuleBuilder.DefineType(typeName, TypeAttributes.Public)
                .AddInterfaceImplementation<T>()
                .AddServiceField<T>(out var serviceField)
                .AddConstructor<T>(serviceField)
                .AddMethods<T>(serviceField)
                .CreateTypeInfo();
        }

        private static TypeBuilder AddInterfaceImplementation<T>(this TypeBuilder typeBuilder)
        {
            typeBuilder.AddInterfaceImplementation(typeof(T));
            return typeBuilder;
        }

        private static TypeBuilder AddServiceField<T>(this TypeBuilder typeBuilder, out FieldInfo serviceField)
        {
            serviceField = typeBuilder.DefineField(ServiceFieldName, typeof(Lazy<T>), FieldAttributes.Private);
            return typeBuilder;
        }

        private static TypeBuilder AddConstructor<T>(this TypeBuilder typeBuilder, FieldInfo serviceField)
        {
            var constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] {typeof(Lazy<T>)}
            );

            var generator = constructorBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stfld, serviceField);
            generator.Emit(OpCodes.Ret);

            return typeBuilder;
        }

        private static TypeBuilder AddMethods<T>(this TypeBuilder typeBuilder, FieldInfo serviceField)
        {
            var methods = GetMethods<T>();
            var getServiceValueMethod = GetGetServiceValueMethod<T>(serviceField);

            foreach (var method in methods)
            {
                var parameterTypes = method.GetParameters()
                    .Select(p => p.ParameterType)
                    .ToArray();

                var methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    method.ReturnType,
                    parameterTypes
                );

                if (method.IsGenericMethod)
                {
                    var genericTypeNames = method.GetGenericArguments()
                        .Select(genericType => genericType.Name)
                        .ToArray();

                    methodBuilder.DefineGenericParameters(genericTypeNames);
                }

                var generator = methodBuilder.GetILGenerator();

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, serviceField);
                generator.Emit(OpCodes.Callvirt, getServiceValueMethod);

                for (var i = 1; i < parameterTypes.Length + 1; i++)
                {
                    generator.Emit(OpCodes.Ldarg, i);
                }

                generator.Emit(OpCodes.Callvirt, method);
                generator.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            return typeBuilder;
        }

        private static IEnumerable<MethodInfo> GetMethods<T>()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            return typeof(T).GetMethods(flags)
                .Concat(typeof(T).GetInterfaces()
                    .SelectMany(@interface => @interface.GetMethods(flags)))
                .Distinct();
        }

        private static MethodInfo GetGetServiceValueMethod<T>(FieldInfo serviceField)
        {
            // ReSharper disable once PossibleNullReferenceException
            return serviceField.FieldType.GetProperty(nameof(Lazy<T>.Value)).GetGetMethod(true);
        }
    }
}