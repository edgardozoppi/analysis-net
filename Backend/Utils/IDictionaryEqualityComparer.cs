using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace Backend.Utils
{
	public sealed class IDictionaryEqualityComparer<K, V> : IEqualityComparer<IDictionary<K,V>>
	{
		private static readonly IDictionaryEqualityComparer<K, V> instance = new IDictionaryEqualityComparer<K, V>();

		public static IDictionaryEqualityComparer<K, V> Instance
		{
			get { return instance; }
		}

		public bool Equals(IDictionary<K, V> x, IDictionary<K, V> y)
		{
			return this.Equals(x, y, (a, b) => a.Equals(b));
		}

		public bool Equals(IDictionary<K, V> x, IDictionary<K, V> y, Func<V, V, bool> equalsValues)
		{
			if (object.ReferenceEquals(x, y)) return true;
			if (x.Count != y.Count) return false;

			foreach (var key in x.Keys)
			{
				var otherContainsKey = y.ContainsKey(key);
				if (!otherContainsKey) return false;
			}

			foreach (var entry in x)
			{
				var value = y[entry.Key];
				var valuesAreEquals = equalsValues(entry.Value, value);

				if (!valuesAreEquals) return false;
			}

			return true;
		}

		public int GetHashCode(IDictionary<K, V> dic)
		{
			var hashcode = 0;

			foreach (var key in dic.Keys)
			{
				hashcode ^= key.GetHashCode();
			}

			return hashcode;
		}
	}
}
