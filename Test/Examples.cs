using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
	public class Examples
	{
		public void Example1(int arg1)
		{
			int a = 1;
			int b = 2;
			byte c = (byte)((a + a) + (a + a) + b * arg1);
		}
	}
}
