using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
	public class Examples
	{
		public byte Example1(int arg1)
		{
			int a = 1;
			int b = 2;
			byte c = (byte)((a + a) + (a + a) + b * arg1);

			if (c > 3) c = 4;
			else c = 5;

			return c;
		}
	}
}
