// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using Model.ThreeAddressCode;
using Model.ThreeAddressCode.Instructions;
using Model;
using Model.Types;

namespace Backend.Model
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
		//public IExpression Condition { get; set; }

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
			{
				sb.AppendFormat("B{0} ", node.Id);
			}

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

	public class CFGNode : IInstructionContainer
	{
		private ISet<CFGNode> dominators;
		private ISet<CFGNode> postDominators;

		public int Id { get; private set; }
		public int ForwardIndex { get; set; }
		public int BackwardIndex { get; set; }
		public CFGNodeKind Kind { get; private set; }
		public ISet<CFGNode> Predecessors { get; private set; }
		public ISet<CFGNode> Successors { get; private set; }
		public IList<IInstruction> Instructions { get; private set; }
		public CFGNode ImmediateDominator { get; set; }
		public ISet<CFGNode> ImmediateDominated { get; private set; }
		public ISet<CFGNode> DominanceFrontier { get; private set; }
		public CFGNode ImmediatePostDominator { get; set; }
		public ISet<CFGNode> ImmediatePostDominated { get; private set; }
		public ISet<CFGNode> PostDominanceFrontier { get; private set; }
		public ISet<CFGNode> ImmediateControlDependencies { get; set; }
		public ISet<CFGNode> ImmediateControlDependents { get; private set; }

		public CFGNode(int id, CFGNodeKind kind = CFGNodeKind.BasicBlock)
		{
			this.Id = id;
			this.Kind = kind;
			this.ForwardIndex = -1;
			this.BackwardIndex = -1;
			this.Predecessors = new HashSet<CFGNode>();
			this.Successors = new HashSet<CFGNode>();
			this.Instructions = new List<IInstruction>();
			this.ImmediateDominated = new HashSet<CFGNode>();
			this.DominanceFrontier = new HashSet<CFGNode>();
			this.ImmediatePostDominated = new HashSet<CFGNode>();
			this.PostDominanceFrontier = new HashSet<CFGNode>();
			this.ImmediateControlDependencies = new HashSet<CFGNode>();
			this.ImmediateControlDependents = new HashSet<CFGNode>();
		}

		public ISet<CFGNode> Dominators
		{
			get
			{
				if (this.dominators == null)
				{
					this.dominators = ComputeDominators(this);
				}

				return this.dominators;
			}
		}

		public ISet<CFGNode> PostDominators
		{
			get
			{
				if (this.postDominators == null)
				{
					this.postDominators = ComputePostDominators(this);
				}

				return this.postDominators;
			}
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

		private static ISet<CFGNode> ComputeDominators(CFGNode node)
		{
			var result = new HashSet<CFGNode>();

			do
			{
				result.Add(node);
				node = node.ImmediateDominator;
			}
			while (node != null);

			return result;
		}

		private static ISet<CFGNode> ComputePostDominators(CFGNode node)
		{
			var result = new HashSet<CFGNode>();

			do
			{
				result.Add(node);
				node = node.ImmediatePostDominator;
			}
			while (node != null);

			return result;
		}
	}

	public class ControlFlowGraph
	{
		public const int EntryNodeId = 0;
		public const int ExitNodeId = 1;

		private CFGNode[] forwardOrder;
		private CFGNode[] backwardOrder;

		public CFGNode Entry { get; private set; }
		public CFGNode Exit { get; private set; }
		public ISet<CFGNode> Nodes { get; private set; }
		public ISet<CFGLoop> Loops { get; private set; }

		public ControlFlowGraph()
		{
			this.Entry = new CFGNode(EntryNodeId, CFGNodeKind.Entry);
			this.Exit = new CFGNode(ExitNodeId, CFGNodeKind.Exit);
			this.Nodes = new HashSet<CFGNode>() { this.Entry, this.Exit };
			this.Loops = new HashSet<CFGLoop>();
		}

		public IEnumerable<CFGNode> Entries
		{
			get
			{
				var result = from node in this.Nodes
							 where node.Predecessors.Count == 0
							 select node;

				return result;
			}
		}

		public IEnumerable<CFGNode> Exits
		{
			get
			{
				var result = from node in this.Nodes
							 where node.Successors.Count == 0
							 select node;

				return result;
			}
		}

		public CFGNode[] ForwardOrder
		{
			get
			{
				if (this.forwardOrder == null)
				{
					this.forwardOrder = this.ComputeForwardTopologicalSort();
				}

				return this.forwardOrder;
			}
		}

		public CFGNode[] BackwardOrder
		{
			get
			{
				if (this.backwardOrder == null)
				{
					this.backwardOrder = this.ComputeBackwardTopologicalSort();
				}

				return this.backwardOrder;
			}
		}

		public void ConnectNodes(CFGNode predecessor, CFGNode successor)
		{
			successor.Predecessors.Add(predecessor);
			predecessor.Successors.Add(successor);
			this.Nodes.Add(predecessor);
			this.Nodes.Add(successor);
		}

		#region Topological Sort

		// Tarjan's topological sort recursive algorithm
		private CFGNode[] ComputeForwardTopologicalSort()
		{
			var result = new CFGNode[this.Nodes.Count];
			var visited = new bool[this.Nodes.Count];
			var index = this.Nodes.Count - 1;

			foreach (var node in this.Entries)
			{
				ForwardDFS(result, visited, node, ref index);
			}

			return result;
		}

		// Depth First Search algorithm
		private static void ForwardDFS(CFGNode[] result, bool[] visited, CFGNode node, ref int index)
		{
			var alreadyVisited = visited[node.Id];

			if (!alreadyVisited)
			{
				visited[node.Id] = true;

				foreach (var succ in node.Successors)
				{
					ForwardDFS(result, visited, succ, ref index);
				}

				node.ForwardIndex = index;
				result[index] = node;
				index--;
			}
		}

		// Tarjan's topological sort recursive algorithm
		private CFGNode[] ComputeBackwardTopologicalSort()
		{
			var result = new CFGNode[this.Nodes.Count];
			var visited = new bool[this.Nodes.Count];
			var index = this.Nodes.Count - 1;

			foreach (var node in this.Exits)
			{
				BackwardDFS(result, visited, node, ref index);
			}

			return result;
		}

		// Depth First Search algorithm
		private static void BackwardDFS(CFGNode[] result, bool[] visited, CFGNode node, ref int index)
		{
			var alreadyVisited = visited[node.Id];

			if (!alreadyVisited)
			{
				visited[node.Id] = true;

				foreach (var pred in node.Predecessors)
				{
					BackwardDFS(result, visited, pred, ref index);
				}

				node.BackwardIndex = index;
				result[index] = node;
				index--;
			}
		}

		//// Kahn's topological sort iterative algorithm
		//private CFGNode[] ComputeForwardTopologicalSort()
		//{
		//	// worklist always contains nodes with no incoming edges
		//	var worklist = new Queue<CFGNode>();
		//	var visited = new HashSet<CFGNode>();
		//	var result = new CFGNode[this.Nodes.Count];
		//	var index = 0;

		//	foreach (var node in this.Entries)
		//	{
		//		worklist.Enqueue(node);
		//	}

		//	while (worklist.Count > 0)
		//	{
		//		var node = worklist.Dequeue();
		//		visited.Add(node);

		//		node.ForwardIndex = index;
		//		result[index] = node;
		//		index++;

		//		foreach (var succ in node.Successors)
		//		{
		//			if (visited.Contains(succ) ||
		//				succ.Predecessors.Except(visited).Any())
		//				continue;

		//			// succ can never be already in the worklist
		//			worklist.Enqueue(succ);
		//		}
		//	}

		//	return result;
		//}

		//// Kahn's topological sort iterative algorithm
		//private CFGNode[] ComputeBackwardTopologicalSort()
		//{
		//	// worklist always contains nodes with no outgoing edges
		//	var worklist = new Queue<CFGNode>();
		//	var visited = new HashSet<CFGNode>();
		//	var result = new CFGNode[this.Nodes.Count];
		//	var index = 0;

		//	foreach (var node in this.Exits)
		//	{
		//		worklist.Enqueue(node);
		//	}

		//	while (worklist.Count > 0)
		//	{
		//		var node = worklist.Dequeue();
		//		visited.Add(node);

		//		node.BackwardIndex = index;
		//		result[index] = node;
		//		index++;

		//		foreach (var pred in node.Predecessors)
		//		{
		//			if (visited.Contains(pred) ||
		//				pred.Successors.Except(visited).Any())
		//				continue;

		//			// pred can never be already in the worklist
		//			worklist.Enqueue(pred);
		//		}
		//	}

		//	return result;
		//}

		#endregion
	}
}
