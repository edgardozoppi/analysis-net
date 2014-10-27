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

		public WebAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public void Analyze()
		{
			var analysis = new ReachingDefinitionsAnalysis(cfg);
			analysis.Analyze();
			analysis.ComputeDefUseAndUseDefChains();

			this.ComputeWebs(analysis.DefinitionUses, analysis.UseDefinitions);
		}

		private void ComputeWebs(MapList<DefinitionInstruction, Instruction> def_use, MapList<Instruction, DefinitionInstruction> use_def)
		{
			var result = new MapSet<IVariable, Instruction>();

			foreach (var def in def_use.Keys)
			{
				if (result.ContainsKey(def.Result)) continue;
				var web = new HashSet<Instruction>();
				var pendings = new HashSet<Instruction>();
				pendings.Add(def);

				while (pendings.Count > 0)
				{
					var instruction = pendings.First();
					pendings.Remove(instruction);

					var isNewElement = web.Add(instruction);

					if (isNewElement)
					{
						if (instruction is DefinitionInstruction)
						{
							var uses = def_use[instruction as DefinitionInstruction];
							pendings.UnionWith(uses);
						}
						else
						{
							var defs = use_def[instruction];
							var var_defs = defs.Where(d => d.Result.Equals(def.Result));
							pendings.UnionWith(var_defs);
						}
					}
				}

				result.Add(def.Result, web);
			}
		}
	}
}
