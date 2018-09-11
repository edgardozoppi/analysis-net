// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class DominanceAnalysis
	{
		private ControlFlowGraph cfg;

		public DominanceAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		// Implementation of the algorithm presented in the paper:
		// "A simple, fast dominance algorithm."
		// Cooper, Keith D., Timothy J. Harvey, and Ken Kennedy.
		public ControlFlowGraph Analyze()
		{
			bool changed;
			var sorted_nodes = cfg.ForwardOrder;

			do
			{
				changed = false;

				for (var i = 0; i < sorted_nodes.Length; ++i)
				{
					var node = sorted_nodes[i];
                    if (node.Predecessors.Count < 1) continue;

					var new_idom = node.Predecessors.First();
					var predecessors = node.Predecessors.Skip(1);

					foreach (var pred in predecessors)
					{
						new_idom = FindCommonAncestor(pred, new_idom);
                        if (new_idom == null) break;
					}

                    if (new_idom == null) continue;
					var old_idom = node.ImmediateDominator;
					var equals = old_idom != null && old_idom.Equals(new_idom);

					if (!equals)
					{
						node.ImmediateDominator = new_idom;
						changed = true;
					}
				}
			}
			while (changed);

            return cfg;
		}

		private static CFGNode FindCommonAncestor(CFGNode a, CFGNode b)
		{
			while (a.ForwardIndex != b.ForwardIndex)
			{
                if (a.ImmediateDominator == null &&
                    b.ImmediateDominator == null)
                {
                    a = null;
                    break;
                }

                while (a.ForwardIndex > b.ForwardIndex && a.ImmediateDominator != null)
					a = a.ImmediateDominator;

				while (b.ForwardIndex > a.ForwardIndex && b.ImmediateDominator != null)
					b = b.ImmediateDominator;
			}

			return a;
		}

		// Alternative implementation
		//public ControlFlowGraph Analyze()
		//{
		//    bool changed;
		//    var sorted_nodes = cfg.ForwardOrder;
		//    var result = new Subset<CFGNode>[sorted_nodes.Length];

		//    var entry_dom = sorted_nodes.ToEmptySubset();
		//    entry_dom.Add(cfg.Entry.Id);
		//    result[cfg.Entry.Id] = entry_dom;

		//    // Skip first node: entry
		//    for (var i = 1; i < sorted_nodes.Length; ++i)
		//    {
		//        result[i] = sorted_nodes.ToSubset();
		//    }			

		//    do
		//    {
		//        changed = false;

		//        // Skip first node: entry
		//        for (var i = 1; i < sorted_nodes.Length; ++i)
		//        {
		//            var node = sorted_nodes[i];
		//            var old_dom = result[node.Id];
		//            var new_dom = sorted_nodes.ToSubset();

		//            foreach (var pred in node.Predecessors)
		//            {
		//                var pre_dom = result[pred.Id];
		//                new_dom.Intersect(pre_dom);
		//            }

		//            new_dom.Add(node.Id);
		//            var equals = old_dom.Equals(new_dom);

		//            if (!equals)
		//            {
		//                result[node.Id] = new_dom;
		//                changed = true;
		//            }
		//        }
		//    }
		//    while (changed);

		//    for (var i = 0; i < sorted_nodes.Length; ++i)
		//    {
		//        var node = sorted_nodes[i];
		//        var node_dom = result[node.Id];

		//        node_dom.ToSet(node.Dominators);
		//    }

		//    return cfg;
		//}

		public ControlFlowGraph GenerateDominanceTree()
		{
			foreach (var node in cfg.Nodes)
			{
				node.ImmediateDominated.Clear();
			}

			foreach (var node in cfg.Nodes)
			{
				if (node.ImmediateDominator == null) continue;

				node.ImmediateDominator.ImmediateDominated.Add(node);
			}

			return cfg;
		}
	}
}
