using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SharedResources
{
    public class LinqCompare<T> : IEqualityComparer<T>
    {
        private Func<T, T, bool> _comparer { get; set; }
        private Func<T, int> _getHash { get; set; }
        public LinqCompare(Func<T, T, bool> comparer, Func<T, int> getHashCode = null)
        {
            _comparer = comparer;
            _getHash = getHashCode;
        }

        public bool Equals(T x, T y) => _comparer.Invoke(x, y);

        public int GetHashCode(T obj) => _getHash != null ? unchecked(_getHash.Invoke(obj)) : base.GetHashCode();
    }
}
