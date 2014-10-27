using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	public class Map<TKey, TValue, TCollection> : Dictionary<TKey, TCollection>
		where TCollection : ICollection<TValue>, new()
	{
		public void Add(TKey key, TValue value)
		{
			TCollection list;

			if (this.ContainsKey(key))
			{
				list = this[key];
			}
			else
			{
				list = new TCollection();
				this.Add(key, list);
			}

			list.Add(value);
		}

		public void AddRange(TKey key, IEnumerable<TValue> values)
		{
			TCollection list;

			if (this.ContainsKey(key))
			{
				list = this[key];
			}
			else
			{
				list = new TCollection();
				this.Add(key, list);
			}

			list.AddRange(values);
		}
	}

	public class MapSet<TKey, TValue> : Map<TKey, TValue, HashSet<TValue>> { }

	public class MapList<TKey, TValue> : Map<TKey, TValue, List<TValue>> { }
}
