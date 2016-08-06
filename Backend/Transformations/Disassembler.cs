// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.ThreeAddressCode;
using Backend.Utils;
using Model.ThreeAddressCode.Values;
using Tac = Model.ThreeAddressCode.Instructions;
using Model.Types;
using Backend.Analyses;
using Backend.Model;
using Model;
using Bytecode = Model.Bytecode;
using Model.Bytecode.Visitor;

namespace Backend.Transformations
{
	public class Disassembler
	{
		#region class OperandStack

		private class OperandStack
		{
			private ushort capacity;
			private ushort top;
			private TemporalVariable[] stack;			

			public OperandStack(ushort capacity)
			{
				this.capacity = capacity;
				stack = new TemporalVariable[capacity + 1];

				for (var i = 0u; i < stack.Length; ++i)
				{
					stack[i] = new TemporalVariable("$s", i);
				}
			}

			public IEnumerable<TemporalVariable> Variables
			{
				get { return stack; }
			}

			public int Capacity
			{
				get { return capacity; }
			}

			public ushort Size
			{
				get { return top; }
				set
				{
					if (value < 0 || value > capacity) throw new InvalidOperationException();
					top = value;
				}
			}

			public void Clear()
			{
				top = 0;
			}

			public void IncrementCapacity()
			{
				if (capacity >= stack.Length) throw new InvalidOperationException();
				capacity++;
			}

			public void DecrementCapacity()
			{
				if (capacity <= stack.Length - 1) throw new InvalidOperationException();
				capacity--;
			}

			public TemporalVariable Push()
			{
				if (top >= capacity) throw new InvalidOperationException();
				return stack[top++];
			}

			public TemporalVariable Pop()
			{
				if (top <= 0) throw new InvalidOperationException();
				return stack[--top];
			}

			public TemporalVariable Top()
			{
				if (top <= 0) throw new InvalidOperationException();
				return stack[top - 1];
			}
		}

		#endregion

		#region class Translator

		private class InstructionTranslator : InstructionVisitor
		{
			private MethodBody body;
			private OperandStack stack;
			private IType returnType;

			public InstructionTranslator(OperandStack stack, MethodBody body, IType returnType)
			{
				this.stack = stack;
				this.body = body;				
				this.returnType = returnType;
			}

			public override void Visit(Bytecode.BasicInstruction op)
			{
				switch (op.Operation)
				{
					case Bytecode.BasicOperation.Add:
					case Bytecode.BasicOperation.Sub:
					case Bytecode.BasicOperation.Mul:
					case Bytecode.BasicOperation.Div:
					case Bytecode.BasicOperation.Rem:
					case Bytecode.BasicOperation.And:
					case Bytecode.BasicOperation.Or:
					case Bytecode.BasicOperation.Xor:
					case Bytecode.BasicOperation.Shl:
					case Bytecode.BasicOperation.Shr:
					case Bytecode.BasicOperation.Eq:
					case Bytecode.BasicOperation.Lt:
					case Bytecode.BasicOperation.Gt:
						ProcessBinaryOperation(op);
						break;

					case Bytecode.BasicOperation.Throw:
						ProcessThrow(op);
						break;

					case Bytecode.BasicOperation.Rethrow:
						ProcessRethrow(op);
						break;

					case Bytecode.BasicOperation.Not:
					case Bytecode.BasicOperation.Neg:
						ProcessUnaryOperation(op);
						break;

					case Bytecode.BasicOperation.Nop:
						ProcessEmptyOperation(op);
						break;

					case Bytecode.BasicOperation.Pop:
						ProcessPop(op);
						break;

					case Bytecode.BasicOperation.Dup:
						ProcessDup(op);
						break;

					case Bytecode.BasicOperation.EndFinally:
						ProcessEndFinally(op);
						break;

					//case Bytecode.BasicOperation.EndFilter:
					//	ProcessEndFilter(op);
					//	break;

					case Bytecode.BasicOperation.LocalAllocation:
						ProcessLocalAllocation(op);
						break;

					case Bytecode.BasicOperation.InitBlock:
						ProcessInitializeMemory(op);
						break;

					case Bytecode.BasicOperation.InitObject:
						ProcessInitializeObject(op);
						break;

					case Bytecode.BasicOperation.CopyObject:
						ProcessCopyObject(op);
						break;

					case Bytecode.BasicOperation.CopyBlock:
						ProcessCopyMemory(op);
						break;

					case Bytecode.BasicOperation.LoadArrayLength:
						ProcessLoadArrayLength(op);
						break;

					case Bytecode.BasicOperation.IndirectLoad:
						ProcessIndirectLoad(op);
						break;

					case Bytecode.BasicOperation.LoadArrayElement:
						ProcessLoadArrayElement(op);
						break;

					case Bytecode.BasicOperation.LoadArrayElementAddress:
						ProcessLoadArrayElementAddress(op);
						break;

					case Bytecode.BasicOperation.IndirectStore:
						ProcessIndirectStore(op);
						break;

					case Bytecode.BasicOperation.StoreArrayElement:
						ProcessStoreArrayElement(op);
						break;

					case Bytecode.BasicOperation.Breakpoint:
						ProcessBreakpointOperation(op);
						break;

					case Bytecode.BasicOperation.Return:
						ProcessReturn(op);
						break;

					default: throw op.Operation.ToUnknownValueException();
				}
			}

