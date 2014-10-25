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
			analysis.Analyze();
			analysis.ComputeDefUseAndUseDefChains();
		}
	}
}
