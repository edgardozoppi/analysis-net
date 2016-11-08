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

		public Node Example5(Node a, Node b, out Node p)
		{
			var c = new Node(3);

			a.Next = b;
			b.Next = c;

			p = c;
			return c;
		}

		public void Example6()
		{
			Node r;
			var n = new Node(1);
			var m = new Node(2);
			var w = Example5(n, m, out r);

			w.Next = n;
		}
	}
}