			private void ProcessBinaryOperation(Bytecode.BasicInstruction op)
			{
				var operation = OperationHelper.ToBinaryOperation(op.Operation);

				var right = stack.Pop();
				var left = stack.Pop();
				var dest = stack.Push();
				var instruction = new Tac.BinaryInstruction(op.Offset, dest, left, operation, right);
				instruction.OverflowCheck = op.OverflowCheck;
				instruction.UnsignedOperands = op.UnsignedOperands;
				body.Instructions.Add(instruction);
			}

			private void ProcessThrow(Bytecode.BasicInstruction op)
			{
				var exception = stack.Pop();
				stack.Clear();

				var instruction = new Tac.ThrowInstruction(op.Offset, exception);
				body.Instructions.Add(instruction);
			}

			private void ProcessRethrow(Bytecode.BasicInstruction op)
			{
				var instruction = new Tac.ThrowInstruction(op.Offset);
				body.Instructions.Add(instruction);
			}

			private void ProcessUnaryOperation(Bytecode.BasicInstruction op)
			{
				var operation = OperationHelper.ToUnaryOperation(op.Operation);

				var operand = stack.Pop();
				var dest = stack.Push();
				var instruction = new Tac.UnaryInstruction(op.Offset, dest, operation, operand);
				body.Instructions.Add(instruction);
			}

			private void ProcessEmptyOperation(Bytecode.BasicInstruction op)
			{
				var instruction = new Tac.NopInstruction(op.Offset);
				body.Instructions.Add(instruction);
			}

			private void ProcessPop(Bytecode.BasicInstruction op)
			{
				stack.Pop();
			}

