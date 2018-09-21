// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model;
using Model.Types;
using Model.ThreeAddressCode;
using Bytecode = Model.Bytecode;
using Tac = Model.ThreeAddressCode.Instructions;
using Backend.Model;
using Backend.Utils;

namespace Backend.Analyses
{
	public class ControlFlowAnalysis
	{
		private MethodBody methodBody;
		private ISet<string> exceptionHandlersStart;

		public ControlFlowAnalysis(MethodBody methodBody)
		{
			this.methodBody = methodBody;
			this.exceptionHandlersStart = new HashSet<string>();
		}

		public ControlFlowGraph GenerateNormalControlFlow()
		{
			FillExceptionHandlersStart();

			var instructions = FilterExceptionHandlers();
			var leaders = CreateNodes(instructions);
			var cfg = ConnectNodes(instructions, leaders);

			return cfg;
		}

		public ControlFlowGraph GenerateExceptionalControlFlow()
		{
			FillExceptionHandlersStart();

			var instructions = methodBody.Instructions;
			var leaders = CreateNodes(instructions);
			var cfg = ConnectNodes(instructions, leaders);

			ConnectNodesWithExceptionHandlers(cfg, leaders);

			return cfg;
		}

		private void FillExceptionHandlersStart()
		{
            foreach (var protectedBlock in methodBody.ExceptionInformation)
            {
                exceptionHandlersStart.Add(protectedBlock.Start);
                exceptionHandlersStart.Add(protectedBlock.Handler.Start);

                if (protectedBlock.Handler.Kind == ExceptionHandlerBlockKind.Filter)
                {
                    var filterHandler = protectedBlock.Handler as FilterExceptionHandler;

                    exceptionHandlersStart.Add(filterHandler.Handler.Start);
                }
            }
        }

        private IList<IInstruction> FilterExceptionHandlers()
		{
			var instructions = new List<IInstruction>();
            var handlersRange = new Dictionary<string, string>();
            var i = 0;

            foreach (var protectedBlock in methodBody.ExceptionInformation)
            {
                handlersRange.Add(protectedBlock.Handler.Start, protectedBlock.Handler.End);

                if (protectedBlock.Handler.Kind == ExceptionHandlerBlockKind.Filter)
                {
                    var filterHandler = protectedBlock.Handler as FilterExceptionHandler;

                    handlersRange.Add(filterHandler.Handler.Start, filterHandler.Handler.End);
                }
            }

            while (i < methodBody.Instructions.Count)
			{
				var instruction = methodBody.Instructions[i];

				if (instruction.Label != null && handlersRange.ContainsKey(instruction.Label))
				{
					var handlerEnd = handlersRange[instruction.Label];

					do
					{
						i++;
						instruction = methodBody.Instructions[i];
					}
					while (instruction.Label != handlerEnd);
				}
				else
				{
					instructions.Add(instruction);
					i++;
				}
			}

			return instructions;
		}

		private IDictionary<string, CFGNode> CreateNodes(IEnumerable<IInstruction> instructions)
		{
			var leaders = new Dictionary<string, CFGNode>();
			var nextIsLeader = true;
			var nodeId = 2;

			foreach (var instruction in instructions)
			{
				var isLeader = nextIsLeader || IsLeader(instruction);
				nextIsLeader = false;

				if (isLeader)
				{
					if (instruction.Label == null)
					{
						instruction.Label = string.Format("B_{0:X4}", nodeId);
					}

					if (!leaders.ContainsKey(instruction.Label))
					{
						var node = new CFGNode(nodeId++);
						leaders.Add(instruction.Label, node);
					}
				}

				IList<string> targets;
				var isBranch = instruction.IsBranch(out targets);
				var isExitingMethod = instruction.IsExitingMethod();

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
				if (instruction.Label != null && leaders.ContainsKey(instruction.Label))
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
				connectWithPreviousNode = instruction.CanFallThroughNextInstruction();

				IList<string> targets;
				var isBranch = instruction.IsBranch(out targets);
				var isExitingMethod = instruction.IsExitingMethod();

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

			var orderedLeaders = from entry in leaders
								 orderby entry.Value.StartOffset()
								 select entry;

			foreach (var entry in orderedLeaders)
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

		private bool IsLeader(IInstruction instruction)
		{
			var result = false;

			// Bytecode
			if (instruction is Bytecode.Instruction)
			{
				result = exceptionHandlersStart.Contains(instruction.Label);
			}
			// TAC
			else
			{
				result = instruction is Tac.TryInstruction ||
                         instruction is Tac.FilterInstruction ||
						 instruction is Tac.CatchInstruction ||
						 instruction is Tac.FinallyInstruction;
			}

			return result;
		}
	}
}
