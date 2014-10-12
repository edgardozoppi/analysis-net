using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Backend;

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
			//const string input = root + @"\Test projects\SharpCompress\bin\SharpCompress.dll"; // total  | ok  | unk 
			//const string input = root + @"\Test projects\FtpLib\bin\Debug\ftplib.dll"; // total 4 | ok 0 | unk 4
			//const string input = root + @"\Test projects\Json\Src\Newtonsoft.Json\bin\Debug\Net45\Newtonsoft.Json.dll"; // total 250 | ok 75 | unk 175
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\BH\BH\bin\Debug\BH.exe"; // total 37 | ok 33 | unk 4
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\BiSort\BiSort\bin\Debug\BiSort.exe"; // total 3 | ok 0 | unk 3
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\Em3d\Em3d\bin\Debug\Em3d.exe"; // total 18 | ok 14 | unk 4
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\Health\Health\bin\Debug\Health.exe"; // total 11 | ok 6 | unk 5
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\Perimeter\Perimeter\bin\Debug\Perimeter.exe"; // total 1 | ok 0 | unk 1
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\Power\Power\bin\Debug\Power.exe"; // total 18 | ok 15 | unk 3
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Olden\TSP\TSP\bin\Debug\TSP.exe"; // total 7 | ok 0 | unk 7
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Large\luindex\bin\NLucene.exe"; // total 146 | ok 73 | unk 73
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Large\lusearch\bin\NLucene2.exe"; // total 188 | ok 85 | unk 103
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Spec\SpecRaytracer\SpecRaytracer\bin\Debug\SpecRaytracer.exe"; // total 35 | ok 25 | unk 10
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Spec\DB\DB\bin\Debug\DB.exe"; // total 17 | ok 9 | unk 8
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Simple\DelegateTests\DelegateTests\bin\Debug\DelegateTests.exe"; // total 3 | ok 3 | unk 0
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Simple\FilterMarkSumExample\FilterMarkSumExample\bin\Debug\FilterMarkSumExample.exe"; // total 5 | ok 4 | unk 1
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Simple\LibQuantum\LibQuantum\bin\Debug\LibQuantum.exe"; // total 54 | ok 34 | unk 20
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Simple\MRaytracer\MRaytracer\bin\Debug\MRaytracer.exe"; // total 5 | ok 5 | unk 0
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Simple\RList\RList\bin\Debug\RList.exe"; // total 3 | ok 1 | unk 2
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Simple\SetAndDict\SetAndDict\bin\Debug\SetAndDict.exe"; // total 16 | ok 14 | unk 2
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Simple\TList\TList\bin\Debug\TList.exe"; // total 10 | ok 1 | unk 9
			//const string input = root + @"\Jackalope\DevMark\Data\Tests\Simple\Tree\Tree\bin\Debug\Tree.exe"; // total 0 | ok 0 | unk 0

			using (var host = new PeReader.DefaultHost())
			using (var assembly = new Assembly(host))
			{
				assembly.Load(input);

				Types.Initialize(host);

				//var extractor = new TypesExtractor(host);
				//extractor.Extract(assembly.Module);

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
			var perRecognizedLoops = 0;
			var perUnknownLoops = 0;

			if (totalLoops > 0)
			{
				perRecognizedLoops = recognizedLoops * 100 / totalLoops;
				perUnknownLoops = unknownLoops * 100 / totalLoops;
			}

			System.Console.WriteLine("Total loops:\t\t{0}", totalLoops);
			System.Console.WriteLine("Recognized loops:\t{0} ({1}%)", recognizedLoops, perRecognizedLoops);
			System.Console.WriteLine("Unknown loops:\t\t{0} ({1}%)", unknownLoops, perUnknownLoops);
			System.Console.WriteLine();
		}
	}
}
