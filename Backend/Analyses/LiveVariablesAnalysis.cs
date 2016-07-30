// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;
using Model;
using Backend.Model;

namespace Backend.Analyses
{
	public class LiveVariablesAnalysis : BackwardDataFlowAnalysis<Subset<IVariable>>
	{
		private IVariable[] variables;
		private IDictionary<IVariable, int> variablesIndex;
		private DataFlowAnalysisResult<Subset<IVariable>>[] result;
		private Subset<IVariable>[] GEN;
		private Subset<IVariable>[] KILL;

		public LiveVariablesAnalysis(ControlFlowGraph cfg)
			: base(cfg)
		{
		}

		public DataFlowAnalysisResult<Subset<IVariable>>[] Result
		{
			get { return result; }
		}

		public override DataFlowAnalysisResult<Subset<IVariable>>[] Analyze()
		{
			this.ComputeVariables();
			this.ComputeGen();
			this.ComputeKill();

			var result = base.Analyze();

			this.result = result;
			this.variables = null;
			this.variablesIndex = null;
			this.GEN = null;
			this.KILL = null;

			return result;
		}

		protected override Subset<IVariable> InitialValue(CFGNode node)
		{
			return GEN[node.Id];
		}

		protected override bool Compare(Subset<IVariable> left, Subset<IVariable> right)
		{
			return left.Equals(right);
		}

		protected override Subset<IVariable> Join(Subset<IVariable> left, Subset<IVariable> right)
		{
			var result = left.Clone();
			result.Union(right);
			return result;
		}

		protected override Subset<IVariable> Flow(CFGNode node, Subset<IVariable> input)
		{
			var output = input.Clone();
			var kill = KILL[node.Id];
			var gen = GEN[node.Id];

			var successors = node.Successors.ToArray();

			for (var i = 0; i < successors.Length; ++i)
			{
				var successor = successors[i];
				var derivedVariables = GetDerivedVariables(successor, i);

				output.Except(derivedVariables);
			}

			output.Except(kill);
			output.Union(gen);
			return output;
		}

		private Subset<IVariable> GetDerivedVariables(CFGNode successor, int successorIndex)
		{
			var result = variables.ToEmptySubset();
			var phiInstructions = successor.Instructions.OfType<PhiInstruction>();

			foreach (var phi in phiInstructions)
			{
				var argument = phi.Arguments[successorIndex];
				var argumentIndex = variablesIndex[argument];

				result.Add(argumentIndex);
			}

			return result;
		}

		private void ComputeVariables()
		{
			variables = cfg.GetVariables().ToArray();
			variablesIndex = new Dictionary<IVariable, int>();

			for (var i = 0; i < variables.Length; ++i)
			{
				var variable = variables[i];
				variablesIndex.Add(variable, i);
			}
		}

		private void ComputeGen()
		{
			GEN = new Subset<IVariable>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				var gen = this.variables.ToEmptySubset();

				for (var i = node.Instructions.Count - 1; i >= 0; --i)
				{
					var instruction = node.Instructions[i];
					var modifiedVariables = instruction.ModifiedVariables;
					var usedVariables = instruction.UsedVariables;

					foreach (var variable in modifiedVariables)
					{
						var index = variablesIndex[variable];
						gen.Remove(index);
					}

					foreach (var variable in usedVariables)
					{
						var index = variablesIndex[variable];
						gen.Add(index);
					}
				}

				GEN[node.Id] = gen;
			}
		}

		private void ComputeKill()
		{
			KILL = new Subset<IVariable>[this.cfg.Nodes.Count];

			foreach (var node in this.cfg.Nodes)
			{
				// We add to kill all the variables modified at node
				var kill = this.variables.ToEmptySubset();
				var modifiedVariables = node.GetModifiedVariables();

				foreach (var variable in modifiedVariables)
				{
					var index = variablesIndex[variable];
					kill.Add(index);
				}

				KILL[node.Id] = kill;
			}
		}
	}
}
