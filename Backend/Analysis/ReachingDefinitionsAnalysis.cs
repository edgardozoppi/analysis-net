using Backend.ThreeAddressCode;
using Backend.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;

namespace Backend.Analysis
{
	public class ReachingDefinitionsAnalysis : ForwardDataFlowAnalysis<Subset<DefinitionInstruction>>
	{
		private DefinitionInstruction[] definitions;
		private IDictionary<IVariable, Subset<DefinitionInstruction>> variable_definitions;
		private DataFlowAnalysisResult<Subset<DefinitionInstruction>>[] result;
		private Map<DefinitionInstruction, Instruction> def_use;
		private Map<Instruction, DefinitionInstruction> use_def;
		private Subset<DefinitionInstruction>[] GEN;
		private Subset<DefinitionInstruction>[] KILL;

		public ReachingDefinitionsAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public Map<DefinitionInstruction, Instruction> DefinitionUse
		{
			get { return def_use; }
		}

		public Map<Instruction, DefinitionInstruction> UseDefinition
		{
			get { return use_def; }
		}

		public void ComputeDefUseAndUseDefChains()
		{
			if (this.result == null) throw new InvalidOperationException("Analysis result not available.");

			this.def_use = new Map<DefinitionInstruction, Instruction>();
			this.use_def = new Map<Instruction, DefinitionInstruction>();

			foreach (var node in this.cfg.Nodes)
			{
				ISet<DefinitionInstruction> input = new HashSet<DefinitionInstruction>();

				if (this.result[node.Id].Input != null)
				{
					this.result[node.Id].Input.ToSet(input);
				}

				foreach (var instruction in node.Instructions)
				{
					foreach (var definition in input)
					{
						var variable = definition.Result;

						if (instruction.UsedVariables.Contains(variable))
						{
							def_use.Add(definition, instruction);
							use_def.Add(instruction, definition);
						}
					}

					if (instruction is DefinitionInstruction)
					{
						var definition = instruction as DefinitionInstruction;

						if (definition.HasResult)
						{
							var variable = definition.Result;

							input = this.RemoveVariableDefinitions(input, variable);
							input.Add(definition);
						}
					}
				}
			}
		}

		private ISet<DefinitionInstruction> RemoveVariableDefinitions(ISet<DefinitionInstruction> definitions, IVariable variable)
		{
			var result = new HashSet<DefinitionInstruction>();

			foreach (var definition in definitions)
			{
				if (definition.Result.Equals(variable)) continue;

				result.Add(definition);
			}

			return result;
		}

		public override DataFlowAnalysisResult<Subset<DefinitionInstruction>>[] Analyze()
		{
			this.ComputeDefinitions();
			this.ComputeGen();
			this.ComputeKill();

			var result = base.Analyze();

			this.result = result;
			this.definitions = null;
			this.variable_definitions = null;
			this.GEN = null;
			this.KILL = null;

			return result;
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
						
						if (definition.HasResult)
						{
							result.Add(definition);
						}
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

						if (definition.HasResult)
						{
							defined[definition.Result] = index;
							index++;
						}
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

						if (definition.HasResult)
						{
							var defs = this.variable_definitions[definition.Result];
							kill.Union(defs);
						}
					}
				}

				KILL[node.Id] = kill;
			}
		}
	}
}
