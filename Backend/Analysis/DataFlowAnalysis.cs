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
			var sortedNodes = this.cfg.ForwardTopologicalSort();

			this.result = new DataFlowAnalysisResult<T>[sortedNodes.Length];

			var entryResult = new DataFlowAnalysisResult<T>();
			entryResult.Output = this.InitialValue(cfg.Entry);
			this.result[cfg.Entry.Id] = entryResult;

			// Skip first node: entry
			for (var i = 1; i < sortedNodes.Length; ++i)
			{
				var nodeResult = new DataFlowAnalysisResult<T>();
				var node = sortedNodes[i];

				nodeResult.Output = this.DefaultValue(node);
				this.result[node.Id]  = nodeResult;
			}

			do
			{
				changed = false;

				// Skip first node: entry
				for (var i = 1; i < sortedNodes.Length; ++i)
				{
					var node = sortedNodes[i];
					var nodeResult = this.result[node.Id];
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
			var nodes = cfg.BackwardTopologicalSort();

			this.result = new DataFlowAnalysisResult<T>[nodes.Length];

			var exitResult = new DataFlowAnalysisResult<T>();
			exitResult.Input = this.InitialValue(cfg.Exit);
			this.result[cfg.Exit.Id] = exitResult;

			// Skip first node: exit
			for (var i = 1; i < nodes.Length; ++i)
			{
				var nodeResult = new DataFlowAnalysisResult<T>();
				var node = nodes[i];

				nodeResult.Input = this.DefaultValue(node);
				this.result[node.Id] = nodeResult;
			}

			do
			{
				changed = false;

				// Skip first node: exit
				for (var i = 1; i < nodes.Length; ++i)
				{
					var node = nodes[i];
					var nodeResult = this.result[node.Id];
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
