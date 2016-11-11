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

		public Node Example11(Node node1, int n)
		{
			if (n > 0)
			{
				return Example12(node1, n - 1);
			}

			return node1;
		}

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
	}
}
