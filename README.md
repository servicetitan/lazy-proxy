# Dynamic lazy proxy

A dynamic lazy proxy is a class built in real time, that implemenets some interface `T`, takes to the constructor an argument `Lazy<T>` and routes all invocations to the corresponding method or property of this argument.

The real instance wrapped by `Lazy<T>` is created only after the first invocation of method or property. It allows to distribute the loading from the class creation to the method or property invocation.

```C#
public interface IMyService
{
	void Foo();
}

public class MyService : IMyService
{
	public MyService() => Console.WriteLine("Hello from ctor");
	public void Foo() => Console.WriteLine("Hello from Foo");
}

var proxy = LazyProxyBuilder.CreateLazyProxyInstance<IMyService>(() =>
{
	Console.WriteLine("The real instance creation...");
	return new MyService();
});

Console.WriteLine("Foo execution...");
proxy.Foo();

// Foo execution...
// The real instance creation...
// Hello from ctor
// Hello from Foo
```

The following is supported:
- Void/Result methods;
- Async methods;
- Generic methods;
- Generic interfaces;
- Ref/Out parameters;
- Parameters with default values;
- Parent interface members;
- Indexers;
- Properties (getters and setters);
- Thread-safe proxy generation.

**Not supported yet:**
- Events

## Lazy injection for IoC containers

A dynamic lazy proxy can be used for IoC containers to change the resolving behaviour.

Dependencies registered as lazy are created as dynamic proxy objects built in real time, but the real classes are resolved only after the first execution of proxy method or property.

Also dynamic lazy proxy allows injection of circular dependencies.

## Implementation for Unity

```C#
var container = new UnityContainer().RegisterLazy<IMyService, MyService>();

Console.WriteLine("Resolving service...");
var service = container.Resolve<IMyService>();

Console.WriteLine("Foo execution...");
service.Foo();

// Resolving service...
// Foo execution...
// Hello from ctor
// Hello from Foo

```

The following is supported:
- Registration of types by interfaces;
- Passing lifetime managers;
- Passing injection members;
- Resolving by child containers.

**Not supported yet:**
- Registration of instances.

## Performance

Here is a result of the [Benchmark test](https://github.com/servicetitan/lazy-proxy/blob/master/LazyProxy.Unity.Tests/UnityExtensionBenchmark.cs)

```
BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i5-6600K CPU 3.50GHz (Skylake), 1 CPU, 4 logical and 4 physical cores
Frequency=3421878 Hz, Resolution=292.2372 ns, Timer=TSC
  [Host]     : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3131.0
  Job-XLQBSP : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3131.0

RunStrategy=ColdStart  

                     Method |       Mean |        Error |      StdDev |
--------------------------- |-----------:|-------------:|------------:|
               RegisterType | 312.662 us | 1,014.060 us | 2,989.98 us |
               RegisterLazy | 460.423 us | 1,483.892 us | 4,375.29 us |
                ResolveType | 463.465 us | 1,552.554 us | 4,577.74 us |
                ResolveLazy | 363.829 us | 1,223.447 us | 3,607.36 us |
               InvokeMethod |   3.188 us |    10.095 us |    29.76 us |
  InvokeLazyMethodFirstTime |  94.673 us |   319.856 us |   943.10 us |
 InvokeLazyMethodSecondTime |   3.218 us |     9.952 us |    29.34 us |

// * Legends *
  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  StdDev : Standard deviation of all measurements
  1 us   : 1 Microsecond (0.000001 sec)
```

## License

This project is licensed under the Apache License, Version 2.0. - see the [LICENSE](https://github.com/servicetitan/lazy-proxy/blob/master/LICENSE) file for details.
