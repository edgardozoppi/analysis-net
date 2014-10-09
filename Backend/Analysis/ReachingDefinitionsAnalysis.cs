using Backend.ThreeAddressCode;
using Backend.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analysis
{
	public class ReachingDefinitionsAnalysis : ForwardDataFlowAnalysis<Subset<DefinitionInstruction>>
	{
		private DefinitionInstruction[] definitions;
		private IDictionary<IVariable, Subset<DefinitionInstruction>> variable_definitions;
		private Subset<DefinitionInstruction>[] GEN;
		private Subset<DefinitionInstruction>[] KILL;

		public ReachingDefinitionsAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
			this.ComputeDefinitions();
			this.ComputeGen();
			this.ComputeKill();
		}

		protected override Subset<DefinitionInstruction> InitialValue(CFGNode node)
		{
			return GEN[node.Id];
		}

		protected override bool Compare(Subset<DefinitionInstruction> left, Subset<DefinitionInstruction> right)
		{
			return left.Equals(right);
		}

		protected override Subset<DefinitionInstruction> Join(Subset<DefinitionInstruction> left, Subset<DefinitionInstruction> right)
		{
			var result = left.Clone();
			result.Union(right);
			return result;
		}

		protected override Subset<DefinitionInstruction> Flow(CFGNode node, Subset<DefinitionInstruction> input)
		{
			var output = input.Clone();
			var kill = KILL[node.Id];
			var gen = GEN[node.Id];

			output.Except(kill);
			output.Union(gen);
			return output;
		}

		private void ComputeDefinitions()
		{
			var result = new List<DefinitionInstruction>();

			foreach (var node in this.cfg.Nodes)
			{
				foreach (var instruction in node.Instructions)
				{
					if (instruction is DefinitionInstruction)
					{
						var definition = instruction as DefinitionInstruction;
						result.Add(definition);
					}
				}
			}

			this.definitions = result.ToArray();
			this.variable_definitions = new Dictionary<IVariable, Subset<DefinitionInstruction>>();

			for (var i = 0; i < this.definitions.Length; ++i)
			{
				var definition = this.definitions[i];
				Subset<DefinitionInstruction> defs = null;

				if (variable_definitions.ContainsKey(definition.Result))
				{
					defs = variable_definitions[definition.Result];
				}
				else
				{
					defs = this.definitions.ToEmptySubset();
					variable_definitions[definition.Result] = defs;
				}

				defs.Add(i);
			}
		}

		private void ComputeGen()
		{
			GEN = new Subset<DefinitionInstruction>[this.cfg.Nodes.Count];
			var index = 0;

			foreach (var node in this.cfg.Nodes)
			{
				var defined = new Dictionary<IVariable, int>();				

				foreach (var instruction in node.Instructions)
				{
					if (instruction is DefinitionInstruction)
					{
						var definition = instruction as DefinitionInstruction;

						defined[definition.Result] = index;
						index++;
					}
				}

				// We only add to gen those definitions of node
				// that reach the end of the basic block
				var gen = this.definitions.ToEmptySubset();

				foreach (var def in defined.Values)
				{
					gen.Add(def);
				}

				GEN[node.Id] = gen;
			}
		}

		private void ComputeKill()
		{
			KILL = new Subset<DefinitionInstruction>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				// We add to kill all definitions of the variables defined at node
				var kill = this.definitions.ToEmptySubset();

				foreach (var instruction in node.Instructions)
				{
					if (instruction is DefinitionInstruction)
					{
						var definition = instruction as DefinitionInstruction;

						var defs = this.variable_definitions[definition.Result];
						kill.Union(defs);
					}
				}

				KILL[node.Id] = kill;
			}
		}
	}
}
