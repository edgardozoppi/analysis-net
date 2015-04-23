using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace Backend.Utils
{
	/// <summary>
	/// An equality comparer that compares objects for reference equality.
	/// </summary>
	public sealed class ReferenceEqualityComparer : IEqualityComparer<object>
	{
		private static readonly ReferenceEqualityComparer instance = new ReferenceEqualityComparer();

		/// <summary>
		/// Gets the default instance of the <see cref="ReferenceEqualityComparer"/> class.
		/// </summary>
		/// <value>A <see cref="ReferenceEqualityComparer"/> instance.</value>
		public static ReferenceEqualityComparer Instance
		{
			get { return instance; }
		}

		/// <inheritdoc />
		public new bool Equals(object x, object y)
		{
			return object.ReferenceEquals(x, y);
		}

		/// <inheritdoc />
		public int GetHashCode(object obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
	}
}
