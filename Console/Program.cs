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
			const string root = @"..\..\..";
			//const string root = @"C:"; // casa
			//const string root = @"C:\Users\Edgar\Projects"; // facu

			const string input = root + @"\Test\bin\Debug\Test.dll";
			//const string input = root + @"\Test projects\Json\Src\Newtonsoft.Json\bin\Debug\Net45\Newtonsoft.Json.dll";
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\BH\BH\bin\Debug\BH.exe"; // total 37 | ok 33 | unk 4
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\BiSort\BiSort\bin\Debug\BiSort.exe"; // total 3 | ok 0 | unk 3
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\Em3d\Em3d\bin\Debug\Em3d.exe"; // total 18 | ok 14 | unk 4
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\Health\Health\bin\Debug\Health.exe"; // total 11 | ok 6 | unk 5
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\Perimeter\Perimeter\bin\Debug\Perimeter.exe"; // total 1 | ok 0 | unk 1
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\Power\Power\bin\Debug\Power.exe"; // total 18 | ok 15 | unk 3
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\TSP\TSP\bin\Debug\TSP.exe"; // total 7 | ok 0 | unk 7
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Large\luindex\bin\NLucene.exe"; // total 146 | ok 73 | unk 73
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Large\lusearch\bin\NLucene2.exe"; // total 188 | ok 85 | unk 103

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
