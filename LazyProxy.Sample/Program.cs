using System;
using LazyProxy.Core;
using LazyProxy.Unity;
using Unity;

namespace LazyProxy.Sample
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("--- LazyProxyBuilder example ---");
            LazyProxyBuilderExample();

            Console.WriteLine("--- UnityExtension example #1 ---");
            UnityExtensionExample1();

            Console.WriteLine("--- UnityExtension example #2 ---");
            UnityExtensionExample2();

            Console.WriteLine("Saving the proxy type to a file...");
            LazyProxyBuilder.SaveDynamicAssembly();
        }

        private static void LazyProxyBuilderExample()
        {
            var proxy = LazyProxyBuilder.CreateLazyProxyInstance<IMyService>(() =>
            {
                Console.WriteLine("The real instance creation...");
                return new MyService();
            });

            Console.WriteLine("Foo execution...");
            proxy.Foo();
        }

        private static void UnityExtensionExample1()
        {
            var container = new UnityContainer()
                .RegisterLazy<IMyService, MyService>();

            Console.WriteLine("Resolving service...");
            var service = container.Resolve<IMyService>();

            Console.WriteLine("Foo execution...");
            service.Foo();
        }

        private static void UnityExtensionExample2()
        {
            var container = new UnityContainer()
                .RegisterLazy<IWeaponService, WeaponService>()
                .RegisterLazy<INinjaService, NinjaService>();

            Console.WriteLine("Resolving INinjaService...");
            var service = container.Resolve<INinjaService>();
            Console.WriteLine($"Type of 'ninjaService' is {service.GetType()}");

            Console.WriteLine("Invoking property getter 'service.MinNinjaHealth' ...");
            var minDamage = service.MinNinjaHealth;

            Console.WriteLine("Invoking method 'service.CreateNinja'...");
            var ninja = service.CreateNinja();

            Console.WriteLine("Invoking parent method 'service.GetDamage'...");
            var damage = service.GetDamage(ninja);

            try
            {
                Console.WriteLine("Invoking parent method 'service.GetDamage' with not correct argument...");
                service.GetDamage(null);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine($"Expected exception is thrown: {e.Message}");
            }
        }
    }
}