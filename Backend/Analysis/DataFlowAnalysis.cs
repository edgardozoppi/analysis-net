using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analysis
{
	public abstract class DataFlowAnalysis<T>
	{
		protected ControlFlowGraph cfg;
		protected T[] IN;
		protected T[] OUT;

		public abstract void Analyze();

		public abstract T Meet(T left, T right);

		public abstract T DefaultValue(CFGNode node);

		public abstract T Transfer(CFGNode node, T nodeIN);
	}

	public abstract class ForwardDataFlowAnalysis<T> : DataFlowAnalysis<T>
	{
		public abstract T EntryInitialValue { get; }

		public override void Analyze()
		{
			bool changed;
			var nodes = this.cfg.Nodes.ToArray();

			IN = new T[nodes.Length];
			OUT = new T[nodes.Length];

			OUT[cfg.Entry.Id] = this.EntryInitialValue;

			for (var i = 0; i < nodes.Length; ++i)
			{
				if (i == cfg.Entry.Id) continue;
				var node = nodes[i];
				OUT[i] = this.DefaultValue(node);
			}

			do
			{
				changed = false;

				for (var i = 0; i < nodes.Length; ++i)
				{
					if (i == cfg.Entry.Id) continue;
					var node = nodes[i];
					var nodeIN = this.DefaultValue(node);

					foreach (var predecessor in node.Predecessors)
					{
						var predOUT = OUT[predecessor.Id];
						nodeIN = this.Meet(nodeIN, predOUT);
					}

					IN[i] = nodeIN;

					var oldOUT = OUT[i];
					var newOUT = this.Transfer(node, nodeIN);
					var equals = newOUT.Equals(oldOUT);

					if (!equals)
					{
						OUT[i] = newOUT;
						changed = true;
					}
				}
			}
			while (changed);
		}
	}

	public abstract class BackwardDataFlowAnalysis<T> : DataFlowAnalysis<T>
	{
		public abstract T ExitInitialValue { get; }

		public override void Analyze()
		{
			bool changed;
			var nodes = cfg.Nodes.ToArray();

			IN = new T[nodes.Length];
			OUT = new T[nodes.Length];

			IN[cfg.Exit.Id] = this.ExitInitialValue;

			for (var i = 0; i < nodes.Length; ++i)
			{
				if (i == cfg.Exit.Id) continue;
				IN[i] = this.DefaultValue;
			}

			do
			{
				changed = false;

				for (var i = 0; i < nodes.Length; ++i)
				{
					if (i == cfg.Exit.Id) continue;
					var node = nodes[i];
					var nodeOUT = this.DefaultValue;

					foreach (var successor in node.Successors)
					{
						var succIN = IN[successor.Id];
						nodeOUT = this.Meet(nodeOUT, succIN);
					}

					OUT[i] = nodeOUT;

					var oldIN = IN[i];
					var newIN = this.Transfer(node, nodeOUT);
					var equals = newIN.Equals(oldIN);

					if (!equals)
					{
						IN[i] = newIN;
						changed = true;
					}
				}
			}
			while (changed);
		}
	}
}
