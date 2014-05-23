using Backend.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using Backend.Utils;

namespace Backend.Analisis
{
	public enum CFGNodeKind
	{
		Enter,
		Exit,
		BasicBlock
	}

	public class CFGLoop
	{
		public CFGNode Header { get; set; }
		public ISet<CFGNode> Body { get; private set; }

		public CFGLoop(CFGNode header)
		{
			this.Header = header;
			this.Body = new HashSet<CFGNode>();
			this.Body.Add(header);
		}

		public override string ToString()
		{
			var sb = new StringBuilder(" ");

			foreach (var node in this.Body)
				sb.AppendFormat("B{0} ", node.Id);

			return sb.ToString();
		}
	}

	public class CFGEdge
	{
		public CFGNode Source { get; set; }
		public CFGNode Target { get; set; }

		public CFGEdge(CFGNode source, CFGNode target)
		{
			this.Source = source;
			this.Target = target;
		}

		public override string ToString()
		{
			return string.Format("{0} -> {1}", this.Source, this.Target);
		}
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
		public ISet<CFGLoop> Loops { get; private set; }

		public ControlFlowGraph()
		{
			this.Enter = new CFGNode(0, CFGNodeKind.Enter);
			this.Exit = new CFGNode(1, CFGNodeKind.Exit);
			this.Nodes = new HashSet<CFGNode>() { this.Enter, this.Exit };
			this.Loops = new HashSet<CFGLoop>();
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
			var cfgNodes = cfg.Nodes.ToArray();
			var dominators = new BitArray[cfgNodes.Length];

			var enterDominators = new BitArray(cfgNodes.Length);
			enterDominators[cfg.Enter.Id] = true;
			dominators[cfg.Enter.Id] = enterDominators;

			foreach (var node in cfg.Nodes)
			{
				if (node.Kind == CFGNodeKind.Enter)
					continue;

				dominators[node.Id] = new BitArray(cfgNodes.Length, true);
			}

			bool changed;

			do
			{
				changed = false;

				foreach (var node in cfgNodes)
				{
					if (node.Kind == CFGNodeKind.Enter)
						continue;

					var newDominators = new BitArray(cfgNodes.Length, true);

					foreach (var predecessor in node.Predecessors)
					{
						var preDominators = dominators[predecessor.Id];
						newDominators.And(preDominators);
					}

					newDominators[node.Id] = true;
					var different = dominators[node.Id].Different(newDominators);

					if (different)
					{
						dominators[node.Id] = newDominators;
						changed = true;
					}
				}
			}
			while (changed);

			for (var i = 0; i < cfgNodes.Length; ++i)
			{
				var node = cfgNodes[i];
				var nodeDominators = dominators[i];

				node.Dominators.FromBitArray(cfgNodes, nodeDominators);
			}
		}

		#endregion

		#region Loops

		public static void IdentifyLoops(ControlFlowGraph cfg)
		{
			cfg.Loops.Clear();
			var backEdges = ControlFlowGraph.IdentifyBackEdges(cfg);

			foreach (var edge in backEdges)
			{
				var loop = ControlFlowGraph.IdentifyLoop(edge);
				cfg.Loops.Add(loop);
			}
		}

		private static ISet<CFGEdge> IdentifyBackEdges(ControlFlowGraph cfg)
		{
			var backEdges = new HashSet<CFGEdge>();

			foreach (var node in cfg.Nodes)
			{
				foreach (var successor in node.Successors)
				{
					if (node.Dominators.Contains(successor))
					{
						var edge = new CFGEdge(node, successor);
						backEdges.Add(edge);
					}
				}
			}

			return backEdges;
		}

		private static CFGLoop IdentifyLoop(CFGEdge backEdge)
		{			
			var loop = new CFGLoop(backEdge.Target);
			var nodes = new Stack<CFGNode>();

			nodes.Push(backEdge.Source);

			do
			{
				var node = nodes.Pop();
				var newNode = loop.Body.Add(node);

				if (newNode)
				{
					foreach (var predecessor in node.Predecessors)
						nodes.Push(predecessor);
				}
			}
			while (nodes.Count > 0);

			return loop;
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
