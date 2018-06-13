// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Types;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Visitor;
using Backend.Model;
using Model.ThreeAddressCode.Values;
using Backend.Utils;

namespace Backend.Analyses
{
	public class TypeInferenceAnalysis
	{
		#region class TypeInferer

		private class TypeInferer : InstructionVisitor
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
				instruction.Result.Type = new ArrayType(instruction.ElementType, instruction.Rank);
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

				// Skip implicit "this" parameter.
				var offset = instruction.Method.IsStatic ? 0 : 1;

				for (var i = offset; i < instruction.Arguments.Count; ++i)
				{
					var argument = instruction.Arguments[i];
					var parameter = instruction.Method.Parameters[i - offset];

					// Set the null variable a type.
					if (argument.Type == null ||
						parameter.Type.Equals(PlatformTypes.Boolean))
					{
						argument.Type = parameter.Type;
					}
				}
			}

			public override void Visit(IndirectMethodCallInstruction instruction)
			{
				if (instruction.HasResult)
				{
					instruction.Result.Type = instruction.Function.ReturnType;
				}

				// Skip implicit "this" parameter.
				var offset = instruction.Function.IsStatic ? 0 : 1;

				for (var i = offset; i < instruction.Arguments.Count; ++i)
				{
					var argument = instruction.Arguments[i];
					var parameter = instruction.Function.Parameters[i - offset];

					// Set the null variable a type.
					if (argument.Type == null ||
						parameter.Type.Equals(PlatformTypes.Boolean))
					{
						argument.Type = parameter.Type;
					}
				}
			}

			public override void Visit(LoadInstruction instruction)
			{
				var operandAsConstant = instruction.Operand as Constant;
				var operandAsVariable = instruction.Operand as IVariable;

				// Null is a polymorphic value so we handle it specially. We don't set the
				// corresponding variable's type yet. We postpone it to usage of the variable
				// or set it to System.Object if it is never used.
				if (operandAsConstant != null)
				{
					if (operandAsConstant.Value == null)
					{
						instruction.Result.Type = PlatformTypes.Object;
					}
					else if (instruction.Result.Type != null &&
							 instruction.Result.Type.Equals(PlatformTypes.Boolean))
					{
						// If the result of the load has type Boolean,
						// then we are actually loading a Boolean constant.
						if (operandAsConstant.Value.Equals(0))
						{
							operandAsConstant.Value = false;
							operandAsConstant.Type = PlatformTypes.Boolean;
						}
						else if (operandAsConstant.Value.Equals(1))
						{
							operandAsConstant.Value = true;
							operandAsConstant.Type = PlatformTypes.Boolean;
						}
					}
				}
				// If we have variable to variable assignment where the result was assigned
				// a type but the operand was not, then we set the operand type accordingly.
				else if (operandAsVariable != null && 
						 instruction.Result.Type != null &&
						(operandAsVariable.Type == null ||
						 operandAsVariable.Type.Equals(PlatformTypes.Object) ||
						 instruction.Result.Type.Equals(PlatformTypes.Boolean)))
				{
					operandAsVariable.Type = instruction.Result.Type;
				}
				
				if (instruction.Result.Type == null)
				{
					instruction.Result.Type = instruction.Operand.Type;
				}
			}

			public override void Visit(LoadTokenInstruction instruction)
			{
				instruction.Result.Type = TypeHelper.TokenType(instruction.Token);
			}

			public override void Visit(StoreInstruction instruction)
			{
				// Set the null variable a type.
				if (instruction.Result.Type != null &&
				   (instruction.Operand.Type == null ||
					instruction.Operand.Type.Equals(PlatformTypes.Object) ||
					instruction.Result.Type.Equals(PlatformTypes.Boolean)))
				{
					instruction.Operand.Type = instruction.Result.Type;
				}
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
					case ConvertOperation.Conv:
					case ConvertOperation.Cast:
					case ConvertOperation.Box:
					case ConvertOperation.Unbox:
						// ConversionType is the data type of the result.
						type = instruction.ConversionType;
						break;

					case ConvertOperation.UnboxPtr:
						// Pointer to ConversionType is the data type of the result.
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
					case BinaryOperation.Neq:
						// If one of the operands has type Boolean,
						// then the other operand must also have type Boolean.
						if (left != null && left.Equals(PlatformTypes.Boolean))
						{
							instruction.RightOperand.Type = PlatformTypes.Boolean;
						}
						else if (right != null && right.Equals(PlatformTypes.Boolean))
						{
							instruction.LeftOperand.Type = PlatformTypes.Boolean;
						}

						instruction.Result.Type = PlatformTypes.Boolean;
						break;

					case BinaryOperation.Gt:
					case BinaryOperation.Ge:
					case BinaryOperation.Lt:
					case BinaryOperation.Le:
						// If one of the operands has a reference type,
						// then the operator must be != instead of >.
						if ((left != null && left.TypeKind == TypeKind.ReferenceType) ||
							(right != null && right.TypeKind == TypeKind.ReferenceType))
						{
							instruction.Operation = BinaryOperation.Neq;
						}

						instruction.Result.Type = PlatformTypes.Boolean;
						break;
				}
			}

			public override void Visit(ConditionalBranchInstruction instruction)
			{
				if (instruction.LeftOperand.Type != null &&
					instruction.RightOperand.Type == null)
				{
					instruction.RightOperand.Type = instruction.LeftOperand.Type;
				}
				else if (instruction.LeftOperand.Type == null &&
						 instruction.RightOperand.Type != null)
				{
					instruction.LeftOperand.Type = instruction.RightOperand.Type;
				}

				if (instruction.Operation == BranchOperation.Eq &&
					instruction.RightOperand is Constant &&
					instruction.LeftOperand.Type != null &&
					!instruction.LeftOperand.Type.Equals(PlatformTypes.Boolean))
				{
					var constant = instruction.RightOperand as Constant;

					if (constant.Value is bool)
					{
						var value = (bool)constant.Value;

						if (value)
						{
							// Change num == true to num != true
							instruction.Operation = BranchOperation.Neq;
						}

						if (instruction.LeftOperand.Type.TypeKind == TypeKind.ValueType)
						{
							// Change num == false to num == 0 or
							// num != true to num != 0
							constant.Value = 0;
						}
						else
						{
							// Change num == false to num == null or
							// num != true to num != null
							constant.Value = null;
						}

						constant.Type = instruction.LeftOperand.Type;
					}
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
			var inferer = new TypeInferer();
			var sorted_nodes = cfg.ForwardOrder;

			// Propagate types over the CFG until a fixedpoint is reached
			// (i.e. when types do not change anymore)
			bool changed;

			do
			{
				var result = GetTypeInferenceResult();

				for (var i = 0; i < sorted_nodes.Length; ++i)
				{
					var node = sorted_nodes[i];
					inferer.Visit(node);
				}

				changed = !SameTypes(result);
			}
			while (changed);
		}

		private IDictionary<IVariable, IType> GetTypeInferenceResult()
		{
			var result = new Dictionary<IVariable, IType>();
			var variables = cfg.GetVariables();

			foreach (var variable in variables)
			{
				result[variable] = variable.Type;
			}

			return result;
		}

		private bool SameTypes(IDictionary<IVariable, IType> oldTypes)
		{
			var result = true;
			var variables = cfg.GetVariables();

			foreach (var variable in variables)
			{
				var oldType = oldTypes[variable];
				var newType = variable.Type;

				if (oldType == null || newType == null ||
					!oldType.Equals(newType))
				{
					result = false;
					break;
				}
			}

			return result;
		}
	}
}
