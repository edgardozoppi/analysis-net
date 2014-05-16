using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
	struct Point
	{
		public int x, y;

		public Point(int a, int b)
		{
			x = a;
			y = b;
		}
	}

	public class Examples
	{
		public int F1;
		private static int F2 = 0;

		public void ExampleWhile()
		{
			var a = 3;
			var x = 0;

			while (x < 10)
			{
				a += x;
				x++;
			}
		}

		public void ExampleDoWhile()
		{
			var a = 3;
			var x = 0;

			do
			{
				a += x;
				x++;
			}
			while (x < 10);
		}

		public void ExampleFor()
		{
			var a = 3;

			for (var x = 0; x < 10; x++)
			{
				a += x;
			}
		}

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

			var p1 = new Point(a, b);
			var p2 = p1;

			//var arr = new int[a, 3, 4];
			//var arr2 = new int[2][][];
			
			var obj = new Examples();
			obj.F1 = 6 * obj.F1 + Examples.F2;

			var array = new int[3];
			var tamanio = array.Length;

			array[a] = b;
			a = array[b];

			return a + b;
		}

		public T Example2<T>()
		{
			var a = default(T);
			return a;
		}

		//public void Print(params object[] args)
		//{
		//	Console.WriteLine(args);
		//}
	}
}
