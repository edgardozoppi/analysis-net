// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

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
