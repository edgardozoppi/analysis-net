using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	public class Map<TKey, TValue> : Dictionary<TKey, IList<TValue>>
	{
		public void Add(TKey key, TValue value)
		{
			IList<TValue> list;

			if (this.ContainsKey(key))
			{
				list = this[key];
			}
			else
			{
				list = new List<TValue>();
				this.Add(key, list);
			}

			list.Add(value);
		}
	}
}
