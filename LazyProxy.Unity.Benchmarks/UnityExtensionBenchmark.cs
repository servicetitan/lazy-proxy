using System;
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
    [Config(typeof(Config))]
    public class UnityExtensionBenchmark
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Default.With(RunStrategy.ColdStart).With(Runtime.Clr));
                Add(Job.Default.With(RunStrategy.ColdStart).With(Runtime.Core));
                Add(DefaultConfig.Instance.GetColumnProviders().ToArray());
            }
        }

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
        public int InvokeMethod()
        {
            return _service.Method();
        }

        [GlobalSetup(Target = nameof(InvokeLazyMethodFirstTime))]
        public void GlobalSetupForInvokeLazyMethodFirstTime()
        {
            _container = BuildContainerWithLazyProxy();
            _service = _container.Resolve<ITestService>();
        }

        [Benchmark]
        public int InvokeLazyMethodFirstTime()
        {
            return _service.Method();
        }

        [GlobalSetup(Target = nameof(InvokeLazyMethodSecondTime))]
        public void GlobalSetupForInvokeLazyMethodSecondTime()
        {
            _container = BuildContainerWithLazyProxy();
            _service = _container.Resolve<ITestService>();
            _service.Method();
        }

        [Benchmark]
        public int InvokeLazyMethodSecondTime()
        {
            return _service.Method();
        }

        private static IUnityContainer BuildContainerWithoutLazyProxy() =>
            new UnityContainer().RegisterType<ITestService, TestService>();

        private static IUnityContainer BuildContainerWithLazyProxy() =>
            new UnityContainer().RegisterLazy<ITestService, TestService>();
    }

    public interface ITestService
    {
        int Method();
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class TestService : ITestService
    {
        public TestService()
        {
            // Emulate some hard work (e.g. other services resolving).
            Thread.Sleep(1);
        }

        public int Method() => Thread.CurrentThread.ManagedThreadId;
    }
}