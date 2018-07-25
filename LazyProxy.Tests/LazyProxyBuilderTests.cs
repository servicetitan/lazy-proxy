using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace LazyProxy.Core.Tests
{
    public class LazyProxyBuilderTests
    {
        public class TestArgument { }

        // ReSharper disable once MemberCanBePrivate.Global
        public class TestArgument2 { }

        // ReSharper disable once MemberCanBePrivate.Global
        public class TestArgument3 { }

        private class TestException : Exception { }

        public interface IParentTestService
        {
            int ParentProperty { get; set; }

            string ParentMethod(bool arg);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public interface ITestService : IParentTestService
        {
            TestArgument Property { get; set; }
            string this[int i] { get; set; }

            void VoidMethod();
            Task<string> MethodAsync(string arg);
            string Method(string arg1, int arg2, TestArgument arg3);
            string MethodWithDefaultValue(string arg = "arg");
            string MethodWithOutValue(out string arg);
            string MethodWithRefValue(ref TestArgument arg);
            string GenericMethod<T1, T2>(string arg);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public interface IGenericTestService<T, in TIn, out TOut>
        {
            TOut Method1(T arg1, TIn arg2);
            T Method2(T arg1, TIn arg2);
        }

        [Fact]
        public void ProxyMustImplementInterface()
        {
            var proxyType = LazyProxyBuilder.BuildLazyProxyType<ITestService>();

            Assert.True(proxyType.GetInterfaces().Contains(typeof(ITestService)));
        }

        [Fact]
        public void SameTypeMustBeReturnedInCaseDoubleRegistration()
        {
            var proxyType1 = LazyProxyBuilder.BuildLazyProxyType<ITestService>();
            var proxyType2 = LazyProxyBuilder.BuildLazyProxyType<ITestService>();

            Assert.Equal(proxyType1, proxyType2);
        }

        [Fact]
        public void ServiceCtorMustBeExecutedAfterMethodIsCalledAndOnlyOnce()
        {
            var constructorCounter = 0;
            var proxy = LazyProxyBuilder.CreateLazyProxyInstance(() =>
            {
                constructorCounter++;
                return Mock.Of<ITestService>();
            });

            Assert.Equal(0, constructorCounter);

            proxy.VoidMethod();

            Assert.Equal(1, constructorCounter);

            proxy.VoidMethod();

            Assert.Equal(1, constructorCounter);
        }

        [Fact]
        public void MethodsMustBeProxied()
        {
            const string arg1 = "test";
            const int arg2 = 7;
            var arg3 = new TestArgument();
            const bool arg4 = true;
            const string result1 = "result1";
            const string result2 = "result2";

            var proxy = LazyProxyBuilder.CreateLazyProxyInstance(() =>
            {
                var mock = new Mock<ITestService>(MockBehavior.Strict);

                mock.Setup(s => s.Method(arg1, arg2, arg3)).Returns(result1);
                mock.Setup(s => s.ParentMethod(arg4)).Returns(result2);

                return mock.Object;
            });

            Assert.Equal(result1, proxy.Method(arg1, arg2, arg3));
            Assert.Equal(result2, proxy.ParentMethod(arg4));
        }

        [Fact]
        public async Task AsyncMethodsMustBeProxied()
        {
            const string arg = "arg";
            const string result = "result";

            var proxy = LazyProxyBuilder.CreateLazyProxyInstance(() =>
            {
                var mock = new Mock<ITestService>(MockBehavior.Strict);

                mock.Setup(s => s.MethodAsync(arg)).ReturnsAsync(result);

                return mock.Object;
            });

            var actualResult = await proxy.MethodAsync(arg);

            Assert.Equal(result, actualResult);
        }

        [Fact]
        public void MethodsWithDefaultValuesMustBeProxied()
        {
            const string defaultArg = "arg";
            const string result = "result";

            var proxy = LazyProxyBuilder.CreateLazyProxyInstance(() =>
            {
                var mock = new Mock<ITestService>(MockBehavior.Strict);

                mock.Setup(s => s.MethodWithDefaultValue(defaultArg)).Returns(result);

                return mock.Object;
            });

            var actualResult = proxy.MethodWithDefaultValue();

            Assert.Equal(result, actualResult);
        }

        [Fact]
        public void MethodsWithOutValuesMustBeProxied()
        {
            var expectedOutArg = "arg";
            const string expectedResult = "result";

            var proxy = LazyProxyBuilder.CreateLazyProxyInstance(() =>
            {
                var mock = new Mock<ITestService>(MockBehavior.Strict);

                mock.Setup(s => s.MethodWithOutValue(out expectedOutArg)).Returns(expectedResult);

                return mock.Object;
            });

            var actualResult = proxy.MethodWithOutValue(out var actualOutArg);

            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(expectedOutArg, actualOutArg);
        }

        [Fact]
        public void MethodsWithRefValuesMustBeProxied()
        {
            var refArg = new TestArgument();
            const string expectedResult = "result";

            var proxy = LazyProxyBuilder.CreateLazyProxyInstance(() =>
            {
                var mock = new Mock<ITestService>(MockBehavior.Strict);

                // ReSharper disable once AccessToModifiedClosure
                mock.Setup(s => s.MethodWithRefValue(ref refArg)).Returns(expectedResult);

                return mock.Object;
            });

            var actualResult = proxy.MethodWithRefValue(ref refArg);

            Assert.Equal(expectedResult, actualResult);
        }

        [Fact]
        public void GenericMethodsMustBeProxied()
        {
            const string arg = "arg";
            const string expectedResult = "result";

            var proxy = LazyProxyBuilder.CreateLazyProxyInstance(() =>
            {
                var mock = new Mock<ITestService>(MockBehavior.Strict);

                mock.Setup(s => s.GenericMethod<TestArgument, TestException>(arg)).Returns(expectedResult);

                return mock.Object;
            });

            var actualResult = proxy.GenericMethod<TestArgument, TestException>(arg);

            Assert.Equal(expectedResult, actualResult);
        }

        [Fact]
        public void PropertyGettersMustBeProxied()
        {
            var result1 = new TestArgument();
            const int result2 = 3;

            var proxy = LazyProxyBuilder.CreateLazyProxyInstance(() =>
            {
                var mock = new Mock<ITestService>(MockBehavior.Strict);

                mock.Setup(s => s.Property).Returns(result1);
                mock.Setup(s => s.ParentProperty).Returns(result2);

                return mock.Object;
            });

            Assert.Equal(result1, proxy.Property);
            Assert.Equal(result2, proxy.ParentProperty);
        }

        [Fact]
        public void PropertySettersMustBeProxied()
        {
            var value1 = new TestArgument();
            const int value2 = 3;

            Mock<ITestService> mock = null;

            var proxy = LazyProxyBuilder.CreateLazyProxyInstance(() =>
            {
                mock = new Mock<ITestService>(MockBehavior.Strict);

                mock.SetupSet(s => s.Property = value1);
                mock.SetupSet(s => s.ParentProperty = value2);

                return mock.Object;
            });

            proxy.Property = value1;
            proxy.ParentProperty = value2;

            mock.VerifySet(s => s.Property = value1);
            mock.VerifySet(s => s.ParentProperty = value2);
        }

        [Fact]
        public void IndexerGettersMustBeProxied()
        {
            const int arg = 3;
            const string result = "result";

            var proxy = LazyProxyBuilder.CreateLazyProxyInstance(() =>
            {
                var mock = new Mock<ITestService>(MockBehavior.Strict);

                mock.Setup(s => s[arg]).Returns(result);

                return mock.Object;
            });

            Assert.Equal(result, proxy[arg]);
        }

        [Fact]
        public void IndexerSettersMustBeProxied()
        {
            const int arg = 3;
            const string result = "result";
            Mock<ITestService> mock = null;

            var proxy = LazyProxyBuilder.CreateLazyProxyInstance(() =>
            {
                mock = new Mock<ITestService>(MockBehavior.Strict);

                mock.SetupSet(s => s[arg] = result);

                return mock.Object;
            });

            proxy[arg] = result;

            mock.VerifySet(s => s[arg] = result);
        }

        [Fact]
        public void ExceptionsFromServiceMustBeThrown()
        {
            const bool arg = true;

            var proxy = LazyProxyBuilder.CreateLazyProxyInstance(() =>
            {
                var mock = new Mock<ITestService>(MockBehavior.Strict);

                mock.Setup(s => s.ParentMethod(arg)).Throws<TestException>();

                return mock.Object;
            });

            Assert.Throws<TestException>(() => proxy.ParentMethod(arg));
        }

        [Fact]
        public void GenericInterfacesMustBeProxied()
        {
            var arg1 = new TestArgument();
            var arg2 = new TestArgument2();
            var expectedResult1 = new TestArgument3();
            var expectedResult2 = new TestArgument();

            var proxy = LazyProxyBuilder.CreateLazyProxyInstance(() =>
            {
                var mock = new Mock<IGenericTestService<TestArgument, TestArgument2, TestArgument3>>(MockBehavior.Strict);

                mock.Setup(s => s.Method1(arg1, arg2)).Returns(expectedResult1);
                mock.Setup(s => s.Method2(arg1, arg2)).Returns(expectedResult2);

                return mock.Object;
            });

            var actualResult1 = proxy.Method1(arg1, arg2);
            var actualResult2 = proxy.Method2(arg1, arg2);

            Assert.Equal(expectedResult1, actualResult1);
            Assert.Equal(expectedResult2, actualResult2);
        }

        [Fact]
        public void GenericInterfaceWithDifferentTypeParametersMustBeCreatedWithoutExceptions()
        {
            var exception = Record.Exception(() =>
            {
                LazyProxyBuilder.BuildLazyProxyType<IGenericTestService<TestArgument, TestArgument2, TestArgument3>>();
                LazyProxyBuilder.BuildLazyProxyType<IGenericTestService<TestArgument3, TestArgument, TestArgument2>>();
            });

            Assert.Null(exception);
        }
    }
}