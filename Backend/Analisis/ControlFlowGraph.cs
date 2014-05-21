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
		public int Id { get; private set; }
		public CFGNodeKind Kind { get; private set; }
		public ISet<CFGNode> Predecessors { get; private set; }
		public ISet<CFGNode> Successors { get; private set; }
		public ISet<CFGNode> Dominators { get; private set; }
		public IList<Instruction> Instructions { get; private set; }

		public CFGNode(int id, CFGNodeKind kind)
		{
			this.Id = id;
			this.Kind = kind;
			this.Predecessors = new HashSet<CFGNode>();
			this.Successors = new HashSet<CFGNode>();
			this.Dominators = new HashSet<CFGNode>();
			this.Instructions = new List<Instruction>();
		}

		public override string ToString()
		{
			string result;

			switch (this.Kind)
			{
				case CFGNodeKind.Enter: result = "enter"; break;
				case CFGNodeKind.Exit: result = "exit"; break;
				default: result = string.Join("\n", this.Instructions); break;
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
			this.Enter = new CFGNode(0, CFGNodeKind.Enter);
			this.Exit = new CFGNode(1, CFGNodeKind.Exit);
			this.Nodes = new HashSet<CFGNode>() { this.Enter, this.Exit };
		}

		#region Generate

		public static ControlFlowGraph Generate(MethodBody method)
		{			
			var leaders = ControlFlowGraph.CreateNodes(method);
			var cfg = ControlFlowGraph.ConnectNodes(method, leaders);
			return cfg;
		}

		private static IDictionary<string, CFGNode> CreateNodes(MethodBody method)
		{
			var leaders = new Dictionary<string, CFGNode>();
			var nextIsLeader = true;
			var nodeKind = CFGNodeKind.BasicBlock;
			var nodeId = 2;

			foreach (var instruction in method.Instructions)
			{
				var isLeader = false;

				if (instruction is TryInstruction ||
					instruction is CatchInstruction ||
					instruction is FinallyInstruction ||
					nextIsLeader)
				{
					isLeader = true;
					nextIsLeader = false;
					nodeKind = CFGNodeKind.BasicBlock;
				}

				if (isLeader && !leaders.ContainsKey(instruction.Label))
				{
					var node = new CFGNode(nodeId++, nodeKind);
					leaders.Add(instruction.Label, node);
				}

				if (instruction is UnconditionalBranchInstruction ||
					instruction is ConditionalBranchInstruction)
				{
					nextIsLeader = true;
					var branch = instruction as IBranchInstruction;

					if (!leaders.ContainsKey(branch.Target))
					{
						var node = new CFGNode(nodeId++, CFGNodeKind.BasicBlock);
						leaders.Add(branch.Target, node);
					}
				}
				else if (instruction is ReturnInstruction)
				//TODO: || instruction is ThrowInstruction
				{
					nextIsLeader = true;
				}
			}

			return leaders;
		}

		private static ControlFlowGraph ConnectNodes(MethodBody method, IDictionary<string, CFGNode> leaders)
		{
			var cfg = new ControlFlowGraph();
			var connectWithPreviousNode = true;
			var current = cfg.Enter;
			CFGNode previous;

			foreach (var instruction in method.Instructions)
			{
				if (leaders.ContainsKey(instruction.Label))
				{
					previous = current;
					current = leaders[instruction.Label];

					if (connectWithPreviousNode)
					{
						cfg.ConnectNodes(previous, current);
					}
				}

				connectWithPreviousNode = true;
				current.Instructions.Add(instruction);

				if (instruction is IBranchInstruction)
				{
					var branch = instruction as IBranchInstruction;
					var target = leaders[branch.Target];

					cfg.ConnectNodes(current, target);
					connectWithPreviousNode = instruction is ConditionalBranchInstruction ||
											  instruction is ExceptionalBranchInstruction;
				}
				else if (instruction is ReturnInstruction)
				{
					//TODO: not always connect to exit, could exists a finally block
					cfg.ConnectNodes(current, cfg.Exit);
				}
			}

			cfg.ConnectNodes(current, cfg.Exit);
			return cfg;
		}

		#endregion

		#region Dominators

		public static void ComputeDominators(ControlFlowGraph cfg)
		{
			cfg.Enter.Dominators.Add(cfg.Enter);

			foreach (var node in cfg.Nodes)
			{
				if (node.Kind == CFGNodeKind.Enter)
					continue;

				node.Dominators.UnionWith(cfg.Nodes);
			}

			bool changed;

			do
			{
				changed = false;

				foreach (var node in cfg.Nodes)
				{
					if (node.Kind == CFGNodeKind.Enter)
						continue;

					var oldCount = node.Dominators.Count;

					foreach (var predecessor in node.Predecessors)
						node.Dominators.IntersectWith(predecessor.Dominators);

					node.Dominators.Add(node);
					var newCount = node.Dominators.Count;

					if (newCount != oldCount)
						changed = true;
				}
			}
			while (changed);
		}

		#endregion

		public void ConnectNodes(CFGNode predecessor, CFGNode successor)
		{
			successor.Predecessors.Add(predecessor);
			predecessor.Successors.Add(successor);
			this.Nodes.Add(predecessor);
			this.Nodes.Add(successor);
		}
	}
}
