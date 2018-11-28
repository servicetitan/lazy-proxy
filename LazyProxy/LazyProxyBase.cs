using System;

namespace LazyProxy
{
    /// <summary>
    /// Base class for lazy proxies.
    /// </summary>
    public abstract class LazyProxyBase
    {
        /// <summary>
        /// Initialize inner service of <see cref="Lazy{T}"/> by value factory.
        /// </summary>
        /// <param name="valueFactory">Function that returns a value.</param>
        public abstract void Initialize(Func<object> valueFactory);
    }
}