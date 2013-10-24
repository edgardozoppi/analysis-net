using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;

namespace Console
{
	class Program
	{
		static void Main(string[] args)
		{
			const string input = @"..\..\..\Test\bin\Debug\Test.dll";

			using (var host = new PeReader.DefaultHost())
			using (var assembly = new Assembly(host))
			{
				assembly.Load(input);

				var visitor = new MethodVisitor(host);
				visitor.Rewrite(assembly.Module);
			}

			System.Console.WriteLine("Done!");
			System.Console.ReadKey();
		}
	}
}
