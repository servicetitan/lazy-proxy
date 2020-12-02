using System;

namespace LazyProxy.Sample
{
    public interface IMyService
    {
        void Foo();
    }

    public class MyService : IMyService
    {
        public MyService() => Console.WriteLine("Ctor");
        public void Foo() => Console.WriteLine("Foo");
    }
}
