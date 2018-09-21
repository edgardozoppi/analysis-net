// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

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

            do
			{
				changed = false;

				for (var i = 0; i < sorted_nodes.Length; ++i)
				{
					var node = sorted_nodes[i];
                    if (node.Successors.Count < 1) continue;

					var new_idom = node.Successors.First();
					var successors = node.Successors.Skip(1);

					foreach (var succ in successors)
					{
						new_idom = FindCommonAncestor(succ, new_idom);
                        if (new_idom == null) break;
                    }

                    if (new_idom == null) continue;
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

            return cfg;
		}

		private static CFGNode FindCommonAncestor(CFGNode a, CFGNode b)
		{
			while (a.BackwardIndex != b.BackwardIndex)
			{
                if (a.ImmediatePostDominator == null &&
                    b.ImmediatePostDominator == null)
                {
                    a = null;
                    break;
                }

                while (a.BackwardIndex > b.BackwardIndex && a.ImmediatePostDominator != null)
                    a = a.ImmediatePostDominator;

				while (b.BackwardIndex > a.BackwardIndex && b.ImmediatePostDominator != null)
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
