using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace LazyProxy
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
        /// and proxies all invocations to <see cref="Lazy{T}"/> of this interface.
        /// </summary>
        /// <typeparam name="T">The interface proxy type implements.</typeparam>
        /// <returns>The lazy proxy type.</returns>
        public static Type BuildLazyProxyType<T>()
        {
            return BuildLazyProxyType(typeof(T));
        }

        /// <summary>
        /// Defines at runtime a class that implements interface of Type
        /// and proxies all invocations to <see cref="Lazy{T}"/> of this interface.
        /// </summary>
        /// <param name="type">The interface proxy type implements.</param>
        /// <returns>The lazy proxy type.</returns>
        public static Type BuildLazyProxyType(Type type)
        {
            // There is no way to constraint it on the compilation step.
            if (!type.IsInterface)
            {
                throw new NotSupportedException("The lazy proxy is supported only for interfaces.");
            }

            // Lazy is used to guarantee the valueFactory is invoked only once.
            // More info: http://reedcopsey.com/2011/01/16/concurrentdictionarytkeytvalue-used-with-lazyt/
            var lazy = ProxyTypes.GetOrAdd(type, t => new Lazy<Type>(() => DefineProxyType(t)));
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

        /// <summary>
        /// Generate the lazy proxy type at runtime.
        ///
        /// Here is an example of the generated code for T == IMyService:
        /// <![CDATA[
        ///
        /// public interface IMyService { void Foo(); }
        ///
        /// public class LazyProxyImpl_1eb94ccd79fd48af8adfbc97c76c10ff_IMyService : IMyService
        /// {
        ///     private Lazy<IMyService> _service;
        ///
        ///     public LazyProxyImpl_1eb94ccd79fd48af8adfbc97c76c10ff_IMyService(Lazy<IMyService> service)
        ///     {
        ///         _service = service;
        ///     }
        ///
        ///     public void Foo() => _service.Value.Foo();
        /// }
        ///
        /// ]]>
        /// </summary>
        /// <param name="type">The interface proxy type implements.</param>
        /// <returns>The lazy proxy type.</returns>
        private static Type DefineProxyType(Type type)
        {
            // Add a guid to avoid problems with defining generic types with different type parameters.
            // Dashes are allowed by IL but they are removed to match the class names in C#.
            var guid = Guid.NewGuid().ToString().Replace("-", "");

            var typeName = $"{type.Namespace}.{LazyProxyTypeSuffix}_{guid}_{type.Name}";

            return ModuleBuilder.DefineType(typeName, TypeAttributes.Public)
                .AddInterface(type)
                .AddServiceField(type, out var serviceField)
                .AddConstructor(type, serviceField)
                .AddMethods(type, serviceField)
                .CreateTypeInfo();
        }

        private static TypeBuilder AddInterface(this TypeBuilder typeBuilder, Type type)
        {
            if (type.IsGenericTypeDefinition)
            {
                var parameterNames = type.GetGenericArguments()
                    .Select(p => p.Name)
                    .ToArray();

                typeBuilder.DefineGenericParameters(parameterNames);
            }

            typeBuilder.AddInterfaceImplementation(type);
            return typeBuilder;
        }

        private static TypeBuilder AddServiceField(this TypeBuilder typeBuilder,
            Type type, out FieldInfo serviceField)
        {
            serviceField = typeBuilder.DefineField(
                ServiceFieldName,
                typeof(Lazy<>).MakeGenericType(type),
                FieldAttributes.Private);

            return typeBuilder;
        }

        private static TypeBuilder AddConstructor(this TypeBuilder typeBuilder, Type type, FieldInfo serviceField)
        {
            var constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] {typeof(Lazy<>).MakeGenericType(type)}
            );

            var generator = constructorBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stfld, serviceField);
            generator.Emit(OpCodes.Ret);

            return typeBuilder;
        }

        private static TypeBuilder AddMethods(this TypeBuilder typeBuilder, Type type, FieldInfo serviceField)
        {
            var methods = GetMethods(type);
            var getServiceValueMethod = GetGetServiceValueMethod(serviceField);

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
                generator.Emit(OpCodes.Call, getServiceValueMethod);

                for (var i = 1; i < parameterTypes.Length + 1; i++)
                {
                    generator.Emit(OpCodes.Ldarg, i);
                }

                generator.Emit(OpCodes.Call, method);
                generator.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            return typeBuilder;
        }

        private static IEnumerable<MethodInfo> GetMethods(Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            return type.GetMethods(flags)
                .Concat(type.GetInterfaces()
                    .SelectMany(@interface => @interface.GetMethods(flags)))
                .Distinct();
        }

        private static MethodInfo GetGetServiceValueMethod(FieldInfo serviceField)
        {
            // ReSharper disable once PossibleNullReferenceException
            return serviceField.FieldType.GetProperty("Value").GetGetMethod(true);
        }
    }
}