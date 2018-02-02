// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class PostDominanceAnalysis
	{
		private ControlFlowGraph cfg;

		public PostDominanceAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		// Implementation of the algorithm presented in the paper:
		// "A simple, fast dominance algorithm."
		// Cooper, Keith D., Timothy J. Harvey, and Ken Kennedy.
		public ControlFlowGraph Analyze()
		{
			bool changed;
			var sorted_nodes = cfg.BackwardOrder;

			cfg.Exit.ImmediatePostDominator = cfg.Exit;

			do
			{
				changed = false;

				// Skip first node: exit
				for (var i = 1; i < sorted_nodes.Length; ++i)
				{
					var node = sorted_nodes[i];
					var successors = node.Successors.Where(p => p.ImmediatePostDominator != null);
					var new_idom = successors.First();
					successors = successors.Skip(1);

					foreach (var succ in successors)
					{
						new_idom = FindCommonAncestor(succ, new_idom);
					}

					var old_idom = node.ImmediatePostDominator;
					var equals = old_idom != null && old_idom.Equals(new_idom);

					if (!equals)
					{
						node.ImmediatePostDominator = new_idom;
						changed = true;
					}
				}
			}
			while (changed);

			cfg.Exit.ImmediatePostDominator = null;
			return cfg;
		}

		private static CFGNode FindCommonAncestor(CFGNode a, CFGNode b)
		{
			while (a.BackwardIndex != b.BackwardIndex)
			{
				while (a.BackwardIndex > b.BackwardIndex)
					a = a.ImmediatePostDominator;

				while (b.BackwardIndex > a.BackwardIndex)
					b = b.ImmediatePostDominator;
			}

			return a;
		}

		public ControlFlowGraph GeneratePostDominanceTree()
		{
			foreach (var node in cfg.Nodes)
			{
				node.ImmediatePostDominated.Clear();
			}

			foreach (var node in cfg.Nodes)
			{
				if (node.ImmediatePostDominator == null) continue;

				node.ImmediatePostDominator.ImmediatePostDominated.Add(node);
			}

			return cfg;
		}
	}
}
