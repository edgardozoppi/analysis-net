using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Backend.Utils;

namespace Backend.Analysis
{
	public class WebAnalysis
	{
		private ControlFlowGraph cfg;
		private Map<DefinitionInstruction, Instruction> def_uses;
		private Map<Instruction, DefinitionInstruction> use_defs;

		public WebAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
			this.def_uses = new Map<DefinitionInstruction, Instruction>();
			this.use_defs = new Map<Instruction, DefinitionInstruction>();
		}

		public void Analyze()
		{
			var analysis = new ReachingDefinitionsAnalysis(cfg);
			var rd = analysis.Analyze();

			foreach (var node in cfg.Nodes)
			{
				ISet<DefinitionInstruction> input = new HashSet<DefinitionInstruction>();

				if (rd[node.Id].Input != null)
				{
					rd[node.Id].Input.ToSet(input);
				}

				foreach (var instruction in node.Instructions)
				{
					foreach (var definition in input)
					{
						var variable = definition.Result;

						if (instruction.UsedVariables.Contains(variable))
						{
							def_uses.Add(definition, instruction);
							use_defs.Add(instruction, definition);
						}
					}

					if (instruction is DefinitionInstruction)
					{
						var definition = instruction as DefinitionInstruction;
						var variable = definition.Result;

						input = this.RemoveVariableDefinitions(input, variable);
						input.Add(definition);
					}
				}
			}
		}

		private ISet<DefinitionInstruction> RemoveVariableDefinitions(ISet<DefinitionInstruction> definitions, IVariable variable)
		{
			var result = new HashSet<DefinitionInstruction>();

			foreach (var definition in definitions)
			{
				if (definition.Result.Equals(variable)) continue;
				
				result.Add(definition);
			}

			return result;
		}
	}
}
