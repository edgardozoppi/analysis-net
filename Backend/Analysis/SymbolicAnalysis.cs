using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Backend.ThreeAddressCode;
using Backend.Utils;

namespace Backend.Analysis
{
	public class SymbolicAnalysis : ForwardDataFlowAnalysis<IDictionary<Variable, IExpression>>
	{
		private IDictionary<Variable, IExpression>[] GEN;
		private ISet<Variable>[] KILL;

		public SymbolicAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
			this.ComputeGen();
			this.ComputeKill();
		}

		public DataFlowAnalysisResult<IDictionary<Variable, IExpression>> this[CFGNode node]
		{
			get { return this.result[node.Id]; }
		}

		protected override IDictionary<Variable, IExpression> InitialValue(CFGNode node)
		{
			return new Dictionary<Variable, IExpression>();
		}

		protected override IDictionary<Variable, IExpression> DefaultValue(CFGNode node)
		{
			return GEN[node.Id];
		}

		protected override bool CompareValues(IDictionary<Variable, IExpression> left, IDictionary<Variable, IExpression> right)
		{
			return left.SequenceEqual(right);
		}

		protected override IDictionary<Variable, IExpression> MergeValues(IDictionary<Variable, IExpression> left, IDictionary<Variable, IExpression> right)
		{
			var result = new Dictionary<Variable, IExpression>(left);

			foreach (var equality in right)
			{
				var variable = equality.Key;
				var rightExpr = equality.Value;

				if (left.ContainsKey(variable))
				{
					var leftExpr = left[variable];

					if (!leftExpr.Equals(rightExpr))
					{
						result[variable] = UnknownValue.Value;
					}
				}
				else
				{
					result[variable] = rightExpr;
				}
			}

			return result;
		}

		protected override IDictionary<Variable, IExpression> Flow(CFGNode node, IDictionary<Variable, IExpression> input)
		{
			IDictionary<Variable, IExpression> result;

			if (input == null)
			{
				result = new Dictionary<Variable, IExpression>();
			}
			else
			{
				result = new Dictionary<Variable, IExpression>(input);
			}

			foreach (var instruction in node.Instructions)
			{
				var equality = this.Flow(instruction, result);

				foreach (var variable in instruction.ModifiedVariables)
				{
					this.RemoveEqualitiesWithVariable(result, variable);
				}

				if (equality.HasValue)
				{
					result.Add(equality.Value);
				}
			}

			return result;
		}

		private void ComputeGen()
		{
			GEN = new IDictionary<Variable, IExpression>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var gen = this.Flow(node, null);
				GEN[node.Id] = gen;
			}
		}

		private void ComputeKill()
		{
			KILL = new ISet<Variable>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var kill = new HashSet<Variable>();

				foreach (var instruction in node.Instructions)
				{
					kill.UnionWith(instruction.ModifiedVariables);
				}

				KILL[node.Id] = kill;
			}
		}

		private void RemoveEqualitiesWithVariable(IDictionary<Variable, IExpression> equalities, Variable variable)
		{
			var array = equalities.ToArray();

			foreach (var equality in array)
			{
				if (equality.Key == variable ||
					equality.Value == variable)
				{
					equalities.Remove(equality);
				}
			}
		}

		private KeyValuePair<Variable, IExpression>? Flow(Instruction instruction, IDictionary<Variable, IExpression> equalities)
		{
			KeyValuePair<Variable, IExpression>? result = null;

			if (instruction is ExpressionInstruction)
			{
				var assignment = instruction as ExpressionInstruction;
				var expr = assignment.Value.ReplaceVariables(equalities);
				result = new KeyValuePair<Variable,IExpression>(assignment.Result, expr);
			}

			return result;
		}
	}
}
