// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analyses
{
	public class DataFlowAnalysisResult<T>
	{
		public T Input { get; set; }
		public T Output { get; set; }
	}

	public abstract class DataFlowAnalysis<T>
	{
		protected ControlFlowGraph cfg;

		public DataFlowAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public abstract DataFlowAnalysisResult<T>[] Analyze();

		protected abstract T InitialValue(CFGNode node);

		protected abstract bool Compare(T left, T right);

		protected abstract T Join(T left, T right);

		protected abstract T Flow(CFGNode node, T input);
	}

	public abstract class ForwardDataFlowAnalysis<T> : DataFlowAnalysis<T>
	{
		public ForwardDataFlowAnalysis(ControlFlowGraph cfg)
			: base(cfg)
		{
		}

		public override DataFlowAnalysisResult<T>[] Analyze()
		{
			var sorted_nodes = this.cfg.ForwardOrder;
			var pending_nodes = new Queue<CFGNode>();
			var result = new DataFlowAnalysisResult<T>[sorted_nodes.Length];

			for (var i = 0; i < sorted_nodes.Length; ++i)
			{
				var node = sorted_nodes[i];
				var node_result = new DataFlowAnalysisResult<T>();

				node_result.Output = this.InitialValue(node);
				result[node.Id] = node_result;

				if (node.Predecessors.Count > 0)
				{
					pending_nodes.Enqueue(node);
				}
			}

			while (pending_nodes.Count > 0)
			{
				var node = pending_nodes.Dequeue();
				var node_result = result[node.Id];

				//if (node.Predecessors.Count > 0)
				{
					var first_pred = node.Predecessors.First();
					var other_predecessors = node.Predecessors.Skip(1);
					var pred_result = result[first_pred.Id];
					var node_input = pred_result.Output;

					foreach (var pred in other_predecessors)
					{
						pred_result = result[pred.Id];
						node_input = this.Join(node_input, pred_result.Output);
					}

					node_result.Input = node_input;
				}				

				var old_output = node_result.Output;
				var new_output = this.Flow(node, node_result.Input);
				var equals = this.Compare(new_output, old_output);

				if (!equals)
				{
					node_result.Output = new_output;

					foreach (var succ in node.Successors)
					{
						if (pending_nodes.Contains(succ)) continue;
						pending_nodes.Enqueue(succ);
					}
				}
			}

			return result;
		}
	}

	public abstract class BackwardDataFlowAnalysis<T> : DataFlowAnalysis<T>
	{
		public BackwardDataFlowAnalysis(ControlFlowGraph cfg)
			: base(cfg)
		{
		}

		public override DataFlowAnalysisResult<T>[] Analyze()
		{
			var sorted_nodes = this.cfg.BackwardOrder;
			var pending_nodes = new Queue<CFGNode>();
			var result = new DataFlowAnalysisResult<T>[sorted_nodes.Length];

			for (var i = 0; i < sorted_nodes.Length; ++i)
			{
				var node = sorted_nodes[i];
				var node_result = new DataFlowAnalysisResult<T>();

				node_result.Input = this.InitialValue(node);
				result[node.Id] = node_result;

				if (node.Successors.Count > 0)
				{
					pending_nodes.Enqueue(node);
				}
			}

			while (pending_nodes.Count > 0)
			{
				var node = pending_nodes.Dequeue();
				var node_result = result[node.Id];

				//if (node.Successors.Count > 0)
				{
					var first_succ = node.Successors.First();
					var other_successors = node.Successors.Skip(1);
					var succ_result = result[first_succ.Id];
					var node_output = succ_result.Input;

					foreach (var succ in other_successors)
					{
						succ_result = result[succ.Id];
						node_output = this.Join(node_output, succ_result.Input);
					}

					node_result.Output = node_output;
				}

				var old_input = node_result.Input;
				var new_input = this.Flow(node, node_result.Output);
				var equals = this.Compare(new_input, old_input);

				if (!equals)
				{
					node_result.Input = new_input;

					foreach (var pred in node.Predecessors)
					{
						if (pending_nodes.Contains(pred)) continue;
						pending_nodes.Enqueue(pred);
					}
				}
			}

			return result;
		}
	}
}
