// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

using Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class ControlDependenceAnalysis
	{
		private ControlFlowGraph cfg;

		public ControlDependenceAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		// Implementation of the algorithm presented in the paper:
		// "Efficiently computing static single assignment form and the control dependence graph."
		// Cytron, Ron, Jeanne Ferrante, Barry K. Rosen, Mark N. Wegman, and F. Kenneth Zadeck.
		public ControlFlowGraph Analyze()
		{
			foreach (var node in cfg.Nodes)
			{
				node.ImmediateControlDependencies.Clear();
				node.ImmediateControlDependents.Clear();
			}

			foreach (var dependent in cfg.Nodes)
			{
				var connectWithEntry = false;

				if (dependent.PostDominanceFrontier.Count == 0 &&
					dependent.Kind != CFGNodeKind.Entry)
				{
					// Non Entry node with no dependencies
					connectWithEntry = true;
				}
				else if (dependent.PostDominanceFrontier.Count == 1)
				{
					var dependency = dependent.PostDominanceFrontier.Single();

					if (dependent == dependency)
					{
						// Self loop
						connectWithEntry = true;
					}
				}

				if (dependent.PostDominanceFrontier.Count > 0)
				{
					foreach (var dependency in dependent.PostDominanceFrontier)
					{
						dependency.ImmediateControlDependents.Add(dependent);
						dependent.ImmediateControlDependencies.Add(dependency);
					}
				}

				if (connectWithEntry)
				{
					cfg.Entry.ImmediateControlDependents.Add(dependent);
					dependent.ImmediateControlDependencies.Add(cfg.Entry);
				}
			}

			return cfg;
		}
	}
}
