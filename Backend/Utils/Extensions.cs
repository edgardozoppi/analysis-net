using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Backend.Utils
{
	public static class Extensions
	{
		public static bool Different(this BitArray left, BitArray right)
		{
			if (left.Length != right.Length)
				return true;

			for (var i = 0; i < left.Length; ++i)
			{
				if (left[i] != right[i])
					return true;
			}

			return false;
		}

		public static void FromBitArray<T>(this ISet<T> self, T[] elements, BitArray array)
		{
			self.Clear();

			for (var i = 0; i < array.Length; ++i)
			{
				var included = array[i];

				if (included)
				{
					var element = elements[i];
					self.Add(element);
				}
			}
		}
	}
}
