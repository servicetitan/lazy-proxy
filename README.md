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

[Lazy injection for Unity container](https://github.com/servicetitan/lazy-proxy-unity)

[Lazy injection for Autofac container](https://github.com/servicetitan/lazy-proxy-autofac)

## License

This project is licensed under the Apache License, Version 2.0. - see the [LICENSE](https://github.com/servicetitan/lazy-proxy/blob/master/LICENSE) file for details.
