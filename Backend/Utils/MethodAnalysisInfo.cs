using Backend.Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	public class MethodAnalysisInfo
	{
		private IDictionary<string, object> data;

		public MethodDefinition Method { get; private set; }

		public MethodAnalysisInfo(MethodDefinition method)
		{
			this.Method = method;
			this.data = new Dictionary<string, object>();
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
