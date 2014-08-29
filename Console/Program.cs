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
			//const string input = @"C:\Jackalope\DevMark\Data\Tests\Olden\BH\BH\bin\Debug\BH.exe";
			//const string input = @"C:\Jackalope\DevMark\Data\Tests\Olden\BiSort\BiSort\bin\Debug\BiSort.exe";
			//const string input = @"C:\Jackalope\DevMark\Data\Tests\Olden\Em3d\Em3d\bin\Debug\Em3d.exe";
			//const string input = @"C:\Jackalope\DevMark\Data\Tests\Olden\Health\Health\bin\Debug\Health.exe";
			//const string input = @"C:\Jackalope\DevMark\Data\Tests\Olden\Perimeter\Perimeter\bin\Debug\Perimeter.exe";
			//const string input = @"C:\Jackalope\DevMark\Data\Tests\Olden\Power\Power\bin\Debug\Power.exe";
			//const string input = @"C:\Jackalope\DevMark\Data\Tests\Olden\TSP\TSP\bin\Debug\TSP.exe";
			//const string input = @"C:\Jackalope\DevMark\Data\Tests\Large\luindex\bin\NLucene.exe";
			//const string input = @"C:\Jackalope\DevMark\Data\Tests\Large\lusearch\bin\NLucene2.exe";

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
