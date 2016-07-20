// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;
using Backend.Utils;
using Model;
using Backend.Model;

namespace Backend.Analyses
{
	public class Web
	{
		public IVariable Variable { get; private set; }
		public ISet<DefinitionInstruction> Definitions { get; private set; }
		public ISet<IInstruction> Uses { get; private set; }

		public Web(IVariable variable)
		{
			this.Variable = variable;
			this.Definitions = new HashSet<DefinitionInstruction>();
			this.Uses = new HashSet<IInstruction>();
		}

		public void Rename(IVariable variable)
		{
			var oldvar = this.Variable;			
			var newvar = variable;

			foreach (var definition in this.Definitions)
			{
				definition.Result = newvar;
			}

			foreach (var instruction in this.Uses)
			{
				instruction.Replace(oldvar, newvar);
			}

			this.Variable = variable;
		}

		public override string ToString()
		{
			return this.Variable.ToString();
		}
	}

	public class WebAnalysis
	{
		private ControlFlowGraph cfg;
		private IList<Web> result;

		public WebAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public IList<Web> Result
		{
			get { return this.result; }
		}

		public IList<Web> Analyze()
		{
			var analysis = new ReachingDefinitionsAnalysis(cfg);
			analysis.Analyze();
			analysis.ComputeDefUseAndUseDefChains();

			var result = this.ComputeWebs(analysis.DefinitionUses, analysis.UseDefinitions);
			this.result = result;
			return result;
		}

		public void Transform()
		{
			if (this.result == null) throw new InvalidOperationException("Analysis result not available.");
			var index = 0u;

			foreach (var web in this.result)
			{
				// Do not rename local variables, only rename temporal variables.
				if (web.Variable.IsTemporal())
				{
					var variable = new TemporalVariable("$r", index);
					web.Rename(variable);
					index++;
				}
			}
		}

		private IList<Web> ComputeWebs(MapList<DefinitionInstruction, IInstruction> def_use, MapList<IInstruction, DefinitionInstruction> use_def)
		{
			var result = new List<Web>();
			var definitions = def_use.Keys.ToList();

			while (definitions.Count > 0)
			{
				var def = definitions.First();
				var variable = def.Result;
				var web = new Web(variable);
				var pending_defs = new HashSet<DefinitionInstruction>();
				var pending_uses = new HashSet<IInstruction>();

				result.Add(web);
				pending_defs.Add(def);

				while (pending_defs.Count > 0)
				{
					while (pending_defs.Count > 0)
					{
						var definition = pending_defs.First();
						pending_defs.Remove(definition);

						web.Definitions.Add(definition);
						definitions.Remove(definition);

						var uses = def_use[definition];
						var new_uses = uses.Except(web.Uses);
						pending_uses.UnionWith(new_uses);
					}

					while (pending_uses.Count > 0)
					{
						var instruction = pending_uses.First();
						pending_uses.Remove(instruction);

						web.Uses.Add(instruction);

						var defs = use_def[instruction];
						var var_defs = defs.Where(d => d.Result.Equals(variable));
						var new_defs = var_defs.Except(web.Definitions);
						pending_defs.UnionWith(new_defs);
					}
				}
			}

			return result;
		}
	}
}