			private void ProcessDup(Bytecode.BasicInstruction op)
			{
				var source = stack.Top();
				var dest = stack.Push();
				var instruction = new Tac.LoadInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			private void ProcessEndFinally(Bytecode.BasicInstruction op)
			{
				stack.Clear();

				//// TODO: Maybe we don't need to add this branch instruction
				//// since it is jumping to the next one,
				//// so it is the same as falling through
				//if (exceptionHandlersEnd.ContainsKey(op.Offset))
				//{
				//	var handlers = exceptionHandlersEnd[op.Offset];

				//	foreach (var handler in handlers)
				//	{
				//		if (handler.Kind == ExceptionHandlerBlockKind.Finally ||
				//			handler.Kind == ExceptionHandlerBlockKind.Fault)
				//		{
				//			var branch = new UnconditionalBranchInstruction(op.Offset, op.Offset + 1);
				//			body.Instructions.Add(branch);
				//		}
				//	}
				//}
			}

			private void ProcessLocalAllocation(Bytecode.BasicInstruction op)
			{
				var numberOfBytes = stack.Pop();
				var targetAddress = stack.Push();

				var instruction = new Tac.LocalAllocationInstruction(op.Offset, targetAddress, numberOfBytes);
				body.Instructions.Add(instruction);
			}

			private void ProcessInitializeMemory(Bytecode.BasicInstruction op)
			{
				var numberOfBytes = stack.Pop();
				var fillValue = stack.Pop();
				var targetAddress = stack.Pop();

				var instruction = new Tac.InitializeMemoryInstruction(op.Offset, targetAddress, fillValue, numberOfBytes);
				body.Instructions.Add(instruction);
			}

			private void ProcessInitializeObject(Bytecode.BasicInstruction op)
			{
				var targetAddress = stack.Pop();
				var instruction = new Tac.InitializeObjectInstruction(op.Offset, targetAddress);
				body.Instructions.Add(instruction);
			}

			private void ProcessCopyObject(Bytecode.BasicInstruction op)
			{
				var sourceAddress = stack.Pop();
				var targetAddress = stack.Pop();

				var instruction = new Tac.CopyObjectInstruction(op.Offset, targetAddress, sourceAddress);
				body.Instructions.Add(instruction);
			}

			private void ProcessCopyMemory(Bytecode.BasicInstruction op)
			{
				var numberOfBytes = stack.Pop();
				var sourceAddress = stack.Pop();
				var targetAddress = stack.Pop();

				var instruction = new Tac.CopyMemoryInstruction(op.Offset, targetAddress, sourceAddress, numberOfBytes);
				body.Instructions.Add(instruction);
			}

			private void ProcessLoadArrayLength(Bytecode.BasicInstruction op)
			{
				var array = stack.Pop();
				var dest = stack.Push();
				var length = new ArrayLengthAccess(array);
				var instruction = new Tac.LoadInstruction(op.Offset, dest, length);
				body.Instructions.Add(instruction);
			}

			private void ProcessIndirectLoad(Bytecode.BasicInstruction op)
			{
				var address = stack.Pop();
				var dest = stack.Push();
				var source = new Dereference(address);
				var instruction = new Tac.LoadInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			private void ProcessLoadArrayElement(Bytecode.BasicInstruction op)
			{
				var index = stack.Pop();
				var array = stack.Pop();
				var dest = stack.Push();
				var source = new ArrayElementAccess(array, index);
				var instruction = new Tac.LoadInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			private void ProcessLoadArrayElementAddress(Bytecode.BasicInstruction op)
			{
				var index = stack.Pop();
				var array = stack.Pop();
				var dest = stack.Push();
				var access = new ArrayElementAccess(array, index);
				var source = new Reference(access);
				var instruction = new Tac.LoadInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			private void ProcessIndirectStore(Bytecode.BasicInstruction op)
			{
				var source = stack.Pop();
				var address = stack.Pop();
				var dest = new Dereference(address);
				var instruction = new Tac.StoreInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			private void ProcessStoreArrayElement(Bytecode.BasicInstruction op)
			{
				var source = stack.Pop();
				var index = stack.Pop();
				var array = stack.Pop();
				var dest = new ArrayElementAccess(array, index);
				var instruction = new Tac.StoreInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			private void ProcessBreakpointOperation(Bytecode.BasicInstruction op)
			{
				var instruction = new Tac.BreakpointInstruction(op.Offset);
				body.Instructions.Add(instruction);
			}

			private void ProcessReturn(Bytecode.BasicInstruction op)
			{
				IVariable operand = null;

				if (!returnType.Equals(PlatformTypes.Void))
				{
					operand = stack.Pop();
				}

				var instruction = new Tac.ReturnInstruction(op.Offset, operand);
				body.Instructions.Add(instruction);
			}

			public override void Visit(Bytecode.BranchInstruction op)
			{
				switch (op.Operation)
				{
					case Bytecode.BranchOperation.False:
					case Bytecode.BranchOperation.True:
						ProcessUnaryConditionalBranch(op);
						break;

					case Bytecode.BranchOperation.Eq:
					case Bytecode.BranchOperation.Neq:
					case Bytecode.BranchOperation.Lt:
					case Bytecode.BranchOperation.Le:
					case Bytecode.BranchOperation.Gt:
					case Bytecode.BranchOperation.Ge:
						ProcessBinaryConditionalBranch(op);
						break;

					case Bytecode.BranchOperation.Branch:
						ProcessUnconditionalBranch(op);
						break;

					case Bytecode.BranchOperation.Leave:
						ProcessLeave(op);
						break;

					default: throw op.Operation.ToUnknownValueException();
				}
			}

			private void ProcessUnaryConditionalBranch(Bytecode.BranchInstruction op)
			{
				var condition = OperationHelper.ToBranchOperation(op.Operation);
				var value = OperationHelper.GetUnaryConditionalBranchValue(op.Operation);
				var right = new Constant(value);
				var left = stack.Pop();

				var instruction = new Tac.ConditionalBranchInstruction(op.Offset, left, condition, right, op.Target);
				body.Instructions.Add(instruction);
			}

			private void ProcessBinaryConditionalBranch(Bytecode.BranchInstruction op)
			{
				var condition = OperationHelper.ToBranchOperation(op.Operation);
				var right = stack.Pop();
				var left = stack.Pop();

				var instruction = new Tac.ConditionalBranchInstruction(op.Offset, left, condition, right, op.Target);
				instruction.UnsignedOperands = op.UnsignedOperands;
				body.Instructions.Add(instruction);
			}

			private void ProcessUnconditionalBranch(Bytecode.BranchInstruction op)
			{
				var instruction = new Tac.UnconditionalBranchInstruction(op.Offset, op.Target);
				body.Instructions.Add(instruction);
			}

			private void ProcessLeave(Bytecode.BranchInstruction op)
			{
				var isTryFinallyEnd = false;

				stack.Clear();

				//if (exceptionHandlersEnd.ContainsKey(op.Offset))
				//{
				//	var handlers = exceptionHandlersEnd[op.Offset];
				//	var catchs = new List<BranchInstruction>();
				//	var finallys = new List<BranchInstruction>();
				//
				//	foreach (var handler in handlers)
				//	{
				//		if (handler.Kind == ExceptionHandlerBlockKind.Try)
				//		{
				//			var tryHandler = handler as TryExceptionHandler;
				//
				//			if (tryHandler.Handler.Kind == ExceptionHandlerBlockKind.Catch)
				//			{
				//				var catchHandler = tryHandler.Handler as CatchExceptionHandler;
				//				var branch = new ExceptionalBranchInstruction(op.Offset, 0, catchHandler.ExceptionType);
				//				branch.Target = catchHandler.Start;
				//				catchs.Add(branch);
				//			}
				//			else if (tryHandler.Handler.Kind == ExceptionHandlerBlockKind.Fault)
				//			{
				//				var faultHandler = tryHandler.Handler as FaultExceptionHandler;
				//				var branch = new ExceptionalBranchInstruction(op.Offset, 0, host.PlatformType.SystemException);
				//				branch.Target = faultHandler.Start;
				//				finallys.Add(branch);
				//			}
				//			else if (tryHandler.Handler.Kind == ExceptionHandlerBlockKind.Finally)
				//			{
				//				isTryFinallyEnd = true;
				//				var finallyHandler = tryHandler.Handler as FinallyExceptionHandler;
				//				var branch = new UnconditionalBranchInstruction(op.Offset, 0);
				//				branch.Target = finallyHandler.Start;
				//				finallys.Add(branch);
				//			}
				//		}
				//	}
				//
				//	body.Instructions.AddRange(catchs);
				//	body.Instructions.AddRange(finallys);
				//}

				if (!isTryFinallyEnd)
				{
					var instruction = new Tac.UnconditionalBranchInstruction(op.Offset, op.Target);
					body.Instructions.Add(instruction);
				}
			}

			public override void Visit(Bytecode.ConvertInstruction op)
			{
				var operation = OperationHelper.ToConvertOperation(op.Operation);
				var operand = stack.Pop();
				var result = stack.Push();

				var instruction = new Tac.ConvertInstruction(op.Offset, result, operand, operation, op.ConversionType);
				instruction.OverflowCheck = op.OverflowCheck;
				instruction.UnsignedOperands = op.UnsignedOperands;
				body.Instructions.Add(instruction);
			}

			public override void Visit(Bytecode.CreateArrayInstruction op)
			{
				var lowerBounds = new List<IVariable>();
				var sizes = new List<IVariable>();

				if (op.WithLowerBound)
				{
					for (uint i = 0; i < op.Type.Rank; i++)
					{
						var operand = stack.Pop();
						lowerBounds.Add(operand);
					}
				}

				for (uint i = 0; i < op.Type.Rank; i++)
				{
					var operand = stack.Pop();
					sizes.Add(operand);
				}

				lowerBounds.Reverse();
				sizes.Reverse();

				var result = stack.Push();
				var instruction = new Tac.CreateArrayInstruction(op.Offset, result, op.Type.ElementsType, op.Type.Rank, lowerBounds, sizes);
				body.Instructions.Add(instruction);
			}

			public override void Visit(Bytecode.CreateObjectInstruction op)
			{
				stack.IncrementCapacity();

				var allocationResult = stack.Push();
				stack.Pop();

				var arguments = new List<IVariable>();

				foreach (var par in op.Constructor.Parameters)
				{
					var arg = stack.Pop();
					arguments.Add(arg);
				}

				//foreach (var par in op.Constructor.ExtraParameters)
				//{
				//	var arg = stack.Pop();
				//	arguments.Add(arg);
				//}

				// Adding implicit this parameter
				arguments.Add(allocationResult);
				arguments.Reverse();

				IInstruction instruction = new Tac.CreateObjectInstruction(op.Offset, allocationResult, op.Constructor.ContainingType);
				body.Instructions.Add(instruction);

				instruction = new Tac.MethodCallInstruction(op.Offset, null, op.Constructor, arguments);
				body.Instructions.Add(instruction);

				var result = stack.Push();

				instruction = new Tac.LoadInstruction(op.Offset, result, allocationResult);
				body.Instructions.Add(instruction);

				stack.DecrementCapacity();
			}

			public override void Visit(Bytecode.IndirectMethodCallInstruction op)
			{
				var calleePointer = stack.Pop();
				var arguments = new List<IVariable>();
				IVariable result = null;

				foreach (var par in op.Function.Parameters)
				{
					var arg = stack.Pop();
					arguments.Add(arg);
				}

				if (!op.Function.IsStatic)
				{
					// Adding implicit this parameter
					var argThis = stack.Pop();
					arguments.Add(argThis);
				}

				arguments.Reverse();

				if (!op.Function.ReturnType.Equals(PlatformTypes.Void))
				{
					result = stack.Push();
				}

				var instruction = new Tac.IndirectMethodCallInstruction(op.Offset, result, calleePointer, op.Function, arguments);
				body.Instructions.Add(instruction);
			}

			public override void Visit(Bytecode.LoadFieldInstruction op)
			{
				switch (op.Operation)
				{
					case Bytecode.LoadFieldOperation.Content:
						ProcessLoadField(op);
						break;

					case Bytecode.LoadFieldOperation.Address:
						ProcessLoadFieldAddress(op);
						break;

					default: throw op.Operation.ToUnknownValueException();
				}
			}

			private void ProcessLoadField(Bytecode.LoadFieldInstruction op)
			{
				if (op.Field.IsStatic)
				{
					ProcessLoadStaticField(op);
				}
				else
				{
					ProcessLoadInstanceField(op);
				}
			}

			private void ProcessLoadFieldAddress(Bytecode.LoadFieldInstruction op)
			{
				if (op.Field.IsStatic)
				{
					ProcessLoadStaticFieldAddress(op);
				}
				else
				{
					ProcessLoadInstanceFieldAddress(op);
				}
			}

			private void ProcessLoadStaticField(Bytecode.LoadFieldInstruction op)
			{
				var dest = stack.Push();
				var source = new StaticFieldAccess(op.Field);
				var instruction = new Tac.LoadInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			private void ProcessLoadInstanceField(Bytecode.LoadFieldInstruction op)
			{
				var obj = stack.Pop();
				var dest = stack.Push();
				var source = new InstanceFieldAccess(obj, op.Field);
				var instruction = new Tac.LoadInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			private void ProcessLoadStaticFieldAddress(Bytecode.LoadFieldInstruction op)
			{
				var dest = stack.Push();
				var access = new StaticFieldAccess(op.Field);
				var source = new Reference(access);
				var instruction = new Tac.LoadInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			private void ProcessLoadInstanceFieldAddress(Bytecode.LoadFieldInstruction op)
			{
				var obj = stack.Pop();
				var dest = stack.Push();
				var access = new InstanceFieldAccess(obj, op.Field);
				var source = new Reference(access);
				var instruction = new Tac.LoadInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			public override void Visit(Bytecode.LoadInstruction op)
			{
				switch (op.Operation)
				{
					case Bytecode.LoadOperation.Value:
						ProcessLoadConstant(op);
						break;

					case Bytecode.LoadOperation.Content:
						ProcessLoadVariable(op);
						break;

					case Bytecode.LoadOperation.Address:
						ProcessLoadVariableAddress(op);
						break;

					default: throw op.Operation.ToUnknownValueException();
				}
			}

			private void ProcessLoadConstant(Bytecode.LoadInstruction op)
			{
				var dest = stack.Push();
				var instruction = new Tac.LoadInstruction(op.Offset, dest, op.Operand);
				body.Instructions.Add(instruction);
			}

			private void ProcessLoadVariable(Bytecode.LoadInstruction op)
			{
				var dest = stack.Push();
				var instruction = new Tac.LoadInstruction(op.Offset, dest, op.Operand);
				body.Instructions.Add(instruction);
			}

			private void ProcessLoadVariableAddress(Bytecode.LoadInstruction op)
			{				
				var dest = stack.Push();
				var operand = (IVariable)op.Operand;
				var source = new Reference(operand);
				var instruction = new Tac.LoadInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			public override void Visit(Bytecode.LoadMethodAddressInstruction op)
			{
				switch (op.Operation)
				{
					case Bytecode.LoadMethodAddressOperation.Static:
						ProcessLoadStaticMethodAddress(op);
						break;

					case Bytecode.LoadMethodAddressOperation.Virtual:
						ProcessLoadVirtualMethodAddress(op);
						break;

					default: throw op.Operation.ToUnknownValueException();
				}
			}

			public void ProcessLoadStaticMethodAddress(Bytecode.LoadMethodAddressInstruction op)
			{
				var dest = stack.Push();
				var source = new StaticMethodReference(op.Method);
				var instruction = new Tac.LoadInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			public void ProcessLoadVirtualMethodAddress(Bytecode.LoadMethodAddressInstruction op)
			{
				var obj = stack.Pop();
				var dest = stack.Push();
				var source = new VirtualMethodReference(obj, op.Method);
				var instruction = new Tac.LoadInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			public override void Visit(Bytecode.LoadTokenInstruction op)
			{
				var result = stack.Push();
				var instruction = new Tac.LoadTokenInstruction(op.Offset, result, op.Token);
				body.Instructions.Add(instruction);
			}

			public override void Visit(Bytecode.MethodCallInstruction op)
			{
				var arguments = new List<IVariable>();
				IVariable result = null;

				foreach (var par in op.Method.Parameters)
				{
					var arg = stack.Pop();
					arguments.Add(arg);
				}

				//foreach (var par in op.Method.ExtraParameters)
				//{
				//	var arg = stack.Pop();
				//	arguments.Add(arg);
				//}

				if (!op.Method.IsStatic)
				{
					// Adding implicit this parameter
					var argThis = stack.Pop();
					arguments.Add(argThis);
				}

				arguments.Reverse();

				if (!op.Method.ReturnType.Equals(PlatformTypes.Void))
				{
					result = stack.Push();
				}

				var instruction = new Tac.MethodCallInstruction(op.Offset, result, op.Method, arguments);
				body.Instructions.Add(instruction);
			}

			public override void Visit(Bytecode.SizeofInstruction op)
			{
				var result = stack.Push();
				var instruction = new Tac.SizeofInstruction(op.Offset, result, op.MeasuredType);
				body.Instructions.Add(instruction);
			}

			public override void Visit(Bytecode.StoreFieldInstruction op)
			{
				if (op.Field.IsStatic)
				{
					ProcessStoreStaticField(op);
				}
				else
				{
					ProcessStoreInstanceField(op);
				}
			}

			private void ProcessStoreStaticField(Bytecode.StoreFieldInstruction op)
			{
				var source = stack.Pop();
				var dest = new StaticFieldAccess(op.Field);
				var instruction = new Tac.StoreInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			private void ProcessStoreInstanceField(Bytecode.StoreFieldInstruction op)
			{
				var source = stack.Pop();
				var obj = stack.Pop();
				var dest = new InstanceFieldAccess(obj, op.Field);
				var instruction = new Tac.StoreInstruction(op.Offset, dest, source);
				body.Instructions.Add(instruction);
			}

			public override void Visit(Bytecode.StoreInstruction op)
			{
				var source = stack.Pop();
				var instruction = new Tac.LoadInstruction(op.Offset, op.Target, source);
				body.Instructions.Add(instruction);
			}

			public override void Visit(Bytecode.SwitchInstruction op)
			{
				var operand = stack.Pop();
				var instruction = new Tac.SwitchInstruction(op.Offset, operand, op.Targets);
				body.Instructions.Add(instruction);
			}
		}

		#endregion

		private MethodDefinition method;
		private OperandStack stack;
		private MapList<string, IExceptionHandlerBlock> exceptionHandlersStart;
		//private MapList<string, IExceptionHandlerBlock> exceptionHandlersEnd;

		public Disassembler(MethodDefinition methodDefinition)
		{
			this.method = methodDefinition;
			this.stack = new OperandStack(method.Body.MaxStack);
			this.exceptionHandlersStart = new MapList<string, IExceptionHandlerBlock>();
			//this.exceptionHandlersEnd = new MapList<string, IExceptionHandlerBlock>();
		}

		public MethodBody Execute()
		{
			var body = new MethodBody();

			body.MaxStack = method.Body.MaxStack;
			body.Parameters.AddRange(method.Body.Parameters);
			body.LocalVariables.UnionWith(method.Body.LocalVariables);
			body.ExceptionInformation.AddRange(method.Body.ExceptionInformation);

			if (method.Body.Instructions.Count > 0)
			{
				FillExceptionHandlersStart();

				var translator = new InstructionTranslator(stack, body, method.ReturnType);
				var cfanalysis = new ControlFlowAnalysis(method.Body);
				//var cfg = cfanalysis.GenerateNormalControlFlow();
				var cfg = cfanalysis.GenerateExceptionalControlFlow();
				var stackSizeAtEntry = new ushort?[cfg.Nodes.Count];
				var sorted_nodes = cfg.ForwardOrder;

				foreach (var node in sorted_nodes)
				{
					var stackSize = stackSizeAtEntry[node.Id];

					if (!stackSize.HasValue)
					{
						stackSizeAtEntry[node.Id] = 0;
					}

					stack.Size = stackSizeAtEntry[node.Id].Value;
					this.ProcessBasicBlock(body, node, translator);

					foreach (var successor in node.Successors)
					{
						stackSize = stackSizeAtEntry[successor.Id];

						if (!stackSize.HasValue)
						{
							stackSizeAtEntry[successor.Id] = stack.Size;
						}
						else if (stackSize.Value != stack.Size)
						{
							// Check that the already saved stack size is the same as the current stack size
							throw new Exception("Basic block with different stack size at entry!");
						}
					}
				}

				body.LocalVariables.UnionWith(stack.Variables);

				var instructions = body.Instructions.OrderBy(op => op.Offset).ToList();
				body.Instructions.Clear();
				body.Instructions.AddRange(instructions);
			}

			return body;
		}

		private void FillExceptionHandlersStart()
		{
			foreach (var protectedBlock in method.Body.ExceptionInformation)
			{
				exceptionHandlersStart.Add(protectedBlock.Start, protectedBlock);
				exceptionHandlersStart.Add(protectedBlock.Handler.Start, protectedBlock.Handler);
				//exceptionHandlersEnd.Add(protectedBlock.End, protectedBlock);
				//exceptionHandlersEnd.Add(protectedBlock.Handler.End, protectedBlock.Handler);
			}
		}

		private void ProcessBasicBlock(MethodBody body, CFGNode node, InstructionTranslator translator)
		{
			if (node.Instructions.Count == 0) return;

			var firstInstruction = node.Instructions.First();
			this.ProcessExceptionHandling(body, firstInstruction);

			translator.Visit(node);			
		}

		private void ProcessExceptionHandling(MethodBody body, IInstruction operation)
		{
			if (exceptionHandlersStart.ContainsKey(operation.Label))
			{
				var handlerBlocks = exceptionHandlersStart[operation.Label];

				foreach (var block in handlerBlocks)
				{
					Tac.Instruction instruction;

					switch (block.Kind)
					{
						case ExceptionHandlerBlockKind.Try:
							instruction = new Tac.TryInstruction(operation.Offset);
							break;

						case ExceptionHandlerBlockKind.Catch:
							// push the exception into the stack
							var exception = stack.Push();
							var catchBlock = block as CatchExceptionHandler;
							instruction = new Tac.CatchInstruction(operation.Offset, exception, catchBlock.ExceptionType);
							break;

						case ExceptionHandlerBlockKind.Fault:
							instruction = new Tac.FaultInstruction(operation.Offset);
							break;

						case ExceptionHandlerBlockKind.Finally:
							instruction = new Tac.FinallyInstruction(operation.Offset);
							break;

						default:
							throw new Exception("Unknown ExceptionHandlerKind.");
					}

					body.Instructions.Add(instruction);
				}
			}
		}
	}
}
