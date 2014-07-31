using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci;
using Backend;
using Backend.Analysis;
using Backend.Serialization;
using Backend.ThreeAddressCode;

namespace Console
{
	class MethodVisitor : MetadataRewriter
	{
		private ISourceLocationProvider sourceLocationProvider;

		public MethodVisitor(IMetadataHost host, ISourceLocationProvider sourceLocationProvider)
			: base(host)
		{
			this.sourceLocationProvider = sourceLocationProvider;
		}

		public override IMethodDefinition Rewrite(IMethodDefinition methodDefinition)
		{
			var disassembler = new Disassembler(host, methodDefinition, sourceLocationProvider);
			var methodBody = disassembler.Execute();

			System.Console.WriteLine(methodBody);
			System.Console.WriteLine();

			var cfg = ControlFlowGraph.Generate(methodBody);
			ControlFlowGraph.ComputeDominators(cfg);
			ControlFlowGraph.IdentifyLoops(cfg);

			ControlFlowGraph.ComputeDominatorTree(cfg);
			ControlFlowGraph.ComputeDominanceFrontiers(cfg);

			var ssa = new StaticSingleAssignmentAnalysis(methodBody, cfg);
			ssa.Transform();

			/*var symbolic = new SymbolicAnalysis(cfg);
			symbolic.Analyze();

			if (cfg.Loops.Count > 0)
			{
				var loop = cfg.Loops.First();
				var result = symbolic[loop.Header];

				var branch = loop.Header.Instructions.Last() as ConditionalBranchInstruction;
				var variable = branch.LeftOperand as Variable;
				var condition = result.Output[variable];


			}*/

			//var dot = DOTSerializer.Serialize(cfg);
			var dgml = DGMLSerializer.Serialize(cfg);
			
			return base.Rewrite(methodDefinition);
		}
	}
}
