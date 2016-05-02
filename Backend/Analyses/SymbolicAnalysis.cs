using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.Utils;
using Model.ThreeAddressCode.Values;
using Model.ThreeAddressCode.Expressions;
using Model.ThreeAddressCode.Instructions;
using Model;
using Backend.Model;

namespace Backend.Analyses
{
	public class SymbolicAnalysis : ForwardDataFlowAnalysis<IDictionary<IVariable, IExpression>>
	{
		private DataFlowAnalysisResult<IDictionary<IVariable, IExpression>>[] result;
		private IDictionary<IVariable, IExpression>[] GEN;
		private ISet<IVariable>[] KILL;

		public SymbolicAnalysis(ControlFlowGraph cfg)
			: base(cfg)
		{
		}

		public override DataFlowAnalysisResult<IDictionary<IVariable, IExpression>>[] Analyze()
		{
			this.ComputeGen();
			this.ComputeKill();

			var result = base.Analyze();
			
			this.result = result;
			this.GEN = null;
			this.KILL = null;

			return result;
		}

		public DataFlowAnalysisResult<IDictionary<IVariable, IExpression>> this[CFGNode node]
		{
			get { return this.result[node.Id]; }
		}

		protected override IDictionary<IVariable, IExpression> InitialValue(CFGNode node)
		{
			return GEN[node.Id];
		}

		protected override bool Compare(IDictionary<IVariable, IExpression> left, IDictionary<IVariable, IExpression> right)
		{
			return left.SequenceEqual(right);
		}

		protected override IDictionary<IVariable, IExpression> Join(IDictionary<IVariable, IExpression> left, IDictionary<IVariable, IExpression> right)
		{
			var result = new Dictionary<IVariable, IExpression>(left);

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

		protected override IDictionary<IVariable, IExpression> Flow(CFGNode node, IDictionary<IVariable, IExpression> input)
		{
			IDictionary<IVariable, IExpression> result;

			if (input == null)
			{
				result = new Dictionary<IVariable, IExpression>();
			}
			else
			{
				result = new Dictionary<IVariable, IExpression>(input);
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
			GEN = new IDictionary<IVariable, IExpression>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var gen = this.Flow(node, null);
				GEN[node.Id] = gen;
			}
		}

		private void ComputeKill()
		{
			KILL = new ISet<IVariable>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var kill = new HashSet<IVariable>();

				foreach (var instruction in node.Instructions)
				{
					kill.UnionWith(instruction.ModifiedVariables);
				}

				KILL[node.Id] = kill;
			}
		}

		private void RemoveEqualitiesWithVariable(IDictionary<IVariable, IExpression> equalities, IVariable variable)
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

		private KeyValuePair<IVariable, IExpression>? Flow(IInstruction instruction, IDictionary<IVariable, IExpression> equalities)
		{
			KeyValuePair<IVariable, IExpression>? result = null;

			if (instruction is LoadInstruction)
			{
				var assignment = instruction as LoadInstruction;
				var expr = assignment.Operand.ToExpression().ReplaceVariables(equalities);
				result = new KeyValuePair<IVariable,IExpression>(assignment.Result, expr);
			}
			//else if (instruction is PhiInstruction)
			//{
			//	var phi = instruction as PhiInstruction;
			//	var arguments = phi.Arguments.Skip(1);
			//	IExpression expr = phi.Arguments.First();

			//	foreach (var arg in arguments)
			//	{
			//		expr = new BinaryExpression(expr, BinaryOperation.Or, arg);
			//	}

			//	result = new KeyValuePair<Variable, IExpression>(phi.Result, expr);
			//}

			return result;
		}
	}
}
