// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
	abstract class Shape
	{
		public virtual void Print()
		{
		}
	}

	class Circle : Shape
	{
		public override void Print()
		{
		}
	}

	class ExamplesCallGraph
	{
		public void Example1()
		{
			Shape circle = new Circle();
			circle.Print();
		}
	}
}
