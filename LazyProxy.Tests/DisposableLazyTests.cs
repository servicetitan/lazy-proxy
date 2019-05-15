using System;
using Moq;
using Xunit;

namespace LazyProxy.Tests
{
    public class DisposableLazyTests
    {
        public interface IHaveDisposeBusinessMethod
        {
            void Dispose();
        }

        public interface IImplementIDisposable: IDisposable
        {
            void DoSomething();
        }

        [Fact]
        public void BusinessDisposeMethodIsCalled()
        {
            var mock = new Mock<IHaveDisposeBusinessMethod>(MockBehavior.Strict);
            mock.Setup(s => s.Dispose()).Verifiable();

            var proxy = LazyProxyBuilder.CreateInstance(() => mock.Object);

            proxy.Dispose();

            mock.Verify(s => s.Dispose());
        }

        [Fact]
        public void RegularDisposeMethodIsCalledIfInstanceIsCreated()
        {
            var mock = new Mock<IImplementIDisposable>(MockBehavior.Strict);
            mock.Setup(s => s.DoSomething());
            mock.Setup(s => s.Dispose()).Verifiable();

            var proxy = LazyProxyBuilder.CreateInstance(() => mock.Object);

            proxy.DoSomething();
            proxy.Dispose();

            mock.Verify(s => s.Dispose());
        }

        [Fact]
        public void RegularDisposeMethodIsNotCalledIfNoInstanceIsCreated()
        {
            var mock = new Mock<IImplementIDisposable>(MockBehavior.Strict);
            var callCounter = 0;
            mock.Setup(s => s.Dispose()).Callback(() => callCounter++);

            var proxy = LazyProxyBuilder.CreateInstance(() => mock.Object);

            proxy.Dispose();

            Assert.Equal(0, callCounter);
        }
    }
}