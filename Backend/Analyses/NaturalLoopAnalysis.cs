using Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class NaturalLoopAnalysis
	{
		private ControlFlowGraph cfg;

		public NaturalLoopAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public ISet<CFGLoop> Analyze()
		{
			cfg.Loops.Clear();
			var back_edges = this.IdentifyBackEdges();

			foreach (var edge in back_edges)
			{
				var loop = IdentifyLoop(edge);
				cfg.Loops.Add(loop);
			}

			return cfg.Loops;
		}

		private ISet<CFGEdge> IdentifyBackEdges()
		{
			var back_edges = new HashSet<CFGEdge>();

			foreach (var node in cfg.Nodes)
			{
				foreach (var succ in node.Successors)
				{
					if (node.Dominators.Contains(succ))
					{
						var edge = new CFGEdge(node, succ);
						back_edges.Add(edge);
					}
				}
			}

			return back_edges;
		}

		private static CFGLoop IdentifyLoop(CFGEdge back_edge)
		{
			var loop = new CFGLoop(back_edge.Target);
			var nodes = new Stack<CFGNode>();

			nodes.Push(back_edge.Source);

			do
			{
				var node = nodes.Pop();
				var new_node = loop.Body.Add(node);

				if (new_node)
				{
					foreach (var pred in node.Predecessors)
					{
						nodes.Push(pred);
					}
				}
			}
			while (nodes.Count > 0);

			return loop;
		}
	}
}
