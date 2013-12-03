using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
	public class Examples
	{
		public int F1;

		public int Example1(int arg1)
		{
			int a = 1;
			int b = 2;
			byte c = (byte)((a + a) + (a + a) + b * arg1);

			if (c > 3) c = 4;
			else c = 5;

			var r = this.Plus(a, b);
			return r;
		}

		public int Plus(int a, int b)
		{
			//this.Print(a, b);

			var arr = new int[a, 3, 4];
			var arr2 = new int[2][][];
			
			var obj = new Examples();
			//obj.F1 = 6;

			return a + b;
		}

		//public void Print(params object[] args)
		//{
		//	Console.WriteLine(args);
		//}
	}
}
