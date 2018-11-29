using System;

namespace LazyProxy
{
    /// <summary>
    /// Base class for lazy proxies.
    /// </summary>
    public abstract class LazyProxyBase
    {
        /// <summary>
        /// Initializes inner <see cref="Lazy{T}"/> instance with the valueFactory provided.
        /// </summary>
        /// <param name="valueFactory">Function that returns a value.</param>
        public abstract void Initialize(Func<object> valueFactory);
    }
}