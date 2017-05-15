// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
	class Node
	{
		public object Value;
		public Node Next;

		public Node(object value)
		{
			this.Value = value;
		}
	}

	class ExamplesPointsTo
	{
		public void Example1()
		{
			var node = new Node(1);
			node.Next = new Node(2);

			var first = node;
			var second = first.Next;
			var third = second.Next;
			third.Next = null;
		}

		public void Example2(bool a, bool b)
		{
			var node = new Node(1);

			if (a) node.Next = new Node(2);
			else if (b) node.Next = new Node(3);
			else node.Next = node;

			var next = node.Next;
			next.Next = null;
		}

		public void Example3()
		{
			var current = new Node(1);

			for (var i = 0; i < 5; ++i)
			{
				current.Next = new Node(i + 1);
				current = current.Next;
			}
		}

		public Node Example4(bool a, bool b)
		{
			var node = new Node(1);

			if (a) return new Node(2);
			else if (b) return null;

			return node;
		}

		public Node Example5(Node a, Node b)
		{
			var c = new Node(3);

			a.Next = b;
			b.Next = c;

			return c;
		}

		public void Example6()
		{
			var n = new Node(1);
			var m = new Node(2);
			var w = Example5(n, m);

			w.Next = n;
		}

		// Direct recursion
		public Node Example7(int n)
		{
			Node z = null;

			for (var i = 0; i < n; ++i)
				z = new Node(i);

			if (n > 0)
			{
				z.Next = Example7(n - 1);
			}

			return z;
		}

		public void Example8()
		{
			var q = Example7(4);
		}

		// Direct recursion
		public void Example9(Node node, int n)
		{
			if (n > 0)
			{
				node.Next = new Node(n);
				Example9(node.Next, n - 1);
			}
		}

		public void Example10()
		{
			var first = new Node(1);
			Example9(first, 4);
		}

		// Mutual recursion
		public Node Example11(Node node1, int n)
		{
			if (n > 0)
			{
				return Example12(node1, n - 1);
			}

			return node1;
		}

		// Mutual recursion
		public Node Example12(Node node2, int m)
		{
			node2.Next = new Node(m);
			return Example11(node2.Next, m);
		}

		public void Example13()
		{
			var first = new Node(1);
			var other = Example11(first, 4);
		}

		// Lambdas
		public void ExampleLambdaCaller()
		{
			var c = 5;
			var result = ExampleLambdaCallee(x => x + c++);
			result = c;
		}

		public int ExampleLambdaCallee(Func<int, int> lambda)
		{
			return lambda(3);
		}

		// Delegates
		public delegate int MyDelegate(int w);

		public void ExampleDelegateCaller()
		{
			MyDelegate del = Function;

			if (del == null)
			{
				del = this.Function2;
			}

			var result = ExampleDelegateCallee(del);
		}

		public int ExampleDelegateCallee(MyDelegate lambda)
		{
			return lambda(3);
		}

		public static int Function(int x)
		{
			return x + 1;
		}

		public int Function2(int x)
		{
			return x + 1;
		}

		// Structs
		public struct MyStruct
		{
			public Node Valor;

			public MyStruct(Node valor)
			{
				this.Valor = valor;
				var w = this;
				w.Valor = valor;
			}
		}

		public MyStruct ExampleStructCaller(Node n)
		{
			var a = new MyStruct(n);
			var b = a;
			b.Valor = n;
			n = a.Valor;
			ExampleStructCallee(b);
			return b;
		}

		public void ExampleStructCallee(MyStruct h)
		{
			var k = h;
			k.Valor = null;
		}

		// Strings
		public static void ExampleString()
		{
			var s = "hola".Trim();
			string.Format("chau");
		}
	}
}
