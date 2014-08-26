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
			//const string input = @"C:\Users\Edgar\Projects\Test projects\Json\Src\Newtonsoft.Json\bin\Debug\Net45\Newtonsoft.Json.dll";

			using (var host = new PeReader.DefaultHost())
			using (var assembly = new Assembly(host))
			{
				assembly.Load(input);

				var visitor = new MethodVisitor(host, assembly.PdbReader);
				visitor.Rewrite(assembly.Module);

				DisplayLoopsInfo(visitor.TotalLoops, visitor.RecognizedLoops);
			}

			System.Console.WriteLine("Done!");
			System.Console.ReadKey();
		}

		private static void DisplayLoopsInfo(int totalLoops, int recognizedLoops)
		{
			var unknownLoops = totalLoops - recognizedLoops;
			var perRecognizedLoops = recognizedLoops * 100 / totalLoops;
			var perUnknownLoops = unknownLoops * 100 / totalLoops;

			System.Console.WriteLine("Total loops:\t\t{0}", totalLoops);
			System.Console.WriteLine("Recognized loops:\t{0} ({1}%)", recognizedLoops, perRecognizedLoops);
			System.Console.WriteLine("Unknown loops:\t\t{0} ({1}%)", unknownLoops, perUnknownLoops);
			System.Console.WriteLine();
		}
	}
}
