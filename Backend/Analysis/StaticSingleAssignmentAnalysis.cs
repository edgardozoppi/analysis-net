using Backend.ThreeAddressCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analysis
{
	public class StaticSingleAssignmentAnalysis
	{
		private MethodBody method;
		private ControlFlowGraph cfg;
		private IDictionary<CFGNode, IDictionary<IVariable, PhiInstruction>> phi_instructions;

		public StaticSingleAssignmentAnalysis(MethodBody method, ControlFlowGraph cfg)
		{
			this.method = method;
			this.cfg = cfg;
			this.phi_instructions = new Dictionary<CFGNode, IDictionary<IVariable, PhiInstruction>>();
		}

		public void Transform()
		{
			//ControlFlowGraph.ComputeDominators(cfg);
			//ControlFlowGraph.ComputeDominatorTree(cfg);
			//ControlFlowGraph.ComputeDominanceFrontiers(cfg);

			this.InsertPhiInstructions();
			this.RenameVariables();
		}

		private void InsertPhiInstructions()
		{
			var defining_nodes = new Dictionary<IVariable, ISet<CFGNode>>();

			foreach (var node in cfg.Nodes)
			{
				foreach (var instruction in node.Instructions)
				{
					if (instruction is DefinitionInstruction)
					{
						var definition = instruction as DefinitionInstruction;

						if (definition.HasResult)
						{
							ISet<CFGNode> nodes;

							if (defining_nodes.ContainsKey(definition.Result))
							{
								nodes = defining_nodes[definition.Result];
							}
							else
							{
								nodes = new HashSet<CFGNode>();
								defining_nodes.Add(definition.Result, nodes);
							}

							nodes.Add(node);
						}
					}
				}
			}

			foreach (var entry in defining_nodes)
			{
				var variable = entry.Key;
				var nodes = new Stack<CFGNode>(entry.Value);

				while (nodes.Count > 0)
				{
					var current = nodes.Pop();

					foreach (var node in current.DominanceFrontier)
					{
						if (phi_instructions.ContainsKey(node) && phi_instructions[node].ContainsKey(variable)) continue;
						IDictionary<IVariable, PhiInstruction> node_phi_instructions;
						
						if (phi_instructions.ContainsKey(node))
						{
							node_phi_instructions = phi_instructions[node];
						}
						else
						{
							node_phi_instructions = new Dictionary<IVariable, PhiInstruction>();
							phi_instructions.Add(node, node_phi_instructions);
						}

						var phi = new PhiInstruction(0, variable);

						node.Instructions.Insert(0, phi);
						node_phi_instructions.Add(variable, phi);

						if (!defining_nodes[variable].Contains(node) && !nodes.Contains(node))
						{
							nodes.Push(node);
						}
					}
				}
			}
		}

		private void RenameVariables()
		{
			var derived_variables = new Dictionary<IVariable, Stack<DerivedVariable>>();
			var indices = new Dictionary<IVariable, uint>();

			foreach (var variable in method.Variables)
			{
				var derived = new DerivedVariable(variable, 0u);
				var stack = new Stack<DerivedVariable>();

				stack.Push(derived);
				derived_variables.Add(variable, stack);
				indices.Add(variable, 1u);
			}

			this.RenameVariables(cfg.Entry, derived_variables, indices);
		}

		private void RenameVariables(CFGNode node, IDictionary<IVariable, Stack<DerivedVariable>> derived_variables, Dictionary<IVariable, uint> indices)
		{
			foreach (var instruction in node.Instructions)
			{
				DerivedVariable result_derived = null;

				if (instruction is DefinitionInstruction)
				{
					var definition = instruction as DefinitionInstruction;

					if (definition.HasResult && indices.ContainsKey(definition.Result))
					{
						var result = definition.Result;
						var index = indices[result];

						result_derived = new DerivedVariable(result, index);
						definition.Result = result_derived;
					}
				}

				foreach (var variable in instruction.UsedVariables)
				{
					if (!derived_variables.ContainsKey(variable)) continue;

					var stack = derived_variables[variable];
					var derived = stack.Peek();
					instruction.Replace(variable, derived);
				}

				if (result_derived != null)
				{
					var result = result_derived.Original;
					var index = result_derived.Index;
					var result_stack = derived_variables[result];

					result_stack.Push(result_derived);
					indices[result] = index + 1;
				}
			}

			foreach (var succ in node.Successors)
			{
				if (!phi_instructions.ContainsKey(succ)) continue;
				var node_phi_instructions = phi_instructions[succ];

				foreach (var entry in node_phi_instructions)
				{
					var variable = entry.Key;
					var phi = entry.Value;
					var stack = derived_variables[variable];
					var derived = stack.Peek();

					phi.Arguments.Add(derived);
				}
			}

			foreach (var child in node.Childs)
			{
				this.RenameVariables(child, derived_variables, indices);
			}

			foreach (var instruction in node.Instructions)
			{
				if (instruction is DefinitionInstruction)
				{
					var definition = instruction as DefinitionInstruction;

					if (definition.HasResult && derived_variables.ContainsKey(definition.Result))
					{
						var derived = definition.Result as DerivedVariable;
						var result = derived.Original;
						var stack = derived_variables[result];

						stack.Pop();
					}
				}
			}
		}
	}
}
