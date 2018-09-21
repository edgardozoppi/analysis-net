// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

using Backend.Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	public abstract class AnalysisInfo
	{
		private IDictionary<string, object> data;

		protected AnalysisInfo()
		{
			data = new Dictionary<string, object>();
		}

		public IEnumerable<string> Keys
		{
			get { return data.Keys; }
		}

		public bool Contains(string key)
		{
			return data.ContainsKey(key);
		}

		public void Add(string key, object value)
		{
			data.Add(key, value);
		}

		public void Set(string key, object value)
		{
			data[key] = value;
		}

		public void ClearKeys()
		{
			data.Clear();
		}

		public virtual void Clear()
		{
			this.ClearKeys();
		}

		public void Remove(string key)
		{
			data.Remove(key);
		}

		public T Get<T>(string key)
		{
			return (T)data[key];
		}

		public bool TryGet<T>(string key, out T value)
		{
			object info;
			var result = data.TryGetValue(key, out info);

			if (result)
			{
				value = (T)info;
			}
			else
			{
				value = default(T);
			}

			return result;
		}
	}
}
