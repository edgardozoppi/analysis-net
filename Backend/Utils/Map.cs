// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.Model;
using Model;

namespace Backend.Utils
{
	public abstract class Map<TKey, TValue, TCollection> : Dictionary<TKey, TCollection>
		where TCollection : ICollection<TValue>, new()
	{
		protected Map()
		{
		}

		protected Map(IEnumerable<KeyValuePair<TKey, TCollection>> other)
		{
			this.UnionWith(other);
		}

		protected Map(IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> other)
		{
			this.UnionWith(other);
		}

		protected Map(IEnumerable<KeyValuePair<TKey, TValue>> other)
		{
			this.UnionWith(other);
		}

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

		public bool ContainsValue(TKey key, TValue value)
		{
			TCollection values;
			var result = this.TryGetValue(key, out values);
			result = result && values.Contains(value);
			return result;
		}

		public void UnionWith<TValues>(IEnumerable<KeyValuePair<TKey, TValues>> other)
			where TValues : IEnumerable<TValue>
		{
			foreach (var entry in other)
			{
				this.AddRange(entry.Key, entry.Value);
			}
		}

		public void UnionWith(IEnumerable<KeyValuePair<TKey, TValue>> other)
		{
			foreach (var entry in other)
			{
				this.Add(entry.Key, entry.Value);
			}
		}

		public void IntersectWith<TValues>(IEnumerable<KeyValuePair<TKey, TValues>> other)
			where TValues : IEnumerable<TValue>
		{
			var keys = new HashSet<TKey>();

			foreach (var entry in other)
			{
				TCollection value;

				if (this.TryGetValue(entry.Key, out value))
				{
					value = ValueIntersect(value, entry.Value);

					if (value != null)
					{
						this[entry.Key] = value;
						keys.Add(entry.Key);
					}
					else
					{
						this.Remove(entry.Key);
					}
				}
			}

			var keysToRemove = this.Keys.Except(keys).ToArray();

			foreach (var key in keysToRemove)
			{
				this.Remove(key);
			}
		}

		public bool MapEquals(Map<TKey, TValue, TCollection> other)
		{
			return this.DictionaryEquals(other, ValueEquals);
		}

		protected abstract bool ValueEquals(TCollection a, TCollection b);
		protected abstract TCollection ValueIntersect(TCollection a, IEnumerable<TValue> b);

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
	}

	public class MapSet<TKey, TValue> : Map<TKey, TValue, HashSet<TValue>>
	{
		public MapSet()
		{
		}

		protected MapSet(IEnumerable<KeyValuePair<TKey, HashSet<TValue>>> other)
			: base(other)
		{
		}

		public MapSet(IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> other)
			: base(other)
		{
		}

		public MapSet(IEnumerable<KeyValuePair<TKey, TValue>> other)
			: base(other)
		{
		}

		protected override bool ValueEquals(HashSet<TValue> a, HashSet<TValue> b)
		{
			return a.SetEquals(b);
		}

		protected override HashSet<TValue> ValueIntersect(HashSet<TValue> a, IEnumerable<TValue> b)
		{
			a.IntersectWith(b);
			var result = a;

			if (result.Count == 0)
			{
				result = null;
			}

			return result;
		}
	}

	public class MapList<TKey, TValue> : Map<TKey, TValue, List<TValue>>
	{
		public MapList()
		{
		}

		protected MapList(IEnumerable<KeyValuePair<TKey, List<TValue>>> other)
			: base(other)
		{
		}

		public MapList(IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> other)
			: base(other)
		{
		}

		public MapList(IEnumerable<KeyValuePair<TKey, TValue>> other)
			: base(other)
		{
		}

		protected override bool ValueEquals(List<TValue> a, List<TValue> b)
		{
			return a.SequenceEqual(b);
		}

		protected override List<TValue> ValueIntersect(List<TValue> a, IEnumerable<TValue> b)
		{
			var result = a.Intersect(b).ToList();

			if (result.Count == 0)
			{
				result = null;
			}

			return result;
		}
	}
}
