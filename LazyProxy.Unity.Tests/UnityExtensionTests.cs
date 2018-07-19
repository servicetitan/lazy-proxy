using System;
using Moq;
using NUnit.Framework;
using Unity;
using Unity.Exceptions;
using Unity.Injection;
using Unity.Lifetime;

namespace LazyProxy.Unity.Tests
{
    public class UnityExtensionTests
    {
        [ThreadStatic]
        private static string _service1Id;

        [ThreadStatic]
        private static string _service2Id;

        // ReSharper disable once MemberCanBePrivate.Global
        public interface IService1
        {
            string Property { get; set; }
            string MethodWithoutOtherServiceInvocation();
            string MethodWithOtherServiceInvocation(string arg);
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class Service1 : IService1
        {
            private readonly IService2 _otherService;

            public Service1(IService2 otherService)
            {
                _service1Id = Guid.NewGuid().ToString();
                _otherService = otherService;
            }

            public string Property { get; set; } = "property";
            public string MethodWithoutOtherServiceInvocation() => "service1";
            public string MethodWithOtherServiceInvocation(string arg) => "service1->" + _otherService.Method(arg);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public interface IService2
        {
            string Method(string arg);
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class Service2 : IService2
        {
            public Service2()
            {
                _service2Id = Guid.NewGuid().ToString();
            }

            public string Method(string arg) => "service2->" + arg;
        }

        [Test]
        public void ServiceCtorMustBeExecutedAfterMethodIsCalledAndOnlyOnce()
        {
            _service1Id = string.Empty;
            _service2Id = string.Empty;

            var service = new UnityContainer()
                .RegisterLazy<IService1, Service1>()
                .RegisterType<IService2, Service2>()
                .Resolve<IService1>();

            Assert.IsEmpty(_service1Id);
            Assert.IsEmpty(_service2Id);

            var result1 = service.MethodWithoutOtherServiceInvocation();

            Assert.AreEqual("service1", result1);
            Assert.IsNotEmpty(_service1Id);
            Assert.IsNotEmpty(_service2Id);

            var prevService1Id = _service1Id;
            var prevService2Id = _service2Id;

            var result2 = service.MethodWithoutOtherServiceInvocation();

            Assert.AreEqual("service1", result2);
            Assert.AreEqual(prevService1Id, _service1Id);
            Assert.AreEqual(prevService2Id, _service2Id);
        }

        [Test]
        public void ServiceCtorMustBeExecutedAfterPropertyGetterIsCalled()
        {
            _service1Id = string.Empty;
            _service2Id = string.Empty;

            var service = new UnityContainer()
                .RegisterLazy<IService1, Service1>()
                .RegisterType<IService2, Service2>()
                .Resolve<IService1>();

            Assert.IsEmpty(_service1Id);
            Assert.IsEmpty(_service2Id);

            var result = service.Property;

            Assert.AreEqual("property", result);
            Assert.IsNotEmpty(_service1Id);
            Assert.IsNotEmpty(_service2Id);
        }

        [Test]
        public void ServiceCtorMustBeExecutedAfterPropertySetterIsCalled()
        {
            _service1Id = string.Empty;
            _service2Id = string.Empty;

            var service = new UnityContainer()
                .RegisterLazy<IService1, Service1>()
                .RegisterType<IService2, Service2>()
                .Resolve<IService1>();

            Assert.IsEmpty(_service1Id);
            Assert.IsEmpty(_service2Id);

            service.Property = "newProperty";

            Assert.IsNotEmpty(_service1Id);
            Assert.IsNotEmpty(_service2Id);
        }

        [Test]
        public void ServiceCtorMustBeExecutedAfterMethodIsCalledForAllNestedLazyTypes()
        {
            _service1Id = string.Empty;
            _service2Id = string.Empty;

            var service = new UnityContainer()
                .RegisterLazy<IService1, Service1>()
                .RegisterLazy<IService2, Service2>()
                .Resolve<IService1>();

            Assert.IsEmpty(_service1Id);
            Assert.IsEmpty(_service2Id);

            var result1 = service.MethodWithoutOtherServiceInvocation();

            Assert.AreEqual("service1", result1);
            Assert.IsNotEmpty(_service1Id);
            Assert.IsEmpty(_service2Id);

            var result2 = service.MethodWithOtherServiceInvocation("test");
            Assert.AreEqual("service1->service2->test", result2);
            Assert.IsNotEmpty(_service1Id);
            Assert.IsNotEmpty(_service2Id);
        }

        [Test]
        public void LifetimeMustBeCorrect()
        {
            _service1Id = string.Empty;
            _service2Id = string.Empty;

            var container = new UnityContainer()
                .RegisterLazy<IService1, Service1>(() => new SingletonLifetimeManager())
                .RegisterLazy<IService2, Service2>(() => new SingletonLifetimeManager());

            Assert.IsEmpty(_service1Id);
            Assert.IsEmpty(_service2Id);

            container.Resolve<IService1>().MethodWithOtherServiceInvocation("test1");

            Assert.IsNotEmpty(_service1Id);
            Assert.IsNotEmpty(_service2Id);

            var prevService1Id = _service1Id;
            var prevService2Id = _service2Id;

            container.Resolve<IService1>().MethodWithOtherServiceInvocation("test2");

            Assert.AreEqual(prevService1Id, _service1Id);
            Assert.AreEqual(prevService2Id, _service2Id);
        }

        [Test]
        public void InjectionMembersMustBeCorrect()
        {
            const string arg = "test";
            const string result = "result";
            var service2Mock = new Mock<IService2>(MockBehavior.Strict);

            service2Mock.Setup(s => s.Method(arg)).Returns(result);

            var container = new UnityContainer()
                .RegisterLazy<IService1, Service1>(
                    new InjectionConstructor(service2Mock.Object));

            var actualResult = container.Resolve<IService1>().MethodWithOtherServiceInvocation(arg);

            Assert.AreEqual("service1->" + result, actualResult);
        }

        [Test]
        public void ServicesMustBeResolvedFromChildContainer()
        {
            var container = new UnityContainer()
                .RegisterLazy<IService1, Service1>();

            Assert.Throws<ResolutionFailedException>(() =>
            {
                var service = container.Resolve<IService1>();
                service.MethodWithoutOtherServiceInvocation();
            });

            var childContainer = container.CreateChildContainer()
                .RegisterType<IService2, Service2>();

            Assert.DoesNotThrow(() =>
            {
                var service = childContainer.Resolve<IService1>();
                service.MethodWithoutOtherServiceInvocation();
            });
        }
    }
}