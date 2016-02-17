using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model;
using Model.Types;
using Model.ThreeAddressCode;
using Bytecode = Model.Bytecode;
using Tac = Model.ThreeAddressCode.Instructions;

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
				var isLeader = nextIsLeader || IsLeader(instruction);
				nextIsLeader = false;

				if (isLeader && !leaders.ContainsKey(instruction.Label))
				{
					var node = new CFGNode(nodeId++);
					leaders.Add(instruction.Label, node);
				}

				IList<string> targets;
				var isBranch = IsBranch(instruction, out targets);
				var isExitingMethod = IsExitingMethod(instruction);

				if (isBranch)
				{
					nextIsLeader = true;

					foreach (var target in targets)
					{
						if (!leaders.ContainsKey(target))
						{
							var node = new CFGNode(nodeId++);
							leaders.Add(target, node);
						}
					}
				}
				else if (isExitingMethod)
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

					// A node cannot fall-through itself,
					// unless it contains another
					// instruction with the same label 
					// of the node's leader instruction
					if (connectWithPreviousNode && previous.Id != current.Id)
					{
						cfg.ConnectNodes(previous, current);
					}
				}

				current.Instructions.Add(instruction);
				connectWithPreviousNode = CanFallThroughNextInstruction(instruction);

				IList<string> targets;
				var isBranch = IsBranch(instruction, out targets);
				var isExitingMethod = IsExitingMethod(instruction);

				if (isBranch)
				{
					foreach (var label in targets)
					{
						var target = leaders[label];

						cfg.ConnectNodes(current, target);
					}
				}
				else if (isExitingMethod)
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

		private static bool IsLeader(IInstruction instruction)
		{
			var result = instruction is Tac.TryInstruction ||
						 instruction is Tac.CatchInstruction ||
						 instruction is Tac.FinallyInstruction;

			return result;
		}

		private static bool IsBranch(IInstruction instruction, out IList<string> targets)
		{
			var result = false;
			targets = null;

			// Bytecode
			if (instruction is Bytecode.BranchInstruction)
			{
				var branch = instruction as Bytecode.BranchInstruction;

				targets = new List<string>() { branch.Target };
				result = true;
			}
			else if (instruction is Bytecode.SwitchInstruction)
			{
				var branch = instruction as Bytecode.SwitchInstruction;

				targets = branch.Targets;
				result = true;
			}
			// TAC
			else if (instruction is Tac.UnconditionalBranchInstruction ||
					 instruction is Tac.ConditionalBranchInstruction)
			{
				var branch = instruction as Tac.BranchInstruction;

				targets = new List<string>() { branch.Target };
				result = true;
			}
			else if (instruction is Tac.SwitchInstruction)
			{
				var branch = instruction as Tac.SwitchInstruction;

				targets = branch.Targets;
				result = true;
			}

			return result;
		}

		private static bool IsExitingMethod(IInstruction instruction)
		{
			var result = false;

			// Bytecode
			if (instruction is Bytecode.BasicInstruction)
			{
				var basic = instruction as Bytecode.BasicInstruction;

				result = basic.Operation == Bytecode.BasicOperation.Return ||
						 basic.Operation == Bytecode.BasicOperation.Throw ||
						 basic.Operation == Bytecode.BasicOperation.Rethrow;
			}
			// TAC
			else
			{
				result = instruction is Tac.ReturnInstruction ||
						 instruction is Tac.ThrowInstruction;
			}

			return result;
		}

		private static bool CanFallThroughNextInstruction(IInstruction instruction)
		{
			var result = false;

			// Bytecode
			if (instruction is Bytecode.BranchInstruction)
			{
				var branch = instruction as Bytecode.BranchInstruction;

				result = branch.Operation == Bytecode.BranchOperation.Branch ||
						 branch.Operation == Bytecode.BranchOperation.Leave;
			}
			// TAC
			else
			{
				result = instruction is Tac.UnconditionalBranchInstruction;
			}

			result = result || IsExitingMethod(instruction);
			return !result;
		}
	}
}
