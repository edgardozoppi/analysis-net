using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
	public static class Extensions
	{
		public static void AddRange<T>(this ICollection<T> self, IEnumerable<T> elements)
		{
			foreach (var element in elements)
			{
				self.Add(element);
			}
		}

		public static IEnumerable<T> ToEnumerable<T>(this T item)
		{
			yield return item;
		}
	}
}
