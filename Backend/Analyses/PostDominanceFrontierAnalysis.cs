// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

using Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class PostDominanceFrontierAnalysis
	{
		private ControlFlowGraph cfg;

		public PostDominanceFrontierAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		// Implementation of the algorithm presented in the paper:
		// "A simple, fast dominance algorithm."
		// Cooper, Keith D., Timothy J. Harvey, and Ken Kennedy.
		public ControlFlowGraph Analyze()
		{
			foreach (var node in cfg.Nodes)
			{
				node.PostDominanceFrontier.Clear();
			}

			foreach (var node in cfg.Nodes)
			{
				if (node.Successors.Count < 2) continue;

				foreach (var succ in node.Successors)
				{
					var runner = succ;

					while (runner != null &&
						   node.ImmediatePostDominator != null &&
						   runner.Id != node.ImmediatePostDominator.Id)
					{
						runner.PostDominanceFrontier.Add(node);
						runner = runner.ImmediatePostDominator;
					}
				}
			}

			return cfg;
		}
	}
}
