using System;
using System.Collections.Generic;
using System.Linq;

namespace Nito.Disposables.Internals
{
    /// <summary>
    /// Extension methods for enumerables.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns a sequence with the `null` instances removed.
        /// </summary>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source) where T : class
        {
            return source.Where(x => x != null);
        }

        public static Queue<T> Enqueue<T>(this Queue<T> queue, T disposable)
        {
            queue.Enqueue(disposable);
            return queue;
        }
    }
}