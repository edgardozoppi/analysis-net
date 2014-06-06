using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analysis
{
	public class DataFlowAnalysisResult<T>
	{
		public T Input { get; set; }
		public T Output { get; set; }
	}

	public abstract class DataFlowAnalysis<T>
	{
		protected ControlFlowGraph cfg;
		protected DataFlowAnalysisResult<T>[] result;

		public abstract void Analyze();

		public abstract bool CompareValues(T left, T right);

		public abstract T InitialValue(CFGNode node);

		public abstract T DefaultValue(CFGNode node);

		public abstract T Merge(T left, T right);

		public abstract T Flow(CFGNode node, T input);
	}

	public abstract class ForwardDataFlowAnalysis<T> : DataFlowAnalysis<T>
	{
		public override void Analyze()
		{
			bool changed;
			var nodes = this.cfg.Nodes.ToArray();

			this.result = new DataFlowAnalysisResult<T>[nodes.Length];

			var entryResult = new DataFlowAnalysisResult<T>();
			entryResult.Output = this.InitialValue(cfg.Entry);
			this.result[cfg.Entry.Id] = entryResult;

			for (var i = 0; i < nodes.Length; ++i)
			{
				if (i == cfg.Entry.Id) continue;

				var nodeResult = new DataFlowAnalysisResult<T>();
				var node = nodes[i];

				nodeResult.Output = this.DefaultValue(node);
				this.result[i]  = nodeResult;
			}

			do
			{
				changed = false;

				for (var i = 0; i < nodes.Length; ++i)
				{
					if (i == cfg.Entry.Id) continue;

					var node = nodes[i];
					var nodeResult = this.result[i];
					var nodeInput = this.InitialValue(node);

					foreach (var predecessor in node.Predecessors)
					{
						var predResult = this.result[predecessor.Id];
						nodeInput = this.Merge(nodeInput, predResult.Output);
					}

					nodeResult.Input = nodeInput;

					var oldOutput = nodeResult.Output;
					var newOutput = this.Flow(node, nodeInput);
					var equals = this.CompareValues(newOutput, oldOutput);

					if (!equals)
					{
						nodeResult.Output = newOutput;
						changed = true;
					}
				}
			}
			while (changed);
		}
	}

	public abstract class BackwardDataFlowAnalysis<T> : DataFlowAnalysis<T>
	{
		public override void Analyze()
		{
			bool changed;
			var nodes = cfg.Nodes.ToArray();

			this.result = new DataFlowAnalysisResult<T>[nodes.Length];

			var exitResult = new DataFlowAnalysisResult<T>();
			exitResult.Input = this.InitialValue(cfg.Exit);
			this.result[cfg.Exit.Id] = exitResult;

			for (var i = 0; i < nodes.Length; ++i)
			{
				if (i == cfg.Exit.Id) continue;

				var nodeResult = new DataFlowAnalysisResult<T>();
				var node = nodes[i];

				nodeResult.Input = this.DefaultValue(node);
				this.result[i] = nodeResult;
			}

			do
			{
				changed = false;

				for (var i = 0; i < nodes.Length; ++i)
				{
					if (i == cfg.Exit.Id) continue;

					var node = nodes[i];
					var nodeResult = this.result[i];
					var nodeOutput = this.InitialValue(node);

					foreach (var successor in node.Successors)
					{
						var succResult = this.result[successor.Id];
						nodeOutput = this.Merge(nodeOutput, succResult.Input);
					}

					nodeResult.Output = nodeOutput;

					var oldInput = nodeResult.Input;
					var newInput = this.Flow(node, nodeOutput);
					var equals = newInput.Equals(oldInput);

					if (!equals)
					{
						nodeResult.Input = newInput;
						changed = true;
					}
				}
			}
			while (changed);
		}
	}
}
