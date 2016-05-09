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
			private ushort top;
			private TemporalVariable[] stack;			

			public OperandStack(ushort capacity)
			{
				stack = new TemporalVariable[capacity];

				for (var i = 0u; i < capacity; ++i)
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
				get { return stack.Length; }
			}

			public ushort Size
			{
				get { return top; }
				set
				{
					if (value < 0 || value > stack.Length) throw new InvalidOperationException();
					top = value;
				}
			}

			public void Clear()
			{
				top = 0;
			}

			public TemporalVariable Push()
			{
				if (top >= stack.Length) throw new InvalidOperationException();
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

		private class Translator : InstructionVisitor
		{
			private MethodBody body;
			private OperandStack stack;

			public Translator(MethodBody body, OperandStack stack)
			{
				this.body = body;
				this.stack = stack;
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
						break;
					case Bytecode.BasicOperation.Throw:
						break;
					case Bytecode.BasicOperation.Rethrow:
						break;
					case Bytecode.BasicOperation.Not:
						break;
					case Bytecode.BasicOperation.Neg:
						break;
					case Bytecode.BasicOperation.Nop:
						break;
					case Bytecode.BasicOperation.Pop:
						break;
					case Bytecode.BasicOperation.Dup:
						break;
					case Bytecode.BasicOperation.EndFinally:
						break;
					case Bytecode.BasicOperation.EndFilter:
						break;
					case Bytecode.BasicOperation.LocalAllocation:
						break;
					case Bytecode.BasicOperation.InitBlock:
						break;
					case Bytecode.BasicOperation.InitObject:
						break;
					case Bytecode.BasicOperation.CopyObject:
						break;
					case Bytecode.BasicOperation.CopyBlock:
						break;
					case Bytecode.BasicOperation.LoadArrayLength:
						break;
					case Bytecode.BasicOperation.IndirectLoad:
						break;
					case Bytecode.BasicOperation.LoadArrayElement:
						break;
					case Bytecode.BasicOperation.LoadArrayElementAddress:
						break;
					case Bytecode.BasicOperation.IndirectStore:
						break;
					case Bytecode.BasicOperation.StoreArrayElement:
						break;
					case Bytecode.BasicOperation.Breakpoint:
						break;
					case Bytecode.BasicOperation.Return:
						break;

					default: throw op.Operation.ToUnknownValueException();
				}
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
					case Bytecode.BranchOperation.Leave:
						ProcessUnconditionalBranch(op);
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
				if (op.Operation == Bytecode.BranchOperation.Leave)
				{
					stack.Clear();
				}

				var instruction = new Tac.UnconditionalBranchInstruction(op.Offset, op.Target);
				body.Instructions.Add(instruction);
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

				var result = stack.Push();
				// Adding implicit this parameter
				// TODO: [Warning] Use of result variable before definition!
				arguments.Add(result);
				arguments.Reverse();

				var instruction = new Tac.CreateObjectInstruction(op.Offset, result, op.Constructor, arguments);
				body.Instructions.Add(instruction);
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
				var dest = stack.Push();
				var source = new StaticMethodReference(op.Method);
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
		private MapList<string, IExceptionHandlerBlock> exceptionHandlersEnd;

		public Disassembler(MethodDefinition methodDefinition)
		{
			this.method = methodDefinition;
			this.stack = new OperandStack(method.Body.MaxStack);
			this.exceptionHandlersStart = new MapList<string, IExceptionHandlerBlock>();
			this.exceptionHandlersEnd = new MapList<string, IExceptionHandlerBlock>();
		}

		public MethodBody Execute()
		{
			var body = new MethodBody();

			body.Parameters.AddRange(method.Body.Parameters);
			body.LocalVariables.UnionWith(method.Body.LocalVariables);
			body.ExceptionInformation.AddRange(method.Body.ExceptionInformation);

			if (method.Body.Instructions.Count > 0)
			{
				foreach (var protectedBlock in method.Body.ExceptionInformation)
				{
					exceptionHandlersStart.Add(protectedBlock.Start, protectedBlock);
					exceptionHandlersEnd.Add(protectedBlock.End, protectedBlock);
				}

				var translator = new Translator(body, stack);
				var cfanalysis = new ControlFlowAnalysis(method.Body);
				var cfg = cfanalysis.GenerateNormalControlFlow();

				var stackSizeAtEntry = new ushort?[cfg.Nodes.Count];
				var sorted_nodes = cfg.ForwardOrder;

				foreach (var node in sorted_nodes)
				{
					stack.Size = stackSizeAtEntry[node.Id].Value;
					this.ProcessBasicBlock(body, node, translator);

					foreach (var successor in node.Successors)
					{
						var stackSize = stackSizeAtEntry[successor.Id];

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
			}

			return body;
		}

		private void ProcessBasicBlock(MethodBody body, CFGNode node, Translator translator)
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

		private void ProcessSwitch(BasicBlockInfo bb, IOperation op)
		{
			var targets = op.Value as uint[];
			var operand = stack.Pop();

			var instruction = new SwitchInstruction(op.Offset, operand, targets);
			bb.Instructions.Add(instruction);

			foreach (var target in targets)
			{
				this.AddToPendingBasicBlocks(target, true);
			}
		}

		private void ProcessThrow(BasicBlockInfo bb, IOperation op)
		{
			var exception = stack.Pop();
			stack.Clear();

			var instruction = new ThrowInstruction(op.Offset, exception);
			bb.Instructions.Add(instruction);
		}

		private void ProcessRethrow(BasicBlockInfo bb, IOperation op)
		{
			var instruction = new ThrowInstruction(op.Offset);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLocalAllocation(BasicBlockInfo bb, IOperation op)
		{
			var numberOfBytes = stack.Pop();
			var targetAddress = stack.Push();

			var instruction = new LocalAllocationInstruction(op.Offset, targetAddress, numberOfBytes);
			bb.Instructions.Add(instruction);
		}

		private void ProcessInitializeMemory(BasicBlockInfo bb, IOperation op)
		{
			var numberOfBytes = stack.Pop();
			var fillValue = stack.Pop();
			var targetAddress = stack.Pop();

			var instruction = new InitializeMemoryInstruction(op.Offset, targetAddress, fillValue, numberOfBytes);
			bb.Instructions.Add(instruction);
		}

		private void ProcessInitializeObject(BasicBlockInfo bb, IOperation op)
		{
			var targetAddress = stack.Pop();
			var instruction = new InitializeObjectInstruction(op.Offset, targetAddress);
			bb.Instructions.Add(instruction);
		}

		private void ProcessCreateArray(BasicBlockInfo bb, IOperation op)
		{
			var arrayType = op.Value as IArrayTypeReference;
			var lowerBounds = new List<IVariable>();
			var sizes = new List<IVariable>();

			if (op.OperationCode == OperationCode.Array_Create_WithLowerBound)
			{
				for (uint i = 0; i < arrayType.Rank; i++)
				{
					var operand = stack.Pop();
					lowerBounds.Add(operand);
				}
			}

			for (uint i = 0; i < arrayType.Rank; i++)
			{
				var operand = stack.Pop();
				sizes.Add(operand);
			}

			lowerBounds.Reverse();
			sizes.Reverse();

			var result = stack.Push();
			var instruction = new CreateArrayInstruction(op.Offset, result, arrayType.ElementType, arrayType.Rank, lowerBounds, sizes);
			bb.Instructions.Add(instruction);
		}

		private void ProcessCopyObject(BasicBlockInfo bb, IOperation op)
		{
			var sourceAddress = stack.Pop();
			var targetAddress = stack.Pop();

			var instruction = new CopyObjectInstruction(op.Offset, targetAddress, sourceAddress);
			bb.Instructions.Add(instruction);
		}

		private void ProcessCopyMemory(BasicBlockInfo bb, IOperation op)
		{
			var numberOfBytes = stack.Pop();
			var sourceAddress = stack.Pop();
			var targetAddress = stack.Pop();

			var instruction = new CopyMemoryInstruction(op.Offset, targetAddress, sourceAddress, numberOfBytes);
			bb.Instructions.Add(instruction);
		}

		private void ProcessCreateObject(BasicBlockInfo bb, IOperation op)
		{
			var callee = op.Value as IMethodReference;
			var arguments = new List<IVariable>();

			foreach (var par in callee.Parameters)
			{
				var arg = stack.Pop();
				arguments.Add(arg);
			}

			foreach (var par in callee.ExtraParameters)
			{
				var arg = stack.Pop();
				arguments.Add(arg);
			}

			var result = stack.Push();
			// Adding implicit this parameter
			arguments.Add(result);
			arguments.Reverse();

			var instruction = new CreateObjectInstruction(op.Offset, result, callee, arguments);
			bb.Instructions.Add(instruction);
		}

		private void ProcessMethodCall(BasicBlockInfo bb, IOperation op)
		{
			var callee = op.Value as IMethodReference;
			var arguments = new List<IVariable>();
			IVariable result = null;

			foreach (var par in callee.Parameters)
			{
				var arg = stack.Pop();
				arguments.Add(arg);
			}

			foreach (var par in callee.ExtraParameters)
			{
				var arg = stack.Pop();
				arguments.Add(arg);
			}

			if (!callee.IsStatic)
			{
				// Adding implicit this parameter
				var argThis = stack.Pop();
				arguments.Add(argThis);
			}

			arguments.Reverse();

			if (callee.Type.TypeCode != PrimitiveTypeCode.Void)
			{
				result = stack.Push();
			}

			var instruction = new MethodCallInstruction(op.Offset, result, callee, arguments);
			bb.Instructions.Add(instruction);
		}

		private void ProcessMethodCallIndirect(BasicBlockInfo bb, IOperation op)
		{
			var calleeType = op.Value as IFunctionPointerTypeReference;
			var calleePointer = stack.Pop();
			var arguments = new List<IVariable>();
			IVariable result = null;

			foreach (var par in calleeType.Parameters)
			{
				var arg = stack.Pop();
				arguments.Add(arg);
			}

			if (!calleeType.IsStatic)
			{
				// Adding implicit this parameter
				var argThis = stack.Pop();
				arguments.Add(argThis);
			}

			arguments.Reverse();

			if (calleeType.Type.TypeCode != PrimitiveTypeCode.Void)
			{
				result = stack.Push();
			}

			var instruction = new IndirectMethodCallInstruction(op.Offset, result, calleePointer, calleeType, arguments);
			bb.Instructions.Add(instruction);
		}

		private void ProcessJumpCall(BasicBlockInfo bb, IOperation op)
		{
			var callee = op.Value as IMethodReference;
			var arguments = new List<IVariable>();
			IVariable result = null;

			if (!callee.IsStatic)
			{
				// Adding implicit this parameter
				arguments.Add(thisParameter);
			}

			arguments.AddRange(parameters.Values);

			if (callee.Type.TypeCode != PrimitiveTypeCode.Void)
			{
				result = stack.Push();
			}

			var instruction = new MethodCallInstruction(op.Offset, result, callee, arguments);
			bb.Instructions.Add(instruction);
		}

		private void ProcessSizeof(BasicBlockInfo bb, IOperation op)
		{
			var type = op.Value as ITypeReference;
			var result = stack.Push();
			var instruction = new SizeofInstruction(op.Offset, result, type);
			bb.Instructions.Add(instruction);
		}

		private void ProcessUnaryConditionalBranch(BasicBlockInfo bb, IOperation op)
		{
			var value = OperationHelper.GetUnaryConditionalBranchValue(op.OperationCode);
			var right = new Constant(value);
			var left = stack.Pop();
			var target = (uint)op.Value;
			var instruction = new ConditionalBranchInstruction(op.Offset, left, BranchOperation.Eq, right, target);
			bb.Instructions.Add(instruction);

			this.AddToPendingBasicBlocks(target, true);
		}

		private void ProcessBinaryConditionalBranch(BasicBlockInfo bb, IOperation op)
		{
			var condition = OperationHelper.ToBranchOperation(op.OperationCode);
			var unsigned = OperationHelper.OperandsAreUnsigned(op.OperationCode);

			var right = stack.Pop();
			var left = stack.Pop();
			var target = (uint)op.Value;
			var instruction = new ConditionalBranchInstruction(op.Offset, left, condition, right, target);
			instruction.UnsignedOperands = unsigned;
			bb.Instructions.Add(instruction);

			this.AddToPendingBasicBlocks(target, true);
		}

		private void ProcessEndFinally(BasicBlockInfo bb, IOperation op)
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
			//			bb.Instructions.Add(branch);
			//		}
			//	}
			//}
		}

		private void ProcessLeave(BasicBlockInfo bb, IOperation op)
		{
			var isTryFinallyEnd = false;
	
			stack.Clear();

			//if (exceptionHandlersEnd.ContainsKey(op.Offset))
			//{
			//	var handlers = exceptionHandlersEnd[op.Offset];
			//	var catchs = new List<BranchInstruction>();
			//	var finallys = new List<BranchInstruction>();

			//	foreach (var handler in handlers)
			//	{
			//		if (handler.Kind == ExceptionHandlerBlockKind.Try)
			//		{
			//			var tryHandler = handler as TryExceptionHandler;

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

			//	bb.Instructions.AddRange(catchs);
			//	bb.Instructions.AddRange(finallys);
			//}

			if (!isTryFinallyEnd)
			{
				var target = (uint)op.Value;
				var instruction = new UnconditionalBranchInstruction(op.Offset, target);
				bb.Instructions.Add(instruction);

				this.AddToPendingBasicBlocks(target, true);
			}
		}

		private void ProcessUnconditionalBranch(BasicBlockInfo bb, IOperation op)
		{
			var target = (uint)op.Value;
			var instruction = new UnconditionalBranchInstruction(op.Offset, target);
			bb.Instructions.Add(instruction);

			this.AddToPendingBasicBlocks(target, true);
		}

		private void ProcessReturn(BasicBlockInfo bb, IOperation op)
		{
			IVariable operand = null;

			if (method.Type.TypeCode != PrimitiveTypeCode.Void)
			{
				operand = stack.Pop();
			}

			var instruction = new ReturnInstruction(op.Offset, operand);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadConstant(BasicBlockInfo bb, IOperation op)
		{
			var type = OperationHelper.GetOperationType(op.OperationCode);

			var source = new Constant(op.Value) { Type = type };
			var dest = stack.Push();
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadArgument(BasicBlockInfo bb, IOperation op)
		{
			var source = thisParameter;

			if (op.Value is IParameterDefinition)
			{
				var argument = op.Value as IParameterDefinition;
				source = parameters[argument];
			}

			var dest = stack.Push();
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadArgumentAddress(BasicBlockInfo bb, IOperation op)
		{
			var operand = thisParameter;

			if (op.Value is IParameterDefinition)
			{
				var argument = op.Value as IParameterDefinition;
				operand = parameters[argument];
			}

			var dest = stack.Push();
			var source = new Reference(operand);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadLocal(BasicBlockInfo bb, IOperation op)
		{
			var local = op.Value as ILocalDefinition;
			var source = locals[local];
			var dest = stack.Push();
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadLocalAddress(BasicBlockInfo bb, IOperation op)
		{
			var local = op.Value as ILocalDefinition;
			var operand = locals[local];
			var dest = stack.Push();
			var source = new Reference(operand);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadIndirect(BasicBlockInfo bb, IOperation op)
		{
			var address = stack.Pop();
			var dest = stack.Push();
			var source = new Dereference(address);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadInstanceField(BasicBlockInfo bb, IOperation op)
		{
			var field = op.Value as IFieldReference;
			var obj = stack.Pop();
			var dest = stack.Push();
			var source = new InstanceFieldAccess(obj, field);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadStaticField(BasicBlockInfo bb, IOperation op)
		{
			var field = op.Value as IFieldReference;
			var dest = stack.Push();
			var source = new StaticFieldAccess(field);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadInstanceFieldAddress(BasicBlockInfo bb, IOperation op)
		{
			var field = op.Value as IFieldReference;
			var obj = stack.Pop();
			var dest = stack.Push();
			var access = new InstanceFieldAccess(obj, field);
			var source = new Reference(access);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadStaticFieldAddress(BasicBlockInfo bb, IOperation op)
		{
			var field = op.Value as IFieldReference;
			var dest = stack.Push();
			var access = new StaticFieldAccess(field);
			var source = new Reference(access);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadArrayLength(BasicBlockInfo bb, IOperation op)
		{
			var array = stack.Pop();
			var dest = stack.Push();
			var length = new ArrayLengthAccess(array);
			var instruction = new LoadInstruction(op.Offset, dest, length);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadArrayElement(BasicBlockInfo bb, IOperation op)
		{
			var index = stack.Pop();
			var array = stack.Pop();			
			var dest = stack.Push();
			var source = new ArrayElementAccess(array, index);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadArrayElementAddress(BasicBlockInfo bb, IOperation op)
		{
			var index = stack.Pop();
			var array = stack.Pop();
			var dest = stack.Push();
			var access = new ArrayElementAccess(array, index);
			var source = new Reference(access);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadMethodAddress(BasicBlockInfo bb, IOperation op)
		{
			var method = op.Value as IMethodReference;
			var dest = stack.Push();
			var source = new StaticMethodReference(method);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadVirtualMethodAddress(BasicBlockInfo bb, IOperation op)
		{
			var method = op.Value as IMethodReference;
			var obj = stack.Pop();
			var dest = stack.Push();
			var source = new VirtualMethodReference(obj, method);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessLoadToken(BasicBlockInfo bb, IOperation op)
		{
			var token = op.Value as IReference;
			var result = stack.Push();
			var instruction = new LoadTokenInstruction(op.Offset, result, token);
			bb.Instructions.Add(instruction);
		}

		private void ProcessStoreArgument(BasicBlockInfo bb, IOperation op)
		{
			var dest = thisParameter;

			if (op.Value is IParameterDefinition)
			{
				var argument = op.Value as IParameterDefinition;
				dest = parameters[argument];
			}
			
			var source = stack.Pop();
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessStoreLocal(BasicBlockInfo bb, IOperation op)
		{
			var local = op.Value as ILocalDefinition;
			var dest = locals[local];
			var source = stack.Pop();
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessStoreIndirect(BasicBlockInfo bb, IOperation op)
		{
			var source = stack.Pop();
			var address = stack.Pop();
			var dest = new Dereference(address);
			var instruction = new StoreInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessStoreInstanceField(BasicBlockInfo bb, IOperation op)
		{
			var field = op.Value as IFieldReference;
			var source = stack.Pop();
			var obj = stack.Pop();
			var dest = new InstanceFieldAccess(obj, field);
			var instruction = new StoreInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessStoreStaticField(BasicBlockInfo bb, IOperation op)
		{
			var field = op.Value as IFieldReference;
			var source = stack.Pop();
			var dest = new StaticFieldAccess(field);
			var instruction = new StoreInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessStoreArrayElement(BasicBlockInfo bb, IOperation op)
		{
			var source = stack.Pop();
			var index = stack.Pop();
			var array = stack.Pop();
			var dest = new ArrayElementAccess(array, index);
			var instruction = new StoreInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}

		private void ProcessEmptyOperation(BasicBlockInfo bb, IOperation op)
		{
			var instruction = new NopInstruction(op.Offset);
			bb.Instructions.Add(instruction);
		}

		private void ProcessBreakpointOperation(BasicBlockInfo bb, IOperation op)
		{
			var instruction = new BreakpointInstruction(op.Offset);
			bb.Instructions.Add(instruction);
		}

		private void ProcessUnaryOperation(BasicBlockInfo bb, IOperation op)
		{
			var operation = OperationHelper.ToUnaryOperation(op.OperationCode);

			var operand = stack.Pop();
			var dest = stack.Push();
			var instruction = new UnaryInstruction(op.Offset, dest, operation, operand);
			bb.Instructions.Add(instruction);
		}

		private void ProcessBinaryOperation(BasicBlockInfo bb, IOperation op)
		{
			var operation = OperationHelper.ToBinaryOperation(op.OperationCode);
			var overflow = OperationHelper.PerformsOverflowCheck(op.OperationCode);
			var unsigned = OperationHelper.OperandsAreUnsigned(op.OperationCode);

			var right = stack.Pop();
			var left = stack.Pop();
			var dest = stack.Push();
			var instruction = new BinaryInstruction(op.Offset, dest, left, operation, right);
			instruction.OverflowCheck = overflow;
			instruction.UnsignedOperands = unsigned;
			bb.Instructions.Add(instruction);
		}

		private void ProcessConversion(BasicBlockInfo bb, IOperation op)
		{
			var operation = OperationHelper.ToConvertOperation(op.OperationCode);
			var overflow = OperationHelper.PerformsOverflowCheck(op.OperationCode);
			var unsigned = OperationHelper.OperandsAreUnsigned(op.OperationCode);

			var type = op.Value as ITypeReference;

			if (operation == ConvertOperation.Box && type.IsValueType)
			{
				type = Types.Instance.PlatformType.SystemObject;
			}
			else if (operation == ConvertOperation.Conv)
			{
				type = OperationHelper.GetOperationType(op.OperationCode);
			}

			var operand = stack.Pop();
			var result = stack.Push();
			var instruction = new ConvertInstruction(op.Offset, result, operand, operation, type);
			instruction.OverflowCheck = overflow;
			instruction.UnsignedOperands = unsigned;
			bb.Instructions.Add(instruction);
		}

		private void ProcessDup(BasicBlockInfo bb, IOperation op)
		{
			var source = stack.Top();
			var dest = stack.Push();
			var instruction = new LoadInstruction(op.Offset, dest, source);
			bb.Instructions.Add(instruction);
		}
	}
}
