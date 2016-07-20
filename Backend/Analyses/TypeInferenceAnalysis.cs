// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Types;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Visitor;
using Backend.Model;

namespace Backend.Analyses
{
	public class TypeInferenceAnalysis
	{
		#region class TypeInferencer

		private class TypeInferencer : InstructionVisitor
		{
			public override void Visit(LocalAllocationInstruction instruction)
			{
				instruction.TargetAddress.Type = PlatformTypes.IntPtr;
			}

			public override void Visit(SizeofInstruction instruction)
			{
				instruction.Result.Type = PlatformTypes.SizeofType;
			}

			public override void Visit(CreateArrayInstruction instruction)
			{
				instruction.Result.Type = new ArrayType(instruction.ElementType);
			}

			public override void Visit(CatchInstruction instruction)
			{
				instruction.Result.Type = instruction.ExceptionType;
			}

			public override void Visit(CreateObjectInstruction instruction)
			{
				instruction.Result.Type = instruction.AllocationType;
			}

			public override void Visit(MethodCallInstruction instruction)
			{
				if (instruction.HasResult)
				{
					instruction.Result.Type = instruction.Method.ReturnType;
				}
			}

			public override void Visit(IndirectMethodCallInstruction instruction)
			{
				if (instruction.HasResult)
				{
					instruction.Result.Type = instruction.Function.ReturnType;
				}
			}

			public override void Visit(LoadInstruction instruction)
			{
				instruction.Result.Type = instruction.Operand.Type;
			}

			public override void Visit(LoadTokenInstruction instruction)
			{
				instruction.Result.Type = TypeHelper.TokenType(instruction.Token);
			}

			public override void Visit(StoreInstruction instruction)
			{
				// Nothing to do here, for debugging purposes only
			}

			public override void Visit(UnaryInstruction instruction)
			{
				instruction.Result.Type = instruction.Operand.Type;
			}

			public override void Visit(ConvertInstruction instruction)
			{
				var type = instruction.Operand.Type;

				switch (instruction.Operation)
				{
					case ConvertOperation.Cast:
					case ConvertOperation.Box:
					case ConvertOperation.Unbox:
						// ConversionType is the data type of the result
						type = instruction.ConversionType;
						break;

					case ConvertOperation.UnboxPtr:
						// Pointer to ConversionType is the data type of the result
						type = new PointerType(instruction.ConversionType);
						break;
				}

				instruction.Result.Type = type;
			}

			public override void Visit(PhiInstruction instruction)
			{
				var type = instruction.Arguments.First().Type;
				var arguments = instruction.Arguments.Skip(1);

				foreach (var argument in arguments)
				{
					type = TypeHelper.MergedType(type, argument.Type);
				}

				instruction.Result.Type = type;
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
						instruction.Result.Type = TypeHelper.BinaryNumericOperationType(left, right, unsigned);
						break;

					case BinaryOperation.And:
					case BinaryOperation.Or:
					case BinaryOperation.Xor:
						instruction.Result.Type = TypeHelper.BinaryLogicalOperationType(left, right);
						break;

					case BinaryOperation.Shl:
					case BinaryOperation.Shr:
						instruction.Result.Type = left;
						break;
						
					case BinaryOperation.Eq:
					case BinaryOperation.Gt:
					case BinaryOperation.Lt:
						instruction.Result.Type = PlatformTypes.Boolean;
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

			// TODO: Propagate types over the CFG until a fixpoint is reached (i.e. when types do not change)

			for (var i = 0; i < sorted_nodes.Length; ++i)
			{
				var node = sorted_nodes[i];
				inferer.Visit(node);
			}
		}
	}
}
