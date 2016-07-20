// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;
using Model;
using Backend.Model;

namespace Backend.Analyses
{
	public class ReachingDefinitionsAnalysis : ForwardDataFlowAnalysis<Subset<DefinitionInstruction>>
	{
		private DefinitionInstruction[] definitions;
		private IDictionary<IVariable, Subset<DefinitionInstruction>> variable_definitions;
		private DataFlowAnalysisResult<Subset<DefinitionInstruction>>[] result;
		private MapList<DefinitionInstruction, IInstruction> def_use;
		private MapList<IInstruction, DefinitionInstruction> use_def;
		private Subset<DefinitionInstruction>[] GEN;
		private Subset<DefinitionInstruction>[] KILL;

		public ReachingDefinitionsAnalysis(ControlFlowGraph cfg)
			: base(cfg)
		{
		}

		public MapList<DefinitionInstruction, IInstruction> DefinitionUses
		{
			get { return def_use; }
		}

		public MapList<IInstruction, DefinitionInstruction> UseDefinitions
		{
			get { return use_def; }
		}

		public void ComputeDefUseAndUseDefChains()
		{
			if (this.result == null) throw new InvalidOperationException("Analysis result not available.");

			this.def_use = new MapList<DefinitionInstruction, IInstruction>();
			this.use_def = new MapList<IInstruction, DefinitionInstruction>();

			foreach (var node in this.cfg.Nodes)
			{
				var input = new HashSet<DefinitionInstruction>();
				var node_result = this.result[node.Id];

				if (node_result.Input != null)
				{
					node_result.Input.ToSet(input);
				}

				var definitions = input.ToMapSet(def => def.Result);

				foreach (var instruction in node.Instructions)
				{
					// use-def
					foreach (var variable in instruction.UsedVariables)
					{
						if (definitions.ContainsKey(variable))
						{
							var var_defs = definitions[variable];

							foreach (var definition in var_defs)
							{
								def_use.Add(definition, instruction);
								use_def.Add(instruction, definition);
							}
						}
						else
						{
							// Add all uses, even those with no reaching definitions.
							use_def.Add(instruction);
						}
					}

					// def-use
					if (instruction is DefinitionInstruction)
					{
						var definition = instruction as DefinitionInstruction;

						if (definition.HasResult)
						{
							var variable = definition.Result;

							definitions.Remove(variable);
							definitions.Add(variable, definition);

							// Add all definitions, even those with no uses.
							def_use.Add(definition);
						}
					}
				}
			}
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
