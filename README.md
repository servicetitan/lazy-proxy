# LazyProxy

`LazyProxy` is a lightweight library allowing to build a lazy proxy type for some interface `T` at runtime. The proxy type implements this interface and is initialized by the `Lazy<T>` argument. All method and property invocations route to the corresponding members of the lazy's `Value`.

For illustration, assume there is the following interface:

```CSharp
public interface IMyService
{
    void Foo();
}
```

Then the generated lazy proxy type looks like this:

```CSharp
// In reality, the implementation is a little more complicated,
// but the details are omitted for ease of understanding.
public class LazyProxyImpl_IMyService : IMyService
{
    private Lazy<IMyService> _service;

    public LazyProxyImpl_IMyService(Lazy<IMyService> service)
    {
        _service = service;
    }

    public void Foo() => _service.Value.Foo();
}
```

## Get Packages

The library provides in NuGet.

```
Install-Package LazyProxy
```

## Get Started

Consider the following service:

```CSharp
public interface IMyService
{
    void Foo();
}

public class MyService : IMyService
{
    public MyService() => Console.WriteLine("Ctor");
    public void Foo() => Console.WriteLine("Foo");
}
```

A lazy proxy instance for this service can be created like this:

```CSharp
var lazyProxy = LazyProxyBuilder.CreateInstance<IMyService>(() =>
{
    Console.WriteLine("Creating an instance of the real service...");
    return new MyService();
});

Console.WriteLine("Executing the 'Foo' method...");
lazyProxy.Foo();
```

The output for this example:

```
Executing the 'Foo' method...
Creating an instance of the real service...
Ctor
Foo
```

## Features

Currently, `LazyProxy` supports the following:
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

## Lazy Dependency Injection

Lazy proxies can be used for IoC containers to improve performance by changing resolution behavior.

More info can be found in the article about [Lazy Dependency Injection for .NET](https://dev.to/hypercodeplace/lazy-dependency-injection-37en).

[Lazy injection for Unity container](https://github.com/servicetitan/lazy-proxy-unity)

[Lazy injection for Autofac container](https://github.com/servicetitan/lazy-proxy-autofac)

## License

This project is licensed under the Apache License, Version 2.0. - see the [LICENSE](https://github.com/servicetitan/lazy-proxy/blob/master/LICENSE) file for details.
