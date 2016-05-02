using Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class DominanceFrontierAnalysis
	{
		private ControlFlowGraph cfg;

		public DominanceFrontierAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public ControlFlowGraph Analyze()
		{
			foreach (var node in cfg.Nodes)
			{
				node.DominanceFrontier.Clear();
			}

			foreach (var node in cfg.Nodes)
			{
				if (node.Predecessors.Count < 2) continue;

				foreach (var pred in node.Predecessors)
				{
					var runner = pred;

					while (runner.Id != node.ImmediateDominator.Id)
					{
						runner.DominanceFrontier.Add(node);
						runner = runner.ImmediateDominator;
					}
				}
			}

			return cfg;
		}
	}
}
