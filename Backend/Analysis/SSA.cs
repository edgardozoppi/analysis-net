using Backend.ThreeAddressCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analysis
{
	public class SSA
	{
		private MethodBody method;
		private ControlFlowGraph cfg;
		private IDictionary<CFGNode, IDictionary<Variable, AssignmentInstruction>> phi_instructions;

		public SSA(MethodBody method, ControlFlowGraph cfg)
		{
			this.method = method;
			this.cfg = cfg;
			this.phi_instructions = new Dictionary<CFGNode, IDictionary<Variable, AssignmentInstruction>>();
		}

		public void Transform()
		{
			this.InsertPhiInstructions();
			this.RenameVariables();
		}

		private void InsertPhiInstructions()
		{
			var defining_nodes = new Dictionary<Variable, ISet<CFGNode>>();

			foreach (var node in cfg.Nodes)
			{
				foreach (var instruction in node.Instructions)
				{
					if (instruction is AssignmentInstruction)
					{
						var assignment = instruction as AssignmentInstruction;
						ISet<CFGNode> nodes;

						if (defining_nodes.ContainsKey(assignment.Result))
						{
							nodes = defining_nodes[assignment.Result];
						}
						else
						{
							nodes = new HashSet<CFGNode>();
							defining_nodes.Add(assignment.Result, nodes);
						}

						nodes.Add(node);
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
						IDictionary<Variable, AssignmentInstruction> node_phi_instructions;
						
						if (phi_instructions.ContainsKey(node))
						{
							node_phi_instructions = phi_instructions[node];
						}
						else
						{
							node_phi_instructions = new Dictionary<Variable, AssignmentInstruction>();
							phi_instructions.Add(node, node_phi_instructions);
						}

						var phi = new PhiExpression(variable);
						var assign = new AssignmentInstruction(0, variable, phi);
						node.Instructions.Insert(0, assign);
						node_phi_instructions.Add(variable, assign);

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
			var derived_variables = new Dictionary<Variable, Stack<DerivedVariable>>();
			var indices = new Dictionary<Variable, uint>();

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

		private void RenameVariables(CFGNode node, IDictionary<Variable, Stack<DerivedVariable>> derived_variables, Dictionary<Variable, uint> indices)
		{
			foreach (var instruction in node.Instructions)
			{
				if (instruction is AssignmentInstruction)
				{
					Stack<DerivedVariable> stack;
					DerivedVariable derived;
					var assignment = instruction as AssignmentInstruction;
					var isPhiInstruction = assignment.Expression is PhiExpression;

					if (!isPhiInstruction)
					{
						var variables = assignment.Expression.Variables;

						foreach (var variable in variables)
						{
							stack = derived_variables[variable];
							derived = stack.Peek();
							assignment.Expression = assignment.Expression.Replace(variable, derived);
						}
					}
					
					var result = assignment.Result.Root;
					var index = indices[result];

					stack = derived_variables[result];
					derived = new DerivedVariable(result, index);
					assignment.Result = assignment.Result.Replace(result, derived);
					stack.Push(derived);
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
					var phi = entry.Value.Expression as PhiExpression;
					var stack = derived_variables[variable];
					var derived = stack.Peek();

					phi.Indices.Add(derived.Index);
				}
			}

			foreach (var child in node.Childs)
			{
				this.RenameVariables(child, derived_variables, indices);
			}

			foreach (var instruction in node.Instructions)
			{
				if (instruction is AssignmentInstruction)
				{
					var assignment = instruction as AssignmentInstruction;
					var derived = assignment.Result.Root as DerivedVariable;
					var result = derived.Original;
					var stack = derived_variables[result];

					stack.Pop();
				}
			}
		}
	}
}
