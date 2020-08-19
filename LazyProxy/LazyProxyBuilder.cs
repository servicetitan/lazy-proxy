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

        private static readonly MethodInfo CreateLazyMethod = typeof(LazyProxyImplementation)
            .GetMethod(nameof(LazyProxyImplementation.CreateInstance), BindingFlags.Public | BindingFlags.Static);

        private static readonly MethodInfo DisposeLazyMethod = typeof(LazyProxyImplementation)
            .GetMethod(nameof(LazyProxyImplementation.DisposeInstance), BindingFlags.Public | BindingFlags.Static);

        private static readonly Type DisposableInterface = typeof(IDisposable);

        private static readonly MethodInfo DisposeMethod = DisposableInterface
            .GetMethod(nameof(IDisposable.Dispose), BindingFlags.Public | BindingFlags.Instance);

        private static readonly Type[] InitializeMethodParameterTypes = new [] {typeof(Func<object>)};

        private static Type LazyProxyBaseType = typeof(LazyProxyBase);

        private static Type LazyType = typeof(Lazy<>);


        /// <summary>
        /// Defines at runtime a class that implements interface T
        /// and proxies all invocations to <see cref="Lazy{T}"/> of this interface.
        /// </summary>
        /// <typeparam name="T">The interface proxy type implements.</typeparam>
        /// <returns>The lazy proxy type.</returns>
        public static Type GetType<T>() where T : class
        {
            return GetType(typeof(T));
        }

        /// <summary>
        /// Defines at runtime a class that implements interface of Type
        /// and proxies all invocations to <see cref="Lazy{T}"/> of this interface.
        /// </summary>
        /// <param name="type">The interface proxy type implements.</param>
        /// <returns>The lazy proxy type.</returns>
        public static Type GetType(Type type)
        {
            // There is no way to constraint it on the compilation step.
            if (!type.IsInterface)
            {
                throw new NotSupportedException("The lazy proxy is supported only for interfaces.");
            }

            var interfaceType = type.IsConstructedGenericType
                ? type.GetGenericTypeDefinition()
                : type;

            // Lazy is used to guarantee the valueFactory is invoked only once.
            // More info: http://reedcopsey.com/2011/01/16/concurrentdictionarytkeytvalue-used-with-lazyt/
            var lazy = ProxyTypes.GetOrAdd(interfaceType, t => new Lazy<Type>(() => DefineProxyType(t)));
            var proxyType = lazy.Value;

            return type.IsConstructedGenericType
                ? proxyType.MakeGenericType(type.GetGenericArguments())
                : proxyType;
        }

        /// <summary>
        /// Creates a lazy proxy type instance using a value factory.
        /// </summary>
        /// <param name="valueFactory">The function real value returns.</param>
        /// <typeparam name="T">The interface proxy type implements.</typeparam>
        /// <returns>The lazy proxy type instance.</returns>
        public static T CreateInstance<T>(Func<T> valueFactory) where T : class
        {
            return (T) CreateInstance(typeof(T), valueFactory);
        }

        /// <summary>
        /// Creates a lazy proxy type instance using a value factory.
        /// </summary>
        /// <param name="type">The interface proxy type implements.</param>
        /// <param name="valueFactory">The function real value returns.</param>
        /// <returns>The lazy proxy type instance.</returns>
        public static object CreateInstance(Type type, Func<object> valueFactory)
        {
            var proxyType = GetType(type);

            // Using 'Initialize' method after the instance creation allows to improve performance
            // because Activator.CreateInstance method performance is much worse with arguments.
            var instance = (LazyProxyBase) Activator.CreateInstance(proxyType);
            instance.Initialize(valueFactory);

            return instance;
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
        ///     public void Initialize(Func<object> valueFactory)
        ///     {
        ///         _service = LazyBuilder.CreateInstance<IMyService>(valueFactory);
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

            return ModuleBuilder.DefineType(typeName, TypeAttributes.Public, LazyProxyBaseType)
                .AddGenericParameters(type)
                .AddInterface(type)
                .AddServiceField(type, out var serviceField)
                .AddInitializeMethod(type, serviceField)
                .AddMethods(type, serviceField)
                .AddDisposeMethodIfNeeded(type, serviceField)
                .CreateTypeInfo();
        }

        private static TypeBuilder AddGenericParameters(this TypeBuilder typeBuilder, Type type)
        {
            if (type.IsGenericTypeDefinition)
            {
                AddGenericParameters(type.GetGenericArguments, typeBuilder.DefineGenericParameters);
            }

            return typeBuilder;
        }

        private static TypeBuilder AddInterface(this TypeBuilder typeBuilder, Type type)
        {
            typeBuilder.AddInterfaceImplementation(type);
            return typeBuilder;
        }

        private static TypeBuilder AddServiceField(this TypeBuilder typeBuilder,
            Type type, out FieldInfo serviceField)
        {
            serviceField = typeBuilder.DefineField(
                ServiceFieldName,
                LazyType.MakeGenericType(type),
                FieldAttributes.Private);

            return typeBuilder;
        }

        private static TypeBuilder AddInitializeMethod(this TypeBuilder typeBuilder, Type type, FieldInfo serviceField)
        {
            var methodBuilder = typeBuilder.DefineMethod(
                nameof(LazyProxyBase.Initialize),
                MethodAttributes.Family | MethodAttributes.Virtual,
                null,
                InitializeMethodParameterTypes
            );

            var createLazyMethod = CreateLazyMethod.MakeGenericMethod(type);

            var generator = methodBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Call, createLazyMethod);
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
                    AddGenericParameters(method.GetGenericArguments, methodBuilder.DefineGenericParameters);
                }

                var generator = methodBuilder.GetILGenerator();

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, serviceField);
                generator.Emit(OpCodes.Call, getServiceValueMethod);

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

        private static TypeBuilder AddDisposeMethodIfNeeded(this TypeBuilder typeBuilder, Type type, FieldInfo serviceField)
        {
            if (!DisposableInterface.IsAssignableFrom(type)) {
                return typeBuilder;
            }

            var methodBuilder = typeBuilder.DefineMethod(
                DisposeMethod.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                DisposeMethod.ReturnType,
                Array.Empty<Type>()
            );

            var disposeLazyMethod = DisposeLazyMethod.MakeGenericMethod(type);

            var generator = methodBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, serviceField);
            generator.Emit(OpCodes.Call, disposeLazyMethod);
            generator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, DisposeMethod);

            return typeBuilder;
        }

        private static IEnumerable<MethodInfo> GetMethods(Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            var isDisposable = DisposableInterface.IsAssignableFrom(type);
            return type.GetMethods(flags)
                .Concat(type.GetInterfaces()
                    .SelectMany(@interface => @interface.GetMethods(flags)))
                .Where(method => !isDisposable || method.Name != nameof(IDisposable.Dispose))
                .Distinct();
        }

        private static MethodInfo GetGetServiceValueMethod(FieldInfo serviceField)
        {
            // ReSharper disable once PossibleNullReferenceException
            return serviceField.FieldType.GetProperty("Value").GetGetMethod(true);
        }

        private static void AddGenericParameters(
            Func<IReadOnlyList<Type>> getGenericParameters,
            Func<string[], IReadOnlyList<GenericTypeParameterBuilder>> defineGenericParameters)
        {
            var genericParameters = getGenericParameters();

            var genericParametersNames = genericParameters
                .Select(genericType => genericType.Name)
                .ToArray();

            var definedGenericParameters = defineGenericParameters(genericParametersNames);

            for (var i = 0; i < genericParameters.Count; i++)
            {
                var genericParameter = genericParameters[i];
                var definedGenericParameter = definedGenericParameters[i];
                var genericParameterAttributes = genericParameter.GenericParameterAttributes
                                                 & ~GenericParameterAttributes.Covariant
                                                 & ~GenericParameterAttributes.Contravariant;

                definedGenericParameter.SetGenericParameterAttributes(genericParameterAttributes);

                var genericParameterConstraints = genericParameter.GetGenericParameterConstraints();

                foreach (var constraint in genericParameterConstraints)
                {
                    if (constraint.IsInterface)
                    {
                        definedGenericParameter.SetInterfaceConstraints(constraint);
                    }
                    else
                    {
                        definedGenericParameter.SetBaseTypeConstraint(constraint);
                    }
                }
            }
        }
    }
}