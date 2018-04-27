// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
	struct Point
	{
		public int x, y;

		public void ExampleShortCircuitCondition()
		{
			if (x != 0 && y != 0)
			{
				x = y;
			}
		}

		public Point(int a, int b)
		{
			x = a;
			y = b;
		}
	}

	class Examples
	{
		public int F1;
		private static int F2 = 0;

		public void ExampleBugCopyPropagation()
		{
			var i = 1;
			var r = Math.Abs(i++) + i;
		}

		public void ExampleComplexTryCatch(int a, int b)
		{
			b = 1;

			while (a < 5)
			{
				try
				{
					b = 2;
					if (a == 1) break;
					b = 3;
				}
				catch
				{
					b = 4;
					if (a == 2) continue;
					b = 5;
				}

				a++;
			}

			b = 6;
		}

		public void ExampleSwitch(PlatformID x)
		{
			switch (x)
			{
				case PlatformID.Unix:
					this.F1 = 3;
					break;

				case PlatformID.Xbox:
					this.F1 = 4;
					break;

				default:
					this.F1 = 1000;
					break;
			}
		}

		public Type ExampleLoadToken()
		{
			var type = typeof(PlatformID);
			return type;
		}

		public void ExampleTryCatch()
		{
			var a = 0;

			try
			{
				a = 1;
			}
			catch (NotImplementedException ex1)
			{
				a = 2;
			}
			catch (NullReferenceException ex2)
			{
				a = 3;
			}

			a = 5;
		}

		public void ExampleTryFinally()
		{
			var a = 0;

			try
			{
				a = 1;
			}
			finally
			{
				a = 4;
			}

			a = 5;
		}

		public void ExampleTryCatchFinally()
		{
			var a = 0;

			try
			{
				a = 1;
			}
			catch (NotImplementedException ex1)
			{
				a = 2;
			}
			catch (NullReferenceException ex2)
			{
				a = 3;
			}
			finally
			{
				a = 4;
			}

			a = 5;
		}

		public void ExampleNestedTryCatchFinally()
		{
			var a = 0;

			try
			{
				a = 1;

				try
				{
					a = 2;
				}
				catch (NotImplementedException ex1)
				{
					a = 3;
				}
				catch (NullReferenceException ex2)
				{
					a = 4;
				}

				a = 5;
			}
			finally
			{
				a = 6;
			}

			a = 7;
		}

		public void ExampleIf()
		{
			var a = 0;

			if (a > 0)
			{
				a = 1;
			}
			else
			{
				a = 2;
			}

			a++;
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

        public int ExampleLiveVariables(int p)
        {
            var a = p;
            var b = a;
            var c = b;

            if (p > 0)
            {
                b = b + a;
                c = a;
            }
            else
            {
                c = b + p;
            }

            return p;
        }

		public int ExampleSlice(int n)
		{
			var sum = 0;
			var product = 1;
			var w = 7;

			for (var i = 1; i < n; ++i)
			{
				sum = sum + i + w;
				product = product * i;
			}

			if (n > 0)
			{
				return sum;
			}
			else
			{
				return product;
			}
		}

		// Boolean vs Int
		public void ExampleBoolean1()
		{
			System.Diagnostics.Contracts.Contract.Assert(false);
		}

		// Boolean vs Int
		public void ExampleBoolean2()
		{
			var v = false;
			System.Diagnostics.Contracts.Contract.Assert(v);
		}

		// Boolean vs Int
		// Boolean vs object reference
		public void ExampleConditionalBranches()
		{
			var num1 = 5;
			var num2 = 8;
			var v1 = (num1 == 0 || num2 == 2);
			var v2 = (num1 != 0 || num2 == 2);

			object obj1 = null;
			object obj2 = null;
			var v3 = (obj1 == null || obj2 == null);
			var v4 = (obj1 != null || obj2 == null);
		}

		public void ExampleLambda()
		{
			Func<int, int> l = (x => x * x);
			l(1);
		}

		public void ExampleCasts()
		{
			Exception e = null;
			var v = e is IndexOutOfRangeException;
			v = !(e is IndexOutOfRangeException);

			var w = e as IndexOutOfRangeException;
		}

		//public void Print(params object[] args)
		//{
		//	Console.WriteLine(args);
		//}
	}
}
