using Model;
using Model.ThreeAddressCode;
using Model.ThreeAddressCode.Instructions;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Analysis
{
	public class ControlFlowAnalysis
	{
		private MethodBody methodBody;

		public ControlFlowAnalysis(MethodBody methodBody)
		{
			this.methodBody = methodBody;
		}

		public ControlFlowGraph GenerateNormalControlFlow()
		{
			var instructions = this.FilterExceptionHandlers();
			var leaders = CreateNodes(instructions);
			var cfg = ConnectNodes(instructions, leaders);

			return cfg;
		}

		public ControlFlowGraph GenerateExceptionalControlFlow()
		{
			var instructions = methodBody.Instructions;
			var leaders = CreateNodes(instructions);
			var cfg = ConnectNodes(instructions, leaders);
			this.ConnectNodesWithExceptionHandlers(cfg, leaders);

			return cfg;
		}

		private IList<IInstruction> FilterExceptionHandlers()
		{
			var instructions = new List<IInstruction>();
			var handlers = methodBody.ExceptionInformation.ToDictionary(h => h.Handler.Start, h => h.Handler);
			var i = 0;

			while (i < methodBody.Instructions.Count)
			{
				var instruction = methodBody.Instructions[i];

				if (handlers.ContainsKey(instruction.Label))
				{
					var handler = handlers[instruction.Label];

					do
					{
						i++;
						instruction = methodBody.Instructions[i];
					}
					while (!instruction.Label.Equals(handler.End));
				}
				else
				{
					instructions.Add(instruction);
					i++;
				}
			}

			return instructions;
		}

		private static IDictionary<string, CFGNode> CreateNodes(IEnumerable<IInstruction> instructions)
		{
			var leaders = new Dictionary<string, CFGNode>();
			var nextIsLeader = true;
			var nodeId = 2;

			foreach (var instruction in instructions)
			{
				var isLeader = nextIsLeader;
				nextIsLeader = false;

				if (instruction is TryInstruction ||
					instruction is CatchInstruction ||
					instruction is FinallyInstruction)
				{
					isLeader = true;
				}

				if (isLeader && !leaders.ContainsKey(instruction.Label))
				{
					var node = new CFGNode(nodeId++);
					leaders.Add(instruction.Label, node);
				}

				if (instruction is UnconditionalBranchInstruction ||
					instruction is ConditionalBranchInstruction)
				{
					nextIsLeader = true;
					var branch = instruction as BranchInstruction;

					if (!leaders.ContainsKey(branch.Target))
					{
						var node = new CFGNode(nodeId++);
						leaders.Add(branch.Target, node);
					}
				}
				else if (instruction is SwitchInstruction)
				{
					nextIsLeader = true;
					var branch = instruction as SwitchInstruction;

					foreach (var target in branch.Targets)
					{
						if (!leaders.ContainsKey(target))
						{
							var node = new CFGNode(nodeId++);
							leaders.Add(target, node);
						}
					}
				}
				else if (instruction is ReturnInstruction ||
						 instruction is ThrowInstruction)
				{
					nextIsLeader = true;
				}
			}

			return leaders;
		}

		private static ControlFlowGraph ConnectNodes(IEnumerable<IInstruction> instructions, IDictionary<string, CFGNode> leaders)
		{
			var cfg = new ControlFlowGraph();
			var connectWithPreviousNode = true;
			var current = cfg.Entry;
			CFGNode previous;

			foreach (var instruction in instructions)
			{
				if (leaders.ContainsKey(instruction.Label))
				{
					previous = current;
					current = leaders[instruction.Label];

					// A node cannot fallthrough itself,
					// unless it contains another
					// instruction with the same label 
					// of the node's leader instruction
					if (connectWithPreviousNode && previous.Id != current.Id)
					{
						cfg.ConnectNodes(previous, current);
					}
				}

				connectWithPreviousNode = true;
				current.Instructions.Add(instruction);

				if (instruction is BranchInstruction)
				{
					var branch = instruction as BranchInstruction;
					var target = leaders[branch.Target];

					cfg.ConnectNodes(current, target);

					if (branch is UnconditionalBranchInstruction)
					{
						connectWithPreviousNode = false;
					}
				}
				else if (instruction is SwitchInstruction)
				{
					var branch = instruction as SwitchInstruction;

					foreach (var label in branch.Targets)
					{
						var target = leaders[label];

						cfg.ConnectNodes(current, target);
					}
				}
				else if (instruction is ReturnInstruction ||
						 instruction is ThrowInstruction)
				{
					//TODO: not always connect to exit, could exists a catch or finally block
					cfg.ConnectNodes(current, cfg.Exit);
				}
			}

			cfg.ConnectNodes(current, cfg.Exit);
			return cfg;
		}

		private void ConnectNodesWithExceptionHandlers(ControlFlowGraph cfg, IDictionary<string, CFGNode> leaders)
		{
			var activeProtectedBlocks = new HashSet<ProtectedBlock>();
			var protectedBlocksStart = methodBody.ExceptionInformation.ToLookup(pb => pb.Start);
			var protectedBlocksEnd = methodBody.ExceptionInformation.ToLookup(pb => pb.End);

			foreach (var entry in leaders)
			{
				var label = entry.Key;
				var node = entry.Value;

				if (protectedBlocksStart.Contains(label))
				{
					var startingProtectedBlocks = protectedBlocksStart[label];
					activeProtectedBlocks.UnionWith(startingProtectedBlocks);
				}

				if (protectedBlocksEnd.Contains(label))
				{
					var endingProtectedBlocks = protectedBlocksEnd[label];
					activeProtectedBlocks.ExceptWith(endingProtectedBlocks);
				}

				// Connect each node inside a try block to the first corresponding handler block
				foreach (var block in activeProtectedBlocks)
				{
					var target = leaders[block.Handler.Start];
					cfg.ConnectNodes(node, target);
				}
			}
		}
	}
}
