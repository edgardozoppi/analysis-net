// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Model;
using Backend.Utils;
using Model;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class SliceAnalysis
	{
		private ControlFlowGraph cfg;
		private IDictionary<IVariable, DefinitionInstruction> definitions;
		private IDictionary<IInstruction, CFGNode> containingNodes;

		public SliceAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public IList<Instruction> Analyze(Instruction target)
		{
			// TODO: Use points-to analysis to include fields dependencies.

			// TODO: Support also non-SSA TAC using:
			// - Use-Def chain from Reaching Definitions analysis
			// - Webs from Web analysis

			// The CFG is expected to be in SSA form.
			var result = new List<Instruction>();
			var worklist = new HashSet<Instruction>();

			if (definitions == null)
			{
				definitions = cfg.GetDefinitions();
			}

			if (containingNodes == null)
			{
				containingNodes = cfg.GetContainingNodes();
			}

			worklist.Add(target);

			while (worklist.Count > 0)
			{
				IEnumerable<Instruction> dependencies;

				var instruction = worklist.First();
				worklist.Remove(instruction);
				result.Insert(0, instruction);

				// Get data dependencies
				dependencies = from v in instruction.UsedVariables
							   where definitions.ContainsKey(v)
							   select definitions[v];

				dependencies = dependencies.Except(result);
				worklist.UnionWith(dependencies);

				// Get control dependencies
				var node = containingNodes[instruction];
				dependencies = from dependency in node.ImmediateControlDependencies
							   where dependency.Instructions.Count > 0
							   let last = dependency.Instructions.Last()
							   select last as Instruction;

				dependencies = dependencies.Except(result);
				worklist.UnionWith(dependencies);
			}

			return result;
		}
	}
}
