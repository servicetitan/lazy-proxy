using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using NUnit.Framework;
using Unity;

namespace LazyProxy.Unity.Tests
{
    [SimpleJob(RunStrategy.ColdStart)]
    public class UnityExtensionBenchmark
    {
        private IUnityContainer _container;
        private ITestService _service;

        [Benchmark]
        public IUnityContainer RegisterType()
        {
            return new UnityContainer().RegisterType<ITestService, TestService>();
        }

        [Benchmark]
        public IUnityContainer RegisterLazy()
        {
            return new UnityContainer().RegisterLazy<ITestService, TestService>();
        }

        [GlobalSetup(Target = nameof(ResolveType))]
        public void GlobalSetupForResolveType()
        {
            _container = BuildContainerWithoutLazyProxy();
        }

        [Benchmark]
        public ITestService ResolveType()
        {
            return _container.Resolve<ITestService>();
        }

        [GlobalSetup(Target = nameof(ResolveLazy))]
        public void GlobalSetupForResolveLazy()
        {
            _container = BuildContainerWithLazyProxy();
        }

        [Benchmark]
        public ITestService ResolveLazy()
        {
            return _container.Resolve<ITestService>();
        }

        [GlobalSetup(Target = nameof(InvokeMethod))]
        public void GlobalSetupForInvokeMethod()
        {
            _container = BuildContainerWithoutLazyProxy();
            _service = _container.Resolve<ITestService>();
        }

        [Benchmark]
        public void InvokeMethod()
        {
            _service.Method1();
        }

        [GlobalSetup(Target = nameof(InvokeLazyMethodFirstTime))]
        public void GlobalSetupForInvokeLazyMethodFirstTime()
        {
            _container = BuildContainerWithLazyProxy();
            _service = _container.Resolve<ITestService>();
        }

        [Benchmark]
        public void InvokeLazyMethodFirstTime()
        {
            _service.Method1();
        }

        [GlobalSetup(Target = nameof(InvokeLazyMethodSecondTime))]
        public void GlobalSetupForInvokeLazyMethodSecondTime()
        {
            _container = BuildContainerWithLazyProxy();
            _service = _container.Resolve<ITestService>();
            _service.Method1();
        }

        [Benchmark]
        public void InvokeLazyMethodSecondTime()
        {
            _service.Method1();
        }

        private static IUnityContainer BuildContainerWithoutLazyProxy() =>
            new UnityContainer()
                .RegisterType<ITestService, TestService>()
                .RegisterType<IInnerService1, InnerService1>()
                .RegisterType<IInnerService2, InnerService2>()
                .RegisterType<IInnerService3, InnerService3>()
                .RegisterType<IInnerService4, InnerService4>()
                .RegisterType<IInnerService5, InnerService5>()
                .RegisterType<IInnerService6, InnerService6>()
                .RegisterType<IInnerService7, InnerService7>()
                .RegisterType<IInnerService8, InnerService8>()
                .RegisterType<IInnerService9, InnerService9>()
                .RegisterType<IInnerService10, InnerService10>();

        private static IUnityContainer BuildContainerWithLazyProxy() =>
            new UnityContainer()
                .RegisterLazy<ITestService, TestService>()
                .RegisterType<IInnerService1, InnerService1>()
                .RegisterType<IInnerService2, InnerService2>()
                .RegisterType<IInnerService3, InnerService3>()
                .RegisterType<IInnerService4, InnerService4>()
                .RegisterType<IInnerService5, InnerService5>()
                .RegisterType<IInnerService6, InnerService6>()
                .RegisterType<IInnerService7, InnerService7>()
                .RegisterType<IInnerService8, InnerService8>()
                .RegisterType<IInnerService9, InnerService9>()
                .RegisterType<IInnerService10, InnerService10>();
    }

    public class UnityExtensionBenchmarkRunner
    {
        [Test, Explicit]
        public void Run() => BenchmarkRunner.Run<UnityExtensionBenchmark>();
    }

    public interface ITestService
    {
        void Method1();
        void Method2();
        void Method3();
        void Method4();
        void Method5();
        void Method6();
        void Method7();
        void Method8();
        void Method9();
        void Method10();
    }

    public class TestService : ITestService
    {
        private readonly IInnerService1 _service1;
        private readonly IInnerService1 _service2;
        private readonly IInnerService1 _service3;
        private readonly IInnerService1 _service4;
        private readonly IInnerService1 _service5;
        private readonly IInnerService1 _service6;
        private readonly IInnerService1 _service7;
        private readonly IInnerService1 _service8;
        private readonly IInnerService1 _service9;
        private readonly IInnerService1 _service10;

        public TestService(
            IInnerService1 service1,
            IInnerService1 service2,
            IInnerService1 service3,
            IInnerService1 service4,
            IInnerService1 service5,
            IInnerService1 service6,
            IInnerService1 service7,
            IInnerService1 service8,
            IInnerService1 service9,
            IInnerService1 service10)
        {
            _service1 = service1;
            _service2 = service2;
            _service3 = service3;
            _service4 = service4;
            _service5 = service5;
            _service6 = service6;
            _service7 = service7;
            _service8 = service8;
            _service9 = service9;
            _service10 = service10;
        }

        public void Method1() { }
        public void Method2() { }
        public void Method3() { }
        public void Method4() { }
        public void Method5() { }
        public void Method6() { }
        public void Method7() { }
        public void Method8() { }
        public void Method9() { }
        public void Method10() { }
    }

    public interface IInnerService1 { }
    public class InnerService1 : IInnerService1 { }

    public interface IInnerService2 { }
    public class InnerService2 : IInnerService2 { }

    public interface IInnerService3 { }
    public class InnerService3 : IInnerService3 { }

    public interface IInnerService4 { }
    public class InnerService4 : IInnerService4 { }

    public interface IInnerService5 { }
    public class InnerService5 : IInnerService5 { }

    public interface IInnerService6 { }
    public class InnerService6 : IInnerService6 { }

    public interface IInnerService7 { }
    public class InnerService7 : IInnerService7 { }

    public interface IInnerService8 { }
    public class InnerService8 : IInnerService8 { }

    public interface IInnerService9 { }
    public class InnerService9 : IInnerService9 { }

    public interface IInnerService10 { }
    public class InnerService10 : IInnerService10 { }
}