using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.Instructions;
using Backend.Utils;

namespace Backend.Analysis
{
	public class CopyPropagationAnalysis : ForwardDataFlowAnalysis<Subset<UnaryInstruction>> 
	{
		private UnaryInstruction[] copies;
		private Subset<UnaryInstruction>[] GEN;
		private Subset<UnaryInstruction>[] KILL;

		public CopyPropagationAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
			this.FindCopyInstructions();
			this.ComputeGen();
			this.ComputeKill();
		}

		public override Subset<UnaryInstruction> EntryInitialValue
		{
			get { return GEN[cfg.Entry.Id]; }
		}

		public override Subset<UnaryInstruction> DefaultValue
		{
			get { return copies.ToSubset(); }
		}

		public override Subset<UnaryInstruction> Meet(Subset<UnaryInstruction> left, Subset<UnaryInstruction> right)
		{
			var result = left.Clone();
			result.Intersect(right);
			return result;
		}

		public override Subset<UnaryInstruction> Transfer(CFGNode node, Subset<UnaryInstruction> nodeIN)
		{
			var generatedByNode = GEN[node.Id];
			var killedByNode = KILL[node.Id];
			var output = nodeIN.Clone();

			output.Except(killedByNode);
			output.Union(generatedByNode);
			return output;
		}

		private void FindCopyInstructions()
		{
			var copies = new List<UnaryInstruction>();

			foreach (var node in this.cfg.Nodes)
			{
				foreach (var instruction in node.Instructions)
				{
					var copy = instruction as UnaryInstruction;

					if (copy != null && copy.Operation == UnaryOperation.Assign)
					{
						copies.Add(copy);
					}
				}
			}

			this.copies = copies.ToArray();
		}

		private void ComputeGen()
		{
			var copyId = 0;
			GEN = new Subset<UnaryInstruction>[cfg.Nodes.Count];

			foreach (var node in cfg.Nodes)
			{
				var generatedCopies = this.copies.ToEmptySubset();

				foreach (var instruction in node.Instructions)
				{
					var copy = instruction as UnaryInstruction;

					if (copy != null && copy.Operation == UnaryOperation.Assign)
					{
						generatedCopies.Add(copyId);
						copyId++;
					}
				}

				GEN[node.Id] = generatedCopies;
			}
		}

		private void ComputeKill()
		{
			KILL = new Subset<UnaryInstruction>[cfg.Nodes.Count];


		}
	}
}
