using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
	public class UnknownValueException<T> : Exception where T : struct
	{
		public T Value { get; private set; }

		public UnknownValueException(T value)
		{
			this.Value = value;
		}

		public override string Message
		{
			get { return string.Format("Unknown {0} value: {0}", typeof(T), this.Value); }
		}
	}
}
