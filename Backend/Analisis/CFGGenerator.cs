using Backend.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analisis
{
	public enum CFGNodeKind
	{
		Enter,
		Exit,
		BasicBlock
	}

	public class CFGNode
	{
		public CFGNodeKind Kind { get; private set; }
		public ISet<CFGNode> Predecessors { get; private set; }
		public ISet<CFGNode> Successors { get; private set; }
		public IList<Instruction> Instructions { get; private set; }

		public CFGNode(CFGNodeKind kind)
		{
			this.Kind = kind;
			this.Predecessors = new HashSet<CFGNode>();
			this.Successors = new HashSet<CFGNode>();
			this.Instructions = new List<Instruction>();
		}

		public override string ToString()
		{
			string result;

			switch (this.Kind)
			{
				case CFGNodeKind.Enter: result = "enter"; break;
				case CFGNodeKind.Exit: result = "exit"; break;
				case CFGNodeKind.BasicBlock: result = string.Join("\n\t", this.Instructions); break;
				default: throw new Exception("Unknown Control Flow Graph node kind: " + this.Kind);
			}

			return result;
		}
	}

	public class ControlFlowGraph
	{
		public CFGNode Enter { get; private set; }
		public CFGNode Exit { get; private set; }
		public ISet<CFGNode> Nodes { get; private set; }

		public ControlFlowGraph()
		{
			this.Enter = new CFGNode(CFGNodeKind.Enter);
			this.Exit = new CFGNode(CFGNodeKind.Exit);
			this.Nodes = new HashSet<CFGNode>() { this.Enter, this.Exit };
		}

		public void ConnectNodes(CFGNode predecessor, CFGNode successor)
		{
			successor.Predecessors.Add(predecessor);
			predecessor.Successors.Add(successor);
			this.Nodes.Add(predecessor);
			this.Nodes.Add(successor);
		}
	}

	public class CFGGenerator
	{
		public MethodBody Method { get; private set; }

		public CFGGenerator(MethodBody method)
		{
			this.Method = method;
		}

		public ControlFlowGraph Generate()
		{
			var targets = new Dictionary<string, Instruction>();
			var cfg = new ControlFlowGraph();
			var node = new CFGNode(CFGNodeKind.BasicBlock);

			cfg.ConnectNodes(cfg.Enter, node);

			foreach (var instruction in this.Method.Instructions)
			{
				var createNewNode = false;

				if (instruction is IBranchInstruction)
				{
					var branch = instruction as IBranchInstruction;
					targets.Add(branch.Target, instruction);
					createNewNode = true;
				}
				
				if (targets.ContainsKey(instruction.Label))
				{
					createNewNode = true;

					//TODO: terminar esto!!!
				}

				node.Instructions.Add(instruction);

				if (createNewNode)
				{
					node = new CFGNode(CFGNodeKind.BasicBlock);
				}
			}

			return cfg;
		}
	}
}
