using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	public static class Extensions
	{
		public static Subset<T> Subset<T>(this T[] universe, bool addAll = false)
		{
			return new Subset<T>(universe, addAll);
		}
	}
}
