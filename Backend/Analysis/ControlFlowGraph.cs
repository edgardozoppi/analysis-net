using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using Backend.ThreeAddressCode;
using Backend.Utils;

namespace Backend.Analysis
{
	public enum CFGNodeKind
	{
		Entry,
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
				case CFGNodeKind.Entry: result = "entry"; break;
				case CFGNodeKind.Exit: result = "exit"; break;
				default: result = string.Join("\n", this.Instructions); break;
			}

			return result;
		}
	}

	public class ControlFlowGraph
	{
		public CFGNode Entry { get; private set; }
		public CFGNode Exit { get; private set; }
		public ISet<CFGNode> Nodes { get; private set; }
		public ISet<CFGLoop> Loops { get; private set; }

		public ControlFlowGraph()
		{
			this.Entry = new CFGNode(0, CFGNodeKind.Entry);
			this.Exit = new CFGNode(1, CFGNodeKind.Exit);
			this.Nodes = new HashSet<CFGNode>() { this.Entry, this.Exit };
		}

		#region Generation

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
			var current = cfg.Entry;
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

		#region Topological Sort

		public CFGNode[] ForwardTopologicalSort()
		{
			var stack = new Stack<CFGNode>();
			var result = new CFGNode[this.Nodes.Count];
			var visited = new bool[this.Nodes.Count];
			var index = this.Nodes.Count - 1;

			stack.Push(this.Entry);

			do
			{
				var node = stack.Peek();

				if (!visited[node.Id])
				{
					visited[node.Id] = true;

					foreach (var successor in node.Successors)
					{
						if (!visited[successor.Id])
						{
							stack.Push(successor);
						}
					}
				}
				else
				{
					stack.Pop();
					result[index] = node;
					index--;
				}
			}
			while (stack.Count > 0);

			return result;
		}

		public CFGNode[] BackwardTopologicalSort()
		{
			var stack = new Stack<CFGNode>();
			var result = new CFGNode[this.Nodes.Count];
			var visited = new bool[this.Nodes.Count];
			var index = this.Nodes.Count - 1;

			stack.Push(this.Exit);

			do
			{
				var node = stack.Peek();

				if (!visited[node.Id])
				{
					visited[node.Id] = true;

					foreach (var predecessor in node.Predecessors)
					{
						if (!visited[predecessor.Id])
						{
							stack.Push(predecessor);
						}
					}
				}
				else
				{
					stack.Pop();
					result[index] = node;
					index--;
				}
			}
			while (stack.Count > 0);

			return result;
		}

		#endregion

		#region Dominators

		public static void ComputeDominators(ControlFlowGraph cfg)
		{
			var nodes = cfg.Nodes.ToArray();
			var dominators = new Subset<CFGNode>[nodes.Length];

			var enterDominators = nodes.ToEmptySubset();
			enterDominators.Add(cfg.Entry.Id);
			dominators[cfg.Entry.Id] = enterDominators;

			// Skip first node (entry)
			for (var i = 1; i < nodes.Length; ++i)
			{
				dominators[i] = nodes.ToSubset();
			}

			bool changed;

			do
			{
				changed = false;

				// Skip first node (entry)
				for (var i = 1; i < nodes.Length; ++i)
				{
					var node = nodes[i];
					var oldDominators = dominators[i];
					var newDominators = nodes.ToSubset();

					foreach (var predecessor in node.Predecessors)
					{
						var preDominators = dominators[predecessor.Id];
						newDominators.Intersect(preDominators);
					}

					newDominators.Add(i);					
					var equals = oldDominators.Equals(newDominators);

					if (!equals)
					{
						dominators[i] = newDominators;
						changed = true;
					}
				}
			}
			while (changed);

			for (var i = 0; i < nodes.Length; ++i)
			{
				var node = nodes[i];
				var nodeDominators = dominators[i];

				nodeDominators.ToSet(node.Dominators);
			}
		}

		#endregion

		#region Loops

		public static void IdentifyLoops(ControlFlowGraph cfg)
		{
			cfg.Loops = new HashSet<CFGLoop>();
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
