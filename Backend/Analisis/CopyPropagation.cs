using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.Instructions;

namespace Backend.Analisis
{
	public class CopyPropagation
	{
		private ControlFlowGraph cfg;
		private UnaryInstruction[] copies;

		public CopyPropagation(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
			this.FindCopyInstructions();
		}

		public void Analyze()
		{

		}

		public void Transform()
		{

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
	}
}
