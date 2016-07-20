// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

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

		public int Id { get; private set; }
		public int ForwardIndex { get; set; }
		public int BackwardIndex { get; set; }
		public CFGNodeKind Kind { get; private set; }
		public ISet<CFGNode> Predecessors { get; private set; }
		public ISet<CFGNode> Successors { get; private set; }
		public IList<IInstruction> Instructions { get; private set; }
		public CFGNode ImmediateDominator { get; set; }
		public ISet<CFGNode> Childs { get; private set; }
		public ISet<CFGNode> DominanceFrontier { get; private set; }

		public CFGNode(int id, CFGNodeKind kind = CFGNodeKind.BasicBlock)
		{
			this.Id = id;
			this.Kind = kind;
			this.ForwardIndex = -1;
			this.BackwardIndex = -1;
			this.Predecessors = new HashSet<CFGNode>();
			this.Successors = new HashSet<CFGNode>();
			this.Instructions = new List<IInstruction>();
			this.Childs = new HashSet<CFGNode>();
			this.DominanceFrontier = new HashSet<CFGNode>();
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
	}

	public class ControlFlowGraph
	{
		private CFGNode[] forwardOrder;
		private CFGNode[] backwardOrder;

		public CFGNode Entry { get; private set; }
		public CFGNode Exit { get; private set; }
		public ISet<CFGNode> Nodes { get; private set; }
		public ISet<CFGLoop> Loops { get; private set; }

		public ControlFlowGraph()
		{
			this.Entry = new CFGNode(0, CFGNodeKind.Entry);
			this.Exit = new CFGNode(1, CFGNodeKind.Exit);
			this.Nodes = new HashSet<CFGNode>() { this.Entry, this.Exit };
			this.Loops = new HashSet<CFGLoop>();
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

		//private static CFGNode[] ComputeForwardTopologicalSort(ControlFlowGraph cfg)
		//{
		//    var result = new CFGNode[cfg.Nodes.Count];
		//    var visited = new bool[cfg.Nodes.Count];
		//    var index = cfg.Nodes.Count - 1;

		//    ControlFlowGraph.DepthFirstSearch(result, visited, cfg.Entry, ref index);

		//    //if (result.Any(n => n == null))
		//    //{
		//    //    var nodes = cfg.Nodes.Where(n => n.Predecessors.Count == 0);

		//    //    throw new Exception("Error");
		//    //}

		//    return result;
		//}

		//private static void DepthFirstSearch(CFGNode[] result, bool[] visited, CFGNode node, ref int index)
		//{
		//    var alreadyVisited = visited[node.Id];

		//    if (!alreadyVisited)
		//    {
		//        visited[node.Id] = true;

		//        foreach (var succ in node.Successors)
		//        {
		//            ControlFlowGraph.DepthFirstSearch(result, visited, succ, ref index);
		//        }

		//        node.ForwardIndex = index;
		//        result[index] = node;
		//        index--;
		//    }
		//}

		private enum TopologicalSortNodeStatus
		{
			NeverVisited, // never pushed into stack
			FirstVisit, // pushed into stack for the first time
			SecondVisit // pushed into stack for the second time
		}

		private CFGNode[] ComputeForwardTopologicalSort()
		{
			// reverse postorder traversal from entry node
			var stack = new Stack<CFGNode>();
			var result = new CFGNode[this.Nodes.Count];
			var status = new TopologicalSortNodeStatus[this.Nodes.Count];
			var index = this.Nodes.Count - 1;

			stack.Push(this.Entry);
			status[this.Entry.Id] = TopologicalSortNodeStatus.FirstVisit;

			do
			{
				var node = stack.Peek();
				var node_status = status[node.Id];

				if (node_status == TopologicalSortNodeStatus.FirstVisit)
				{
					status[node.Id] = TopologicalSortNodeStatus.SecondVisit;

					foreach (var succ in node.Successors)
					{
						if (status[succ.Id] == 0)
						{
							stack.Push(succ);
							status[succ.Id] = TopologicalSortNodeStatus.FirstVisit;
						}
					}
				}
				else if (node_status == TopologicalSortNodeStatus.SecondVisit)
				{
					stack.Pop();
					node.ForwardIndex = index;
					result[index] = node;
					index--;
				}
			}
			while (stack.Count > 0);

			//if (result.Any(n => n == null))
			//{
			//    var nodes = cfg.Nodes.Where(n => n.Predecessors.Count == 0);

			//    throw new Exception("Error");
			//}

			return result;
		}

		private CFGNode[] ComputeBackwardTopologicalSort()
		{
			// reverse postorder traversal from exit node
			var stack = new Stack<CFGNode>();
			var result = new CFGNode[this.Nodes.Count];
			var status = new TopologicalSortNodeStatus[this.Nodes.Count];
			var index = this.Nodes.Count - 1;

			stack.Push(this.Exit);
			status[this.Exit.Id] = TopologicalSortNodeStatus.FirstVisit;

			do
			{
				var node = stack.Peek();
				var node_status = status[node.Id];

				if (node_status == TopologicalSortNodeStatus.FirstVisit)
				{
					status[node.Id] = TopologicalSortNodeStatus.SecondVisit;

					foreach (var pred in node.Predecessors)
					{
						if (status[pred.Id] == 0)
						{
							stack.Push(pred);
							status[pred.Id] = TopologicalSortNodeStatus.FirstVisit;
						}
					}
				}
				else if (node_status == TopologicalSortNodeStatus.SecondVisit)
				{
					stack.Pop();
					node.BackwardIndex = index;
					result[index] = node;
					index--;
				}
			}
			while (stack.Count > 0);

			//if (result.Any(n => n == null))
			//{
			//    var nodes = cfg.Nodes.Where(n => n.Predecessors.Count == 0);

			//    throw new Exception("Error");
			//}

			return result;
		}

		#endregion
	}
}
