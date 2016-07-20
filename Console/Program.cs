using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CCIProvider;
using Model;
using Model.Types;
using Backend.Analyses;
using Backend.Serialization;
using Backend.Transformations;
using Backend.Utils;
using Model.ThreeAddressCode.Values;
using Backend.Model;

namespace Console
{
	class Program
	{
		private Host host;

		public Program(Host host)
		{
			this.host = host;
		}
		
		public void VisitMethods()
		{
			var methods = host.Assemblies.SelectMany(a => a.RootNamespace.GetAllTypes())
										 .SelectMany(t => t.Members.OfType<MethodDefinition>())
										 .Where(md => md.Body != null);

			foreach (var method in methods)
			{
				VisitMethod(method);
			}
		}

		private void VisitMethod(MethodDefinition method)
		{
			System.Console.WriteLine(method.Name);

			var methodBodyBytecode = method.Body;
			var disassembler = new Disassembler(method);
			var methodBody = disassembler.Execute();			
			method.Body = methodBody;

			var cfAnalysis = new ControlFlowAnalysis(method.Body);
			var cfg = cfAnalysis.GenerateNormalControlFlow();

			var domAnalysis = new DominanceAnalysis(cfg);
			domAnalysis.Analyze();
			domAnalysis.GenerateDominanceTree();

			var loopAnalysis = new NaturalLoopAnalysis(cfg);
			loopAnalysis.Analyze();

			var domFrontierAnalysis = new DominanceFrontierAnalysis(cfg);
			domFrontierAnalysis.Analyze();

			var splitter = new WebAnalysis(cfg);
			splitter.Analyze();
			splitter.Transform();

			methodBody.UpdateVariables();

			var typeAnalysis = new TypeInferenceAnalysis(cfg);
			typeAnalysis.Analyze();

			var forwardCopyAnalysis = new ForwardCopyPropagationAnalysis(cfg);
			forwardCopyAnalysis.Analyze();
			forwardCopyAnalysis.Transform(methodBody);

			var backwardCopyAnalysis = new BackwardCopyPropagationAnalysis(cfg);
			backwardCopyAnalysis.Analyze();
			backwardCopyAnalysis.Transform(methodBody);

			//var pointsTo = new PointsToAnalysis(cfg);
			//var result = pointsTo.Analyze();

			var ssa = new StaticSingleAssignment(methodBody, cfg);
			ssa.Transform();

			methodBody.UpdateVariables();

			//var dot = DOTSerializer.Serialize(cfg);
			var dgml = DGMLSerializer.Serialize(cfg);

			//dgml = DGMLSerializer.Serialize(host, typeDefinition);
		}

		static void Main(string[] args)
		{
			const string root = @"..\..\..";
			//const string root = @"C:"; // casa
			//const string root = @"C:\Users\Edgar\Projects"; // facu

			const string input = root + @"\Test\bin\Debug\Test.dll";

			//using (var host = new PeReader.DefaultHost())
			//using (var assembly = new Assembly(host))
			//{
			//	assembly.Load(input);

			//	Types.Initialize(host);

			//	//var extractor = new TypesExtractor(host);
			//	//extractor.Extract(assembly.Module);

			//	var visitor = new MethodVisitor(host, assembly.PdbReader);
			//	visitor.Rewrite(assembly.Module);
			//}

			var host = new Host();
			//host.Assemblies.Add(assembly);

			var loader = new Loader(host);
			loader.LoadAssembly(input);
			//loader.LoadCoreAssembly();

			var type = new BasicType("Examples")
			{
				Assembly = new AssemblyReference("Test"),
				Namespace = "Test"
			};

			var typeDefinition = host.ResolveReference(type);

			var method = new MethodReference("ExampleLoopEnumerator", null)
			{
				ContainingType = type,
				ReturnType = PlatformTypes.Void
			};

			var methodDefinition = host.ResolveReference(method) as MethodDefinition;

			var program = new Program(host);
			program.VisitMethods();

			System.Console.WriteLine("Done!");
			System.Console.ReadKey();
		}
	}
}
