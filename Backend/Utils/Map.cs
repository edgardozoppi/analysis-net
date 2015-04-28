using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	public class Map<TKey, TValue, TCollection> : Dictionary<TKey, TCollection>
		where TCollection : ICollection<TValue>, new()
	{
		public void Add(TKey key)
		{
			this.AddKeyAndGetValues(key);
		}

		public void Add(TKey key, TValue value)
		{
			var collection = this.AddKeyAndGetValues(key);
			collection.Add(value);
		}

		public void AddRange(TKey key, IEnumerable<TValue> values)
		{
			var collection = this.AddKeyAndGetValues(key);
			collection.AddRange(values);
		}

		private TCollection AddKeyAndGetValues(TKey key)
		{
			TCollection result;

			if (this.ContainsKey(key))
			{
				result = this[key];
			}
			else
			{
				result = new TCollection();
				this.Add(key, result);
			}

			return result;
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as Map<TKey, TValue, TCollection>;
			return this.Equals(other);
		}

		public override int GetHashCode()
		{
			var hashcode = 0;

			foreach (var key in this.Keys)
			{
				hashcode ^= key.GetHashCode();
			}

			return hashcode;
		}

		protected virtual bool Equals(Map<TKey, TValue, TCollection> other)
		{
			return IDictionaryEqualityComparer<TKey, TCollection>.Instance.Equals(this, other);
		}
	}

	public class MapSet<TKey, TValue> : Map<TKey, TValue, HashSet<TValue>>
	{
		protected override bool Equals(Map<TKey, TValue, HashSet<TValue>> other)
		{
			return IDictionaryEqualityComparer<TKey, HashSet<TValue>>.Instance.Equals(this, other, this.EqualsCollection);
		}

		private bool EqualsCollection(HashSet<TValue> self, HashSet<TValue> other)
		{
			return self.SetEquals(other);
		}
	}

	public class MapList<TKey, TValue> : Map<TKey, TValue, List<TValue>>
	{
		protected override bool Equals(Map<TKey, TValue, List<TValue>> other)
		{
			return IDictionaryEqualityComparer<TKey, List<TValue>>.Instance.Equals(this, other, this.EqualsCollection);
		}

		private bool EqualsCollection(List<TValue> self, List<TValue> other)
		{
			if (self.Count != other.Count) return false;

			for (var i = 0; i < self.Count; ++i)
			{
				var a = self[i];
				var b = other[i];

				if (!a.Equals(b)) return false;
			}

			return true;
		}
	}
}
