using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analysis
{
	public enum DataFlowDirection
	{
		Forward,
		Backward
	}

	public interface IDataFlowAnalysis<T>
	{
		ControlFlowGraph ControlFlowGraph { get; }

		DataFlowDirection Direction { get; }

		T InitialValue { get; }

		T DefaultValue { get; }

		T Meet(T left, T right);

		T Transfer(CFGNode node, T nodeIN);
	}

	public class DataFlowAnalysisResult<T>
	{
		public T[] IN { get; private set; }
		public T[] OUT { get; private set; }

		public DataFlowAnalysisResult(int length)
		{
			this.IN = new T[length];
			this.OUT = new T[length];
		}
	}

	public static class DataFlowAnalyzer
	{
		public static void Analyze<T>(IDataFlowAnalysis<T> analysis)
		{
			switch (analysis.Direction)
			{
				case DataFlowDirection.Forward:
					DataFlowAnalyzer.AnalyzeForward(analysis);
					break;

				case DataFlowDirection.Backward:
					DataFlowAnalyzer.AnalyzeBackward(analysis);
					break;

				default:
					throw new Exception("Invalid data flow analysis direction.");
			}
		}

		private void AnalyzeForward<T>(IDataFlowAnalysis<T> analysis)
		{
			bool changed;
			var nodes = analysis.ControlFlowGraph.Nodes.ToArray();
			var result = new DataFlowAnalysisResult<T>(nodes.Length);

			IN = new T[nodes.Length];
			OUT = new T[nodes.Length];

			OUT[cfg.Entry.Id] = analysis.InitialValue;

			for (var i = 0; i < nodes.Length; ++i)
			{
				if (i == cfg.Entry.Id) continue;
				OUT[i] = analysis.DefaultValue;
			}

			do
			{
				changed = false;

				for (var i = 0; i < nodes.Length; ++i)
				{
					if (i == cfg.Entry.Id) continue;
					var node = nodes[i];
					var nodeIN = analysis.DefaultValue;

					foreach (var predecessor in node.Predecessors)
					{
						var predOUT = OUT[predecessor.Id];
						nodeIN = analysis.Meet(nodeIN, predOUT);
					}

					IN[i] = nodeIN;

					var oldOUT = OUT[i];
					var newOUT = analysis.Transfer(node, nodeIN);
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

		private void AnalyzeBackward<T>(IDataFlowAnalysis<T> analysis)
		{
			bool changed;
			var nodes = cfg.Nodes.ToArray();

			IN = new T[nodes.Length];
			OUT = new T[nodes.Length];

			IN[cfg.Exit.Id] = analysis.InitialValue;

			for (var i = 0; i < nodes.Length; ++i)
			{
				if (i == cfg.Exit.Id) continue;
				IN[i] = analysis.DefaultValue;
			}

			do
			{
				changed = false;

				for (var i = 0; i < nodes.Length; ++i)
				{
					if (i == cfg.Exit.Id) continue;
					var node = nodes[i];
					var nodeOUT = analysis.DefaultValue;

					foreach (var successor in node.Successors)
					{
						var succIN = IN[successor.Id];
						nodeOUT = analysis.Meet(nodeOUT, succIN);
					}

					OUT[i] = nodeOUT;

					var oldIN = IN[i];
					var newIN = analysis.Transfer(node, nodeOUT);
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
