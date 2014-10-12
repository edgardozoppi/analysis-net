using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.ThreeAddressCode.Instructions;
using Backend.Visitors;

namespace Backend.Analysis
{
	public class TypeInferenceAnalysis
	{
		#region class TypeInferencer

		private class TypeInferencer : InstructionVisitor
		{
			public override void Visit(LocalAllocationInstruction instruction)
			{
				instruction.TargetAddress.Type = Types.Instance.NativePointerType;
			}

			public override void Visit(SizeofInstruction instruction)
			{
				instruction.Result.Type = Types.Instance.SizeofType;
			}

			public override void Visit(CreateArrayInstruction instruction)
			{
				instruction.Result.Type = Types.Instance.ArrayType(instruction.ElementType, instruction.Rank);
			}

			public override void Visit(CatchInstruction instruction)
			{
				instruction.Result.Type = instruction.ExceptionType;
			}

			public override void Visit(CreateObjectInstruction instruction)
			{
				instruction.Result.Type = instruction.Constructor.ContainingType;
			}

			public override void Visit(MethodCallInstruction instruction)
			{
				if (instruction.HasResult)
				{
					instruction.Result.Type = instruction.Method.Type;
				}
			}

			public override void Visit(IndirectMethodCallInstruction instruction)
			{
				if (instruction.HasResult)
				{
					instruction.Result.Type = instruction.Function.Type;
				}
			}

			public override void Visit(LoadInstruction instruction)
			{
				instruction.Result.Type = instruction.Operand.Type;
			}

			public override void Visit(LoadTokenInstruction instruction)
			{
				instruction.Result.Type = Types.Instance.TokenType(instruction.Token);
			}

			public override void Visit(UnaryInstruction instruction)
			{
				instruction.Result.Type = instruction.Operand.Type;
			}

			public override void Visit(ConvertInstruction instruction)
			{
				
			}

			public override void Visit(BinaryInstruction instruction)
			{
				var left = instruction.LeftOperand.Type;
				var right = instruction.RightOperand.Type;
				var unsigned = instruction.UnsignedOperands;

				switch (instruction.Operation)
				{
					case BinaryOperation.Add:
					case BinaryOperation.Div:
					case BinaryOperation.Mul:
					case BinaryOperation.Rem:
					case BinaryOperation.Sub:
						instruction.Result.Type = Types.Instance.BinaryNumericOperationType(left, right, unsigned);
						break;

					case BinaryOperation.And:
					case BinaryOperation.Or:
					case BinaryOperation.Xor:
						instruction.Result.Type = Types.Instance.BinaryLogicalOperationType(left, right);
						break;

					case BinaryOperation.Shl:
					case BinaryOperation.Shr:
						instruction.Result.Type = left;
						break;
						
					case BinaryOperation.Eq:
					case BinaryOperation.Gt:
					case BinaryOperation.Lt:
						instruction.Result.Type = Types.Instance.PlatformType.SystemBoolean;
						break;
				}
			}
		}

		#endregion

		private ControlFlowGraph cfg;

		public TypeInferenceAnalysis(ControlFlowGraph cfg)
		{
			this.cfg = cfg;
		}

		public void Analyze()
		{
			var inferer = new TypeInferencer();
			var sorted_nodes = cfg.ForwardOrder;

			for (var i = 0; i < sorted_nodes.Length; ++i)
			{
				var node = sorted_nodes[i];
				inferer.Visit(node);
			}
		}
	}
}
