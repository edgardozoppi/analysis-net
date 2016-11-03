using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	internal class UniqueIDGenerator
	{
		private int nextAvailableId;

		public UniqueIDGenerator(int initialValue = 0)
		{
			nextAvailableId = initialValue;
		}

		public int Next
		{
			get { return nextAvailableId++; }
		}
	}
}
