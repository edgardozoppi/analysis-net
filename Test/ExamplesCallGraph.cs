// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
	interface IPrintable
	{
		void Print();
	}

	abstract class Shape : IPrintable
	{
		public void Show()
		{
		}

		public virtual void Draw()
		{
		}

		public virtual void Print()
		{
		}
	}

	class Ellipse : Shape
	{
		public override void Draw()
		{
		}
	}

	class Circle : Ellipse
	{
		public override void Print()
		{
		}
	}

	class Rectangle : Shape
	{
		public override void Print()
		{
		}
	}

	class Square : Rectangle
	{
		public override void Draw()
		{
		}
	}

	class ExamplesCallGraph
	{
		public void Example1()
		{
			Shape circle = new Circle();
			circle.Show(); // Shape.Show
			circle.Draw(); // Ellipse.Draw
			circle.Print(); // Circle.Print

			Rectangle rectangle = new Square();
			rectangle.Show(); // Shape.Show
			rectangle.Draw(); // Square.Draw
			rectangle.Print(); // Rectangle.Print
		}
	}
}
