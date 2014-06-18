using Backend.ThreeAddressCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analysis
{
	public class SSA
	{
		private ControlFlowGraph cfg;

		public SSA(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public void Transform()
		{
			this.InsertPhiInstructions();
			this.RenameVariables();
		}

		private void InsertPhiInstructions()
		{
			var defining_nodes = new Dictionary<Variable, ISet<CFGNode>>();
			var phi_variables = new Dictionary<CFGNode, ISet<Variable>>();

			foreach (var node in cfg.Nodes)
			{
				if (node.Kind != CFGNodeKind.BasicBlock) continue;

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
						if (phi_variables.ContainsKey(node) && phi_variables[node].Contains(variable)) continue;
						ISet<Variable> variables;

						if (phi_variables.ContainsKey(node))
						{
							variables = phi_variables[node];
						}
						else
						{
							variables = new HashSet<Variable>();
							phi_variables.Add(node, variables);
						}

						variables.Add(variable);
						var phi = new PhiInstruction(0, variable);
						node.Instructions.Insert(0, phi);

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
			
		}
	}
}
