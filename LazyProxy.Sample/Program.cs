using System;

namespace LazyProxy.Sample
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var proxy = LazyProxyBuilder.CreateLazyProxyInstance<IMyService>(() =>
            {
                Console.WriteLine("The real instance creation...");
                return new MyService();
            });

            Console.WriteLine("Foo execution...");
            proxy.Foo();
        }
    }
}