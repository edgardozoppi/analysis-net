using Backend.ThreeAddressCode.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analysis
{
	public class WebAnalysis
	{
		private ControlFlowGraph cfg;

		public WebAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public void Analyze()
		{
			var analysis = new ReachingDefinitionsAnalysis(cfg);
			var rd = analysis.Analyze();

			foreach (var node in cfg.Nodes)
			{
				var input = rd[node.Id].Input.ToSet();

				foreach (var instruction in node.Instructions)
				{
					foreach (var definition in input)
					{
						var variable = definition.Result;

						if (instruction.UsedVariables.Contains(variable))
						{
							
						}
					}

					if (instruction is DefinitionInstruction)
					{

					}
				}
			}
		}
	}
}
