using System;

namespace LazyProxy
{
    /// <summary>
    /// This type hosts factory method that creates <see cref="Lazy{T}"/> instances.
    /// </summary>
    public static class LazyBuilder
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
    }
}