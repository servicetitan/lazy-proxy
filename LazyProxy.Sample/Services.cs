using System;

namespace LazyProxy.Sample
{
    public interface IMyService
    {
        void Foo();
    }

    public class MyService : IMyService
    {
        public MyService() => Console.WriteLine("Hello from ctor");
        public void Foo() => Console.WriteLine("Hello from Foo");
    }
}