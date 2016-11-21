// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

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
using Tac = Model.ThreeAddressCode.Instructions;
using Bytecode = Model.Bytecode;

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
			var allDefinedMethods = from a in host.Assemblies
									from t in a.RootNamespace.GetAllTypes()
									from m in t.Members.OfType<MethodDefinition>()
									where m.HasBody
									select m;

			foreach (var method in allDefinedMethods)
			{
				VisitMethod(method);
			}
		}

		private void VisitMethod(MethodDefinition method)
		{
			System.Console.WriteLine(method.ToSignatureString());

			var methodBodyBytecode = method.Body;
			var disassembler = new Disassembler(method);
			var methodBody = disassembler.Execute();			
			method.Body = methodBody;

			var cfAnalysis = new ControlFlowAnalysis(method.Body);
			//var cfg = cfAnalysis.GenerateNormalControlFlow();
			var cfg = cfAnalysis.GenerateExceptionalControlFlow();

			var dgml_CFG = DGMLSerializer.Serialize(cfg);

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

			// Copy Propagation
			var forwardCopyAnalysis = new ForwardCopyPropagationAnalysis(cfg);
			forwardCopyAnalysis.Analyze();
			forwardCopyAnalysis.Transform(methodBody);

			var backwardCopyAnalysis = new BackwardCopyPropagationAnalysis(cfg);
			backwardCopyAnalysis.Analyze();
			backwardCopyAnalysis.Transform(methodBody);

			// Points-To
			var pointsTo = new PointsToAnalysis(cfg, method);
			var result = pointsTo.Analyze();

			var ptg = result[cfg.Exit.Id].Output;
			//ptg.RemoveVariablesExceptParameters();
			ptg.RemoveTemporalVariables();

			var dgml_PTG = DGMLSerializer.Serialize(ptg);

			// Live Variables
			var liveVariables = new LiveVariablesAnalysis(cfg);
			liveVariables.Analyze();

			// SSA
			var ssa = new StaticSingleAssignment(methodBody, cfg);
			ssa.Transform();
			ssa.Prune(liveVariables);

			methodBody.UpdateVariables();

			//var dot = DOTSerializer.Serialize(cfg);
			//var dgml = DGMLSerializer.Serialize(cfg);

			//dgml = DGMLSerializer.Serialize(host, typeDefinition);
		}

		private static void RunSomeTests()
		{
			const string root = @"..\..\..";
			//const string root = @"C:"; // casa
			//const string root = @"C:\Users\Edgar\Projects"; // facu

			const string input = root + @"\Test\bin\Debug\Test.dll";

			var host = new Host();
			//host.Assemblies.Add(assembly);

			PlatformTypes.Resolve(host);

			var loader = new Loader(host);
			loader.LoadAssembly(input);
			//loader.LoadCoreAssembly();

			var type = new BasicType("ExamplesPointsTo")
			{
				ContainingAssembly = new AssemblyReference("Test"),
				ContainingNamespace = "Test"
			};

			var typeDefinition = host.ResolveReference(type);

			var method = new MethodReference("Example1", PlatformTypes.Void)
			{
				ContainingType = type,
			};

			//var methodDefinition = host.ResolveReference(method) as MethodDefinition;

			var program = new Program(host);
			program.VisitMethods();

			// Testing method calls inlining
			var methodDefinition = host.ResolveReference(method) as MethodDefinition;
			var methodCalls = methodDefinition.Body.Instructions.OfType<Tac.MethodCallInstruction>().ToList();

			foreach (var methodCall in methodCalls)
			{
				var callee = host.ResolveReference(methodCall.Method) as MethodDefinition;
				methodDefinition.Body.Inline(methodCall, callee.Body);
			}

			methodDefinition.Body.UpdateVariables();

			type = new BasicType("ExamplesCallGraph")
			{
				ContainingAssembly = new AssemblyReference("Test"),
				ContainingNamespace = "Test"
			};

			method = new MethodReference("Example1", PlatformTypes.Void)
			{
				ContainingType = type,
			};

			methodDefinition = host.ResolveReference(method) as MethodDefinition;

			var ch = new ClassHierarchy(host);
			ch.Analyze();

			var dgml = DGMLSerializer.Serialize(ch);

			var chcga = new ClassHierarchyAnalysis(host, ch);
			var roots = host.GetRootMethods();
			var cg = chcga.Analyze(roots);

			dgml = DGMLSerializer.Serialize(cg);
		}

		private static void RunGenericsTests()
		{
			const string root = @"..\..\..";
			const string input = root + @"\Test\bin\Debug\Test.dll";

			var host = new Host();

			PlatformTypes.Resolve(host);

			var loader = new Loader(host);
			loader.LoadAssembly(input);
			//loader.LoadCoreAssembly();

			var assembly = new AssemblyReference("Test");

			var typeA = new GenericParameterReference(GenericParameterKind.Type, 0);
			var typeB = new GenericParameterReference(GenericParameterKind.Type, 1);

			var typeNestedClass = new BasicType("NestedClass")
			{
				ContainingAssembly = assembly,
				ContainingNamespace = "Test",
				GenericParameterCount = 2,
				ContainingType = new BasicType("ExamplesGenerics")
				{
					ContainingAssembly = assembly,
					ContainingNamespace = "Test",
					GenericParameterCount = 1
				}				
			};

			//typeNestedClass.ContainingType.GenericArguments.Add(typeA);
			//typeNestedClass.GenericArguments.Add(typeB);

			var typeDefinition = host.ResolveReference(typeNestedClass);

			if (typeDefinition == null)
			{
				System.Console.WriteLine("[Error] Cannot resolve type:\n{0}", typeNestedClass);
			}

			var typeK = new GenericParameterReference(GenericParameterKind.Method, 0);
			var typeV = new GenericParameterReference(GenericParameterKind.Method, 1);

			var typeKeyValuePair = new BasicType("KeyValuePair")
			{
				ContainingAssembly = new AssemblyReference("mscorlib"),
				ContainingNamespace = "System.Collections.Generic",
				GenericParameterCount = 2
			};

			typeKeyValuePair.GenericArguments.Add(typeK);
			typeKeyValuePair.GenericArguments.Add(typeV);

			var methodExampleGenericMethod = new MethodReference("ExampleGenericMethod", typeKeyValuePair)
			{
				ContainingType = typeNestedClass,
				GenericParameterCount = 2
			};

			//methodExampleGenericMethod.GenericArguments.Add(typeK);
			//methodExampleGenericMethod.GenericArguments.Add(typeV);

			methodExampleGenericMethod.Parameters.Add(new MethodParameterReference(0, typeA));
			methodExampleGenericMethod.Parameters.Add(new MethodParameterReference(1, typeB));
			methodExampleGenericMethod.Parameters.Add(new MethodParameterReference(2, typeK));
			methodExampleGenericMethod.Parameters.Add(new MethodParameterReference(3, typeV));
			methodExampleGenericMethod.Parameters.Add(new MethodParameterReference(4, typeKeyValuePair));

			var methodDefinition = host.ResolveReference(methodExampleGenericMethod) as MethodDefinition;

			if (methodDefinition == null)
			{
				System.Console.WriteLine("[Error] Cannot resolve method:\n{0}", methodExampleGenericMethod);
			}

			var methodExample = new MethodReference("Example", PlatformTypes.Void)
			{
				ContainingType = new BasicType("ExamplesGenericReferences")
				{
					ContainingAssembly = assembly,
					ContainingNamespace = "Test"
				}
			};

			methodDefinition = host.ResolveReference(methodExample) as MethodDefinition;

			if (methodDefinition == null)
			{
				System.Console.WriteLine("[Error] Cannot resolve method:\n{0}", methodExample);
			}

			var calls = methodDefinition.Body.Instructions.OfType<Bytecode.MethodCallInstruction>();

			foreach (var call in calls)
			{
				methodDefinition = host.ResolveReference(call.Method) as MethodDefinition;

				if (methodDefinition == null)
				{
					System.Console.WriteLine("[Error] Cannot resolve method:\n{0}", call.Method);
				}
			}
		}

		private static void RunInterPointsToTests()
		{
			const string root = @"..\..\..";
			const string input = root + @"\Test\bin\Debug\Test.dll";

			var host = new Host();

			PlatformTypes.Resolve(host);

			var loader = new Loader(host);
			loader.LoadAssembly(input);
			//loader.LoadCoreAssembly();

			var methodReference = new MethodReference("Example6", PlatformTypes.Void)
			{
				ContainingType = new BasicType("ExamplesPointsTo", TypeKind.ReferenceType)
				{
					ContainingAssembly = new AssemblyReference("Test"),
					ContainingNamespace = "Test"
				}
			};

			//var parameter = new MethodParameterReference(0, PlatformTypes.Boolean);
			//methodReference.Parameters.Add(parameter);
			//parameter = new MethodParameterReference(1, PlatformTypes.Boolean);
			//methodReference.Parameters.Add(parameter);

			//methodReference.ReturnType = new BasicType("Node", TypeKind.ReferenceType)
			//{
			//	ContainingAssembly = new AssemblyReference("Test"),
			//	ContainingNamespace = "Test"
			//};

			methodReference.Resolve(host);

			var programInfo = new ProgramAnalysisInfo();
			var pta = new InterPointsToAnalysis(programInfo);

			var cg = pta.Analyze(methodReference.ResolvedMethod);
			var dgml_CG = DGMLSerializer.Serialize(cg);

			var esca = new EscapeAnalysis(programInfo, cg);
			var escapeResult = esca.Analyze();

			foreach (var method in cg.Methods)
			{
				if (method.ResolvedMethod == null) continue;

				var methodInfo = programInfo[method.ResolvedMethod];
				var result = methodInfo.Get<DataFlowAnalysisResult<PointsToGraph>[]>(InterPointsToAnalysis.PTG_INFO);
				var ptg = result[ControlFlowGraph.ExitNodeId].Output;
				ptg.RemoveTemporalVariables();
				//ptg.RemoveVariablesExceptParameters();
				var dgml_PTG = DGMLSerializer.Serialize(ptg);

				var escapeInfo = escapeResult[method];
			}
		}

		static void Main(string[] args)
		{
			//RunSomeTests();
			//RunGenericsTests();
			RunInterPointsToTests();

			System.Console.WriteLine("Done!");
			System.Console.ReadKey();
		}
	}
}
