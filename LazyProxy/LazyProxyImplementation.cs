using System;

namespace LazyProxy
{
    /// <summary>
    /// This type hosts methods being used by lazy proxy implementations.
    /// </summary>
    public static class LazyProxyImplementation
    {
        /// <summary>
        /// Creates an instance of <see cref="Lazy{T}"/>.
        /// </summary>
        /// <param name="valueFactory">Function that returns a value.</param>
        /// <typeparam name="T">Type of lazy value.</typeparam>
        /// <returns>Instance of <see cref="Lazy{T}"/></returns>
        public static Lazy<T> CreateInstance<T>(Func<object> valueFactory)
        {
            return new Lazy<T>(() => (T) valueFactory());
        }

        /// <summary>
        /// Disposes an instance owned by <see cref="Lazy{T}"/> if any.
        /// </summary>
        /// <param name="instanceOwner"><see cref="Lazy{T}"/> object .</param>
        /// <typeparam name="T">Type of lazy value. It must implement <see cref="IDisposable"/> interface.</typeparam>
        public static void DisposeInstance<T>(Lazy<T> instanceOwner)
            where T: IDisposable
        {
            if (instanceOwner.IsValueCreated)
            {
                instanceOwner.Value.Dispose();
            }
        }
    }
}