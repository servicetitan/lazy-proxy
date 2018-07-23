using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using Unity;

namespace LazyProxy.Unity.Benchmarks
{
    [Config(typeof(BenchmarkConfig))]
    public class UnityExtensionBenchmark
    {
        private class BenchmarkConfig : ManualConfig
        {
            public BenchmarkConfig()
            {
                Add(Job.Default.With(RunStrategy.ColdStart)
                    .With(Runtime.Clr)
                    .WithTargetCount(1)
                    .WithInvocationCount(1)
                    .WithLaunchCount(10)
                );

                Add(Job.Default.With(RunStrategy.ColdStart)
                    .With(Runtime.Core)
                    .WithTargetCount(1)
                    .WithInvocationCount(1)
                    .WithLaunchCount(10)
                );

                Add(DefaultConfig.Instance.GetColumnProviders().ToArray());
            }
        }

        private IUnityContainer _container;
        private ITestService _service;

        [GlobalSetup(Target = nameof(RegisterType) + "," + nameof(RegisterLazy))]
        public void GlobalSetupForRegistrationBenchmarks()
        {
            _container = new UnityContainer();
        }

        [Benchmark]
        public IUnityContainer RegisterType()
        {
            return _container.RegisterType<ITestService, TestService>();
        }

        [Benchmark]
        public IUnityContainer RegisterLazy()
        {
            return _container.RegisterLazy<ITestService, TestService>();
        }

        [GlobalSetup(Target = nameof(ColdResolveType))]
        public void GlobalSetupForColdResolveType()
        {
            _container = new UnityContainer().RegisterType<ITestService, TestService>();
        }

        [Benchmark]
        public ITestService ColdResolveType()
        {
            return _container.Resolve<ITestService>();
        }

        [GlobalSetup(Target = nameof(ColdResolveLazy))]
        public void GlobalSetupForColdResolveLazy()
        {
            _container = new UnityContainer().RegisterLazy<ITestService, TestService>();
        }

        [Benchmark]
        public ITestService ColdResolveLazy()
        {
            return _container.Resolve<ITestService>();
        }

        [GlobalSetup(Target = nameof(HotResolveType))]
        public void GlobalSetupForHotResolveType()
        {
            _container = new UnityContainer().RegisterType<ITestService, TestService>();
            _container.Resolve<ITestService>();
        }

        [Benchmark]
        public ITestService HotResolveType()
        {
            return _container.Resolve<ITestService>();
        }

        [GlobalSetup(Target = nameof(HotResolveLazy))]
        public void GlobalSetupForHotResolveLazy()
        {
            _container = new UnityContainer().RegisterLazy<ITestService, TestService>();
            _container.Resolve<ITestService>();
        }

        [Benchmark]
        public ITestService HotResolveLazy()
        {
            return _container.Resolve<ITestService>();
        }

        [GlobalSetup(Target = nameof(InvokeMethod))]
        public void GlobalSetupForInvokeMethod()
        {
            _service = new UnityContainer()
                .RegisterType<ITestService, TestService>()
                .Resolve<ITestService>();
        }

        [Benchmark]
        public int InvokeMethod()
        {
            return _service.Method();
        }

        [GlobalSetup(Target = nameof(InvokeLazyMethodFirstTime))]
        public void GlobalSetupForInvokeLazyMethodFirstTime()
        {
            _service = new UnityContainer()
                .RegisterLazy<ITestService, TestService>()
                .Resolve<ITestService>();
        }

        [Benchmark]
        public int InvokeLazyMethodFirstTime()
        {
            return _service.Method();
        }

        [GlobalSetup(Target = nameof(InvokeLazyMethodSecondTime))]
        public void GlobalSetupForInvokeLazyMethodSecondTime()
        {
            _service = new UnityContainer()
                .RegisterLazy<ITestService, TestService>()
                .Resolve<ITestService>();

            _service.Method();
        }

        [Benchmark]
        public int InvokeLazyMethodSecondTime()
        {
            return _service.Method();
        }
    }

    public interface ITestService
    {
        int Method();
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class TestService : ITestService
    {
        private readonly int _threadId;

        public TestService()
        {
            // Emulate some hard work (e.g. other services resolving).
            Thread.Sleep(10);
            _threadId = Thread.CurrentThread.ManagedThreadId;
        }

        public int Method() => _threadId;
    }
}