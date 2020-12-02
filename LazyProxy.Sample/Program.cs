using System;

namespace LazyProxy.Sample
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var lazyProxy = LazyProxyBuilder.CreateInstance<IMyService>(() =>
            {
                Console.WriteLine("Creating an instance of the real service...");
                return new MyService();
            });

            Console.WriteLine("Executing the 'Foo' method...");
            lazyProxy.Foo();
        }
    }
}
