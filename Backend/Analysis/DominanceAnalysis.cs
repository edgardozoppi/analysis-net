using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analysis
{
	public class DominanceAnalysis
	{
		private ControlFlowGraph cfg;

		public DominanceAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public ControlFlowGraph Analyze()
		{
			bool changed;
			var sorted_nodes = cfg.ForwardOrder;

			cfg.Entry.ImmediateDominator = cfg.Entry;

			do
			{
				changed = false;

				// Skip first node: entry
				for (var i = 1; i < sorted_nodes.Length; ++i)
				{
					var node = sorted_nodes[i];
					var predecessors = node.Predecessors.Where(p => p.ImmediateDominator != null);
					var new_idom = predecessors.First();
					predecessors = predecessors.Skip(1);

					foreach (var pred in predecessors)
					{
						new_idom = FindCommonAncestor(pred, new_idom);
					}

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

			cfg.Entry.ImmediateDominator = null;
			return cfg;
		}

		private static CFGNode FindCommonAncestor(CFGNode a, CFGNode b)
		{
			while (a.ForwardIndex != b.ForwardIndex)
			{
				while (a.ForwardIndex > b.ForwardIndex)
					a = a.ImmediateDominator;

				while (b.ForwardIndex > a.ForwardIndex)
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
				if (node.ImmediateDominator == null) continue;

				node.ImmediateDominator.Childs.Add(node);
			}

			return cfg;
		}
	}
}
