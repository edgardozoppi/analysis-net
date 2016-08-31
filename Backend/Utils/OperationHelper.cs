// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model;
using Bytecode = Model.Bytecode;
using Tac = Model.ThreeAddressCode.Instructions;

namespace Backend.Utils
{
	public static class OperationHelper
	{
		public static Tac.MethodCallOperation ToMethodCallOperation(Bytecode.MethodCallOperation operation)
		{
			switch (operation)
			{
				case Bytecode.MethodCallOperation.Static: return Tac.MethodCallOperation.Static;
				case Bytecode.MethodCallOperation.Virtual: return Tac.MethodCallOperation.Virtual;
				case Bytecode.MethodCallOperation.Jump: return Tac.MethodCallOperation.Jump;

				default: throw operation.ToUnknownValueException();
			}
		}

		public static Tac.ConvertOperation ToConvertOperation(Bytecode.ConvertOperation operation)
		{
			switch (operation)
			{
				case Bytecode.ConvertOperation.Box: return Tac.ConvertOperation.Box;
				case Bytecode.ConvertOperation.Cast: return Tac.ConvertOperation.Cast;
				case Bytecode.ConvertOperation.Conv: return Tac.ConvertOperation.Conv;
				case Bytecode.ConvertOperation.Unbox: return Tac.ConvertOperation.Unbox;
				case Bytecode.ConvertOperation.UnboxPtr: return Tac.ConvertOperation.UnboxPtr;

				default: throw operation.ToUnknownValueException();
			}
		}

		public static Tac.BranchOperation ToBranchOperation(Bytecode.BranchOperation operation)
		{
			switch (operation)
			{
				case Bytecode.BranchOperation.False:
				case Bytecode.BranchOperation.True:
				case Bytecode.BranchOperation.Eq: return Tac.BranchOperation.Eq;
				case Bytecode.BranchOperation.Ge: return Tac.BranchOperation.Ge;
				case Bytecode.BranchOperation.Gt: return Tac.BranchOperation.Gt;
				case Bytecode.BranchOperation.Le: return Tac.BranchOperation.Le;
				case Bytecode.BranchOperation.Lt: return Tac.BranchOperation.Lt;
				case Bytecode.BranchOperation.Neq: return Tac.BranchOperation.Neq;

				default: throw operation.ToUnknownValueException();
			}
		}

		public static Tac.UnaryOperation ToUnaryOperation(Bytecode.BasicOperation operation)
		{
			switch (operation)
			{
				case Bytecode.BasicOperation.Neg: return Tac.UnaryOperation.Neg;
				case Bytecode.BasicOperation.Not: return Tac.UnaryOperation.Not;

				default: throw operation.ToUnknownValueException();
			}
		}

		public static Tac.BinaryOperation ToBinaryOperation(Bytecode.BasicOperation operation)
		{
			switch (operation)
			{
				case Bytecode.BasicOperation.Add:	return Tac.BinaryOperation.Add;
				case Bytecode.BasicOperation.And:	return Tac.BinaryOperation.And;
				case Bytecode.BasicOperation.Eq:	return Tac.BinaryOperation.Eq;
				case Bytecode.BasicOperation.Gt:	return Tac.BinaryOperation.Gt;
				case Bytecode.BasicOperation.Lt:	return Tac.BinaryOperation.Lt;
				case Bytecode.BasicOperation.Div:	return Tac.BinaryOperation.Div;
				case Bytecode.BasicOperation.Mul:	return Tac.BinaryOperation.Mul;
				case Bytecode.BasicOperation.Or:	return Tac.BinaryOperation.Or;
				case Bytecode.BasicOperation.Rem:	return Tac.BinaryOperation.Rem;
				case Bytecode.BasicOperation.Shl:	return Tac.BinaryOperation.Shl;
				case Bytecode.BasicOperation.Shr:	return Tac.BinaryOperation.Shr;
				case Bytecode.BasicOperation.Sub:	return Tac.BinaryOperation.Sub;
				case Bytecode.BasicOperation.Xor:	return Tac.BinaryOperation.Xor;

				default: throw operation.ToUnknownValueException();
			}
		}

		public static bool GetUnaryConditionalBranchValue(Bytecode.BranchOperation operation)
		{
			switch (operation)
			{
				case Bytecode.BranchOperation.False: return false;
				case Bytecode.BranchOperation.True:  return true;

				default: throw operation.ToUnknownValueException();
			}
		}

		public static bool CanFallThroughNextInstruction(Bytecode.Instruction instruction)
		{
			var result = true;

			if (instruction is Bytecode.BranchInstruction)
			{
				var branch = instruction as Bytecode.BranchInstruction;

				switch (branch.Operation)
				{
					case Bytecode.BranchOperation.Branch:
					case Bytecode.BranchOperation.Leave: result = false; break;
				}
			}
			else if (instruction is Bytecode.BasicInstruction)
			{
				var basic = instruction as Bytecode.BasicInstruction;

				switch (basic.Operation)
				{
					case Bytecode.BasicOperation.Return:
					case Bytecode.BasicOperation.Throw:
					case Bytecode.BasicOperation.Rethrow:
					case Bytecode.BasicOperation.EndFilter:
					case Bytecode.BasicOperation.EndFinally: result = false; break;
				}
			}

			return result;
		}

		public static bool IsBranch(Bytecode.Instruction instruction)
		{
			var result = instruction is Bytecode.BranchInstruction ||
						 instruction is Bytecode.SwitchInstruction;

			return result;
		}
	}
}
