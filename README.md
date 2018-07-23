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
Frequency=3421881 Hz, Resolution=292.2369 ns, Timer=TSC
.NET Core SDK=2.1.104
  [Host]     : .NET Core 2.0.6 (CoreCLR 4.6.26212.01, CoreFX 4.6.26212.01), 64bit RyuJIT
  Job-LNUHGS : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3131.0
  Job-MRUQFJ : .NET Core 2.0.6 (CoreCLR 4.6.26212.01, CoreFX 4.6.26212.01), 64bit RyuJIT

RunStrategy=ColdStart  

                     Method | Runtime |         Mean |       Error |      StdDev |
--------------------------- |-------- |-------------:|------------:|------------:|
               RegisterType |     Clr |   420.921 us | 1,380.15 us | 4,069.41 us |
               RegisterLazy |     Clr |   717.123 us | 2,343.60 us | 6,910.14 us |
                ResolveType |     Clr | 1,728.152 us | 1,159.80 us | 3,419.69 us |
                ResolveLazy |     Clr |   398.445 us | 1,329.19 us | 3,919.15 us |
               InvokeMethod |     Clr |     3.542 us |    11.39 us |    33.59 us |
  InvokeLazyMethodFirstTime |     Clr |    65.259 us |   220.09 us |   648.93 us |
 InvokeLazyMethodSecondTime |     Clr |     3.329 us |    10.30 us |    30.36 us |
               RegisterType |    Core |   264.074 us |   818.77 us | 2,414.16 us |
               RegisterLazy |    Core |   480.817 us | 1,527.39 us | 4,503.55 us |
                ResolveType |    Core | 1,937.475 us | 1,679.42 us | 4,951.80 us |
                ResolveLazy |    Core |   484.272 us | 1,618.87 us | 4,773.26 us |
               InvokeMethod |    Core |     4.907 us |    15.25 us |    44.97 us |
  InvokeLazyMethodFirstTime |    Core |    67.729 us |   227.56 us |   670.97 us |
 InvokeLazyMethodSecondTime |    Core |     4.173 us |    12.04 us |    35.50 us |

// * Legends *
  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  StdDev : Standard deviation of all measurements
  1 us   : 1 Microsecond (0.000001 sec)
```

## License

This project is licensed under the Apache License, Version 2.0. - see the [LICENSE](https://github.com/servicetitan/lazy-proxy/blob/master/LICENSE) file for details.
