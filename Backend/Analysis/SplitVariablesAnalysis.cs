using Backend.ThreeAddressCode;
using Backend.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analysis
{
	public class SplitVariablesAnalysis
	{
		private ControlFlowGraph cfg;

		public SplitVariablesAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public void Analyze()
		{
			var rd = new ReachingDefinitionsAnalysis(this.cfg);
			var result = rd.Analyze();


		}

		private void ComputeUsingDefinitions(DataFlowAnalysisResult<Subset<DefinitionInstruction>> rd)
		{
			foreach (var node in this.cfg.Nodes)
			{

			}
		}
	}
}
