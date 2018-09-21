// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

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
			var signature = MemberHelper.GetMethodSignature(methodDefinition, NameFormattingOptions.Signature | NameFormattingOptions.ParameterName); 
			System.Console.WriteLine(signature);

			var disassembler = new Disassembler(host, methodDefinition, sourceLocationProvider);
			var methodBody = disassembler.Execute();

			//System.Console.WriteLine(methodBody);
			//System.Console.WriteLine();

			var cfg = ControlFlowGraph.GenerateNormalControlFlow(methodBody);
			ControlFlowGraph.ComputeDominators(cfg);
			ControlFlowGraph.IdentifyLoops(cfg);

			ControlFlowGraph.ComputeDominatorTree(cfg);
			ControlFlowGraph.ComputeDominanceFrontiers(cfg);

			var splitter = new WebAnalysis(cfg);
			splitter.Analyze();
			splitter.Transform();

			methodBody.UpdateVariables();

			var analysis = new TypeInferenceAnalysis(cfg);
			analysis.Analyze();

			//var pointsTo = new PointsToAnalysis(cfg);
			//var result = pointsTo.Analyze();

			var ssa = new StaticSingleAssignmentAnalysis(methodBody, cfg);
			ssa.Transform();

			methodBody.UpdateVariables();

			////var dot = DOTSerializer.Serialize(cfg);
			var dgml = DGMLSerializer.Serialize(cfg);
			
			return base.Rewrite(methodDefinition);
		}
	}
}
