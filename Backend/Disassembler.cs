using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Backend.Operands;
using Backend.Instructions;

namespace Backend
{
	public class Disassembler
	{
		#region class OperandStack

		class OperandStack
		{
			private TemporalVariable[] stack;
			private ushort top;

			public OperandStack(ushort capacity)
			{
				stack = new TemporalVariable[capacity];

				for (var i = 0u; i < capacity; ++i)
					stack[i] = new TemporalVariable(i);
			}

			public IEnumerable<TemporalVariable> Variables
			{
				get { return this.stack; }
			}

			public int Capacity
			{
				get { return stack.Length; }
			}

			public int Size
			{
				get { return top; }
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

			public TemporalVariable Peek()
			{
				if (top <= 0) throw new InvalidOperationException();
				return stack[top - 1];
			}
		}

		#endregion

		#region Try Catch Finally information

		enum ContextKind
		{
			None,
			Try,
			Catch,
			Finally
		}

		class TryInformation
		{
			public uint BeginOffset { get; set; }
			public uint EndOffset { get; set; }
			public IDictionary<uint, CatchInformation> ExceptionHandlers { get; private set; }
			public FinallyInformation Finally { get; set; }

			public TryInformation(uint begin, uint end)
			{
				this.BeginOffset = begin;
				this.EndOffset = end;
				this.ExceptionHandlers = new Dictionary<uint, CatchInformation>();
			}
		}

		class CatchInformation
		{
			public ITypeReference ExceptionType { get; set; }
			public uint BeginOffset { get; set; }
			public uint EndOffset { get; set; }

			public CatchInformation(uint begin, uint end, ITypeReference exceptionType)
			{
				this.BeginOffset = begin;
				this.EndOffset = end;
				this.ExceptionType = exceptionType;
			}
		}

		class FinallyInformation
		{
			public uint BeginOffset { get; set; }
			public uint EndOffset { get; set; }

			public FinallyInformation(uint begin, uint end)
			{
				this.BeginOffset = begin;
				this.EndOffset = end;
			}
		}

		#endregion

		private IMetadataHost host;
		private IMethodDefinition method;
		private ISourceLocationProvider sourceLocationProvider;
		private LocalVariable thisParameter;
		private IDictionary<IParameterDefinition, LocalVariable> parameters;
		private IDictionary<ILocalDefinition, LocalVariable> locals;
		private IDictionary<uint, TryInformation> trys;
		private OperandStack stack;

		public Disassembler(IMetadataHost host, IMethodDefinition methodDefinition, ISourceLocationProvider sourceLocationProvider)
		{
			this.host = host;
			this.method = methodDefinition;
			this.sourceLocationProvider = sourceLocationProvider;
			this.parameters = new Dictionary<IParameterDefinition, LocalVariable>();
			this.locals = new Dictionary<ILocalDefinition, LocalVariable>();
			this.trys = new Dictionary<uint, TryInformation>();
			this.stack = new OperandStack(method.Body.MaxStack);

			if (!method.IsStatic)
			{
				this.thisParameter = new LocalVariable("this");
			}

			foreach (var parameter in method.Parameters)
			{
				var p = new LocalVariable(parameter.Name.Value);
				this.parameters.Add(parameter, p);
			}

			foreach (var local in method.Body.LocalVariables)
			{
				var name = this.GetLocalSourceName(local);
				var l = new LocalVariable(name);
				this.locals.Add(local, l);
			}

			foreach (var exinf in method.Body.OperationExceptionInformation)
			{
				TryInformation tryInfo;

				if (this.trys.ContainsKey(exinf.TryStartOffset))
				{
					tryInfo = this.trys[exinf.TryStartOffset];
				}
				else
				{
					tryInfo = new TryInformation(exinf.TryStartOffset, exinf.TryEndOffset);
					this.trys.Add(tryInfo.BeginOffset, tryInfo);
				}

				switch (exinf.HandlerKind)
				{
					case HandlerKind.Finally:
						tryInfo.Finally = new FinallyInformation(exinf.HandlerStartOffset, exinf.HandlerEndOffset);
						break;

					case HandlerKind.Catch:
						var catchInfo = new CatchInformation(exinf.HandlerStartOffset, exinf.HandlerEndOffset, exinf.ExceptionType);
						tryInfo.ExceptionHandlers.Add(catchInfo.BeginOffset, catchInfo);
						break;
				}
			}
		}

		public MethodBody Execute()
		{
			var body = new MethodBody(method);
			var contextKind = ContextKind.None;
			TryInformation tryInfo = null;

			foreach (var variable in stack.Variables)
			{
				body.Variables.Add(variable);
			}

			foreach (var op in method.Body.Operations)
			{
				Instruction instruction;

				if (this.trys.ContainsKey(op.Offset))
				{
					contextKind = ContextKind.Try;
					tryInfo = this.trys[op.Offset];					

					instruction = new TryInstruction(op.Offset);
					body.Instructions.Add(instruction);
				}

				if (tryInfo != null)
				{
					if (tryInfo.ExceptionHandlers.ContainsKey(op.Offset))
					{
						contextKind = ContextKind.Catch;
						var catchInfo = tryInfo.ExceptionHandlers[op.Offset];						
						// push the exception into the stack
						var ex = stack.Push();

						instruction = new CatchInstruction(op.Offset, ex, catchInfo.ExceptionType);
						body.Instructions.Add(instruction);
					}

					if (tryInfo.Finally != null && tryInfo.Finally.BeginOffset == op.Offset)
					{
						contextKind = ContextKind.Finally;
						instruction = new FinallyInstruction(op.Offset);
						body.Instructions.Add(instruction);
					}
				}

				instruction = null;

				switch (op.OperationCode)
				{
					case OperationCode.Add:
					case OperationCode.Add_Ovf:
					case OperationCode.Add_Ovf_Un:
						instruction = this.ProcessBinaryOperation(op, BinaryOperation.Add);
						break;

					case OperationCode.And:
						instruction = this.ProcessBinaryOperation(op, BinaryOperation.And);
						break;

					case OperationCode.Ceq:
						instruction = this.ProcessBinaryOperation(op, BinaryOperation.Eq);
						break;

					case OperationCode.Cgt:
					case OperationCode.Cgt_Un:
						instruction = this.ProcessBinaryOperation(op, BinaryOperation.Gt);
						break;

					case OperationCode.Clt:
					case OperationCode.Clt_Un:
						instruction = this.ProcessBinaryOperation(op, BinaryOperation.Lt);
						break;

					case OperationCode.Div:
					case OperationCode.Div_Un:
						instruction = this.ProcessBinaryOperation(op, BinaryOperation.Div);
						break;

					case OperationCode.Mul:
					case OperationCode.Mul_Ovf:
					case OperationCode.Mul_Ovf_Un:
						instruction = this.ProcessBinaryOperation(op, BinaryOperation.Mul);
						break;

					case OperationCode.Or:
						instruction = this.ProcessBinaryOperation(op, BinaryOperation.Or);
						break;

					case OperationCode.Rem:
					case OperationCode.Rem_Un:
						instruction = this.ProcessBinaryOperation(op, BinaryOperation.Rem);
						break;

					case OperationCode.Shl:
						instruction = this.ProcessBinaryOperation(op, BinaryOperation.Shl);
						break;

					case OperationCode.Shr:
					case OperationCode.Shr_Un:
						instruction = this.ProcessBinaryOperation(op, BinaryOperation.Shr);
						break;

					case OperationCode.Sub:
					case OperationCode.Sub_Ovf:
					case OperationCode.Sub_Ovf_Un:
						instruction = this.ProcessBinaryOperation(op, BinaryOperation.Sub);
						break;

					case OperationCode.Xor:
						instruction = this.ProcessBinaryOperation(op, BinaryOperation.Xor);
						break;

					//case OperationCode.Arglist:
					//    //expression = new RuntimeArgumentHandleExpression();
					//    break;

					case OperationCode.Array_Create_WithLowerBound:
						instruction = this.ProcessCreateArray(op, true);
						break;

					case OperationCode.Array_Create:
					case OperationCode.Newarr:
						instruction = this.ProcessCreateArray(op, false);
						break;

					case OperationCode.Array_Get:
					case OperationCode.Ldelem:
					case OperationCode.Ldelem_I:
					case OperationCode.Ldelem_I1:
					case OperationCode.Ldelem_I2:
					case OperationCode.Ldelem_I4:
					case OperationCode.Ldelem_I8:
					case OperationCode.Ldelem_R4:
					case OperationCode.Ldelem_R8:
					case OperationCode.Ldelem_U1:
					case OperationCode.Ldelem_U2:
					case OperationCode.Ldelem_U4:
					case OperationCode.Ldelem_Ref:
						instruction = this.ProcessLoadArrayElement(op);
						break;

					case OperationCode.Array_Addr:
					case OperationCode.Ldelema:
						instruction = this.ProcessLoadArrayElementAddress(op);
						break;

					case OperationCode.Beq:
					case OperationCode.Beq_S:
						instruction = this.ProcessBinaryConditionalBranch(op, BranchCondition.Eq);
						break;

					case OperationCode.Bne_Un:
					case OperationCode.Bne_Un_S:
						instruction = this.ProcessBinaryConditionalBranch(op, BranchCondition.Neq);
						break;

					case OperationCode.Bge:
					case OperationCode.Bge_S:
					case OperationCode.Bge_Un:
					case OperationCode.Bge_Un_S:
						instruction = this.ProcessBinaryConditionalBranch(op, BranchCondition.Ge);
						break;

					case OperationCode.Bgt:
					case OperationCode.Bgt_S:
					case OperationCode.Bgt_Un:
					case OperationCode.Bgt_Un_S:
						instruction = this.ProcessBinaryConditionalBranch(op, BranchCondition.Gt);
						break;

					case OperationCode.Ble:
					case OperationCode.Ble_S:
					case OperationCode.Ble_Un:
					case OperationCode.Ble_Un_S:
						instruction = this.ProcessBinaryConditionalBranch(op, BranchCondition.Le);
						break;

					case OperationCode.Blt:
					case OperationCode.Blt_S:
					case OperationCode.Blt_Un:
					case OperationCode.Blt_Un_S:
						instruction = this.ProcessBinaryConditionalBranch(op, BranchCondition.Lt);
						break;

					case OperationCode.Br:
					case OperationCode.Br_S:
						instruction = this.ProcessUnconditionalBranch(op);
						break;

					case OperationCode.Leave:
					case OperationCode.Leave_S:
						string target = string.Format("L_{0:X4}", op.Value);

						if (contextKind == ContextKind.Try)
						{
							foreach (var catchInfo in tryInfo.ExceptionHandlers)
							{
								instruction = new ExceptionalBranchInstruction(op.Offset, catchInfo.Value.BeginOffset, catchInfo.Value.ExceptionType);
								body.Instructions.Add(instruction);
							}
						}
						
						if (contextKind == ContextKind.None ||
							tryInfo.ExceptionHandlers.Count == 0)
						{
							target = string.Format("L_{0:X4}'", tryInfo.Finally.BeginOffset);
						}

						contextKind = ContextKind.None;
						instruction = this.ProcessLeave(op, target);
						break;

					case OperationCode.Break:
						instruction = this.ProcessBreakpointOperation(op);
						break;

					case OperationCode.Nop:
						instruction = this.ProcessEmptyOperation(op);
						break;

					case OperationCode.Brfalse:
					case OperationCode.Brfalse_S:
						instruction = this.ProcessUnaryConditionalBranch(op, false);
						break;

					case OperationCode.Brtrue:
					case OperationCode.Brtrue_S:
						instruction = this.ProcessUnaryConditionalBranch(op, true);
						break;

					case OperationCode.Call:
					case OperationCode.Callvirt:
						instruction = this.ProcessMethodCall(op);
						break;

					case OperationCode.Jmp:
						instruction = this.ProcessJumpCall(op);
						break;

					case OperationCode.Calli:
						instruction = this.ProcessMethodCallIndirect(op);
						break;

					case OperationCode.Castclass:
					case OperationCode.Isinst:
					case OperationCode.Box:
					case OperationCode.Unbox:
					case OperationCode.Unbox_Any:
						instruction = this.ProcessConversion(op);
						break;

					case OperationCode.Conv_I:
					case OperationCode.Conv_Ovf_I:
					case OperationCode.Conv_Ovf_I_Un:
						instruction = this.ProcessConversion(op, host.PlatformType.SystemIntPtr);
						break;

					case OperationCode.Conv_I1:
					case OperationCode.Conv_Ovf_I1:
					case OperationCode.Conv_Ovf_I1_Un:
						instruction = this.ProcessConversion(op, host.PlatformType.SystemInt8);
						break;

					case OperationCode.Conv_I2:
					case OperationCode.Conv_Ovf_I2:
					case OperationCode.Conv_Ovf_I2_Un:
						instruction = this.ProcessConversion(op, host.PlatformType.SystemInt16);
						break;

					case OperationCode.Conv_I4:
					case OperationCode.Conv_Ovf_I4:
					case OperationCode.Conv_Ovf_I4_Un:
						instruction = this.ProcessConversion(op, host.PlatformType.SystemInt32);
						break;

					case OperationCode.Conv_I8:
					case OperationCode.Conv_Ovf_I8:
					case OperationCode.Conv_Ovf_I8_Un:
						instruction = this.ProcessConversion(op, host.PlatformType.SystemInt64);
						break;

					case OperationCode.Conv_U:
					case OperationCode.Conv_Ovf_U:
					case OperationCode.Conv_Ovf_U_Un:
						instruction = this.ProcessConversion(op, host.PlatformType.SystemUIntPtr);
						break;

					case OperationCode.Conv_U1:
					case OperationCode.Conv_Ovf_U1:
					case OperationCode.Conv_Ovf_U1_Un:
						instruction = this.ProcessConversion(op, host.PlatformType.SystemUInt8);
						break;

					case OperationCode.Conv_U2:
					case OperationCode.Conv_Ovf_U2:
					case OperationCode.Conv_Ovf_U2_Un:
						instruction = this.ProcessConversion(op, host.PlatformType.SystemUInt16);
						break;

					case OperationCode.Conv_U4:
					case OperationCode.Conv_Ovf_U4:
					case OperationCode.Conv_Ovf_U4_Un:
						instruction = this.ProcessConversion(op, host.PlatformType.SystemUInt32);
						break;

					case OperationCode.Conv_U8:
					case OperationCode.Conv_Ovf_U8:
					case OperationCode.Conv_Ovf_U8_Un:
						instruction = this.ProcessConversion(op, host.PlatformType.SystemUInt64);
						break;

					case OperationCode.Conv_R4:
						instruction = this.ProcessConversion(op, host.PlatformType.SystemFloat32);
						break;

					case OperationCode.Conv_R8:
					case OperationCode.Conv_R_Un:
						instruction = this.ProcessConversion(op, host.PlatformType.SystemFloat64);
						break;

					//case OperationCode.Ckfinite:
					//    var operand = this.PopOperandStack();
					//    var chkfinite = new MutableCodeModel.MethodReference()
					//    {
					//        CallingConvention = Cci.CallingConvention.FastCall,
					//        ContainingType = host.PlatformType.SystemFloat64,
					//        Name = this.host.NameTable.GetNameFor("__ckfinite__"),
					//        Type = host.PlatformType.SystemFloat64,
					//        InternFactory = host.InternFactory,
					//    };
					//    expression = new MethodCall() { Arguments = new List<IExpression>(1) { operand }, IsStaticCall = true, Type = operand.Type, MethodToCall = chkfinite };
					//    break;

					case OperationCode.Constrained_:
						//This prefix is redundant and is not represented in the code model.
						continue;

					case OperationCode.Cpblk:
						instruction = this.ProcessCopyMemory(op);
						break;

					case OperationCode.Cpobj:
						instruction = this.ProcessCopyObject(op);
						break;

					case OperationCode.Dup:
						instruction = this.ProcessDup(op);
						break;

					//case OperationCode.Endfilter:
					//    statement = this.ParseEndfilter();
					//    break;

					case OperationCode.Endfinally:
						//stack.Clear();
						//continue;
						target = string.Format("L_{0:X4}", tryInfo.Finally.EndOffset);
						contextKind = ContextKind.None;
						instruction = this.ProcessLeave(op, target);
						break;

					case OperationCode.Initblk:
						instruction = this.ProcessInitializeMemory(op);
						break;

					case OperationCode.Initobj:
						instruction = this.ProcessInitializeObject(op);
						break;

					case OperationCode.Ldarg:
					case OperationCode.Ldarg_0:
					case OperationCode.Ldarg_1:
					case OperationCode.Ldarg_2:
					case OperationCode.Ldarg_3:
					case OperationCode.Ldarg_S:
						instruction = this.ProcessLoadArgument(op);
					    break;

					case OperationCode.Ldarga:
					case OperationCode.Ldarga_S:
						instruction = this.ProcessLoadArgumentAddress(op);
						break;

					case OperationCode.Ldloc:
					case OperationCode.Ldloc_0:
					case OperationCode.Ldloc_1:
					case OperationCode.Ldloc_2:
					case OperationCode.Ldloc_3:
					case OperationCode.Ldloc_S:
					    instruction = this.ProcessLoadLocal(op);
					    break;

					case OperationCode.Ldloca:
					case OperationCode.Ldloca_S:
						instruction = this.ProcessLoadLocalAddress(op);
					    break;

					case OperationCode.Ldfld:
						instruction = this.ProcessLoadInstanceField(op);
						break;

					case OperationCode.Ldsfld:
						instruction = this.ProcessLoadStaticField(op);
						break;

					case OperationCode.Ldflda:
						instruction = this.ProcessLoadInstanceFieldAddress(op);
						break;

					case OperationCode.Ldsflda:
						instruction = this.ProcessLoadStaticFieldAddress(op);
						break;

					case OperationCode.Ldftn:
						instruction = this.ProcessLoadMethodAddress(op);
						break;

					case OperationCode.Ldvirtftn:
						instruction = this.ProcessLoadVirtualMethodAddress(op);
						break;

					case OperationCode.Ldc_I4:
					case OperationCode.Ldc_I4_0:
					case OperationCode.Ldc_I4_1:
					case OperationCode.Ldc_I4_2:
					case OperationCode.Ldc_I4_3:
					case OperationCode.Ldc_I4_4:
					case OperationCode.Ldc_I4_5:
					case OperationCode.Ldc_I4_6:
					case OperationCode.Ldc_I4_7:
					case OperationCode.Ldc_I4_8:
					case OperationCode.Ldc_I4_M1:
					case OperationCode.Ldc_I4_S:
					case OperationCode.Ldc_I8:
					case OperationCode.Ldc_R4:
					case OperationCode.Ldc_R8:
					case OperationCode.Ldnull:
					case OperationCode.Ldstr:
						instruction = this.ProcessLoadConstant(op);
						break;

					case OperationCode.Ldind_I:
					case OperationCode.Ldind_I1:
					case OperationCode.Ldind_I2:
					case OperationCode.Ldind_I4:
					case OperationCode.Ldind_I8:
					case OperationCode.Ldind_R4:
					case OperationCode.Ldind_R8:
					case OperationCode.Ldind_Ref:
					case OperationCode.Ldind_U1:
					case OperationCode.Ldind_U2:
					case OperationCode.Ldind_U4:
					case OperationCode.Ldobj:
						instruction = this.ProcessLoadIndirect(op);
						break;

					case OperationCode.Ldlen:
						instruction = this.ProcessLoadArrayLength(op);
						break;

					//case OperationCode.Ldtoken:
					//    expression = ParseToken(currentOperation);
					//    break;

					case OperationCode.Localloc:
						instruction = this.ProcessLocalAllocation(op);
						break;

					//case OperationCode.Mkrefany:
					//    expression = this.ParseMakeTypedReference(currentOperation);
					//    break;

					case OperationCode.Neg:
						instruction = this.ProcessUnaryOperation(op, UnaryOperation.Neg);
						break;

					case OperationCode.Not:
						instruction = this.ProcessUnaryOperation(op, UnaryOperation.Not);
						break;

					case OperationCode.Newobj:
						instruction = this.ProcessCreateObject(op);
						break;

					case OperationCode.No_:
						//if code out there actually uses this, I need to know sooner rather than later.
						//TODO: need object model support
						throw new NotImplementedException("Invalid opcode: No.");

					case OperationCode.Pop:
						stack.Pop();
						continue;

					//case OperationCode.Readonly_:
					//    this.sawReadonly = true;
					//    break;

					//case OperationCode.Refanytype:
					//    expression = this.ParseGetTypeOfTypedReference();
					//    break;

					//case OperationCode.Refanyval:
					//    expression = this.ParseGetValueOfTypedReference(currentOperation);
					//    break;

					case OperationCode.Ret:
						instruction = this.ProcessReturn(op);
						break;

					case OperationCode.Sizeof:
						instruction = this.ProcessSizeof(op);
						break;

					case OperationCode.Starg:
					case OperationCode.Starg_S:
						instruction = this.ProcessStoreArgument(op);
					    break;

					case OperationCode.Array_Set:
					case OperationCode.Stelem:
					case OperationCode.Stelem_I:
					case OperationCode.Stelem_I1:
					case OperationCode.Stelem_I2:
					case OperationCode.Stelem_I4:
					case OperationCode.Stelem_I8:
					case OperationCode.Stelem_R4:
					case OperationCode.Stelem_R8:
					case OperationCode.Stelem_Ref:
						instruction = this.ProcessStoreArrayElement(op);
						break;

					case OperationCode.Stfld:
						instruction = this.ProcessStoreInstanceField(op);
						break;

					case OperationCode.Stsfld:
						instruction = this.ProcessStoreStaticField(op);
						break;

					case OperationCode.Stind_I:
					case OperationCode.Stind_I1:
					case OperationCode.Stind_I2:
					case OperationCode.Stind_I4:
					case OperationCode.Stind_I8:
					case OperationCode.Stind_R4:
					case OperationCode.Stind_R8:
					case OperationCode.Stind_Ref:
					case OperationCode.Stobj:
						instruction = this.ProcessStoreIndirect(op);
					    break;

					case OperationCode.Stloc:
					case OperationCode.Stloc_0:
					case OperationCode.Stloc_1:
					case OperationCode.Stloc_2:
					case OperationCode.Stloc_3:
					case OperationCode.Stloc_S:
					    instruction = this.ProcessStoreLocal(op);
					    break;

					//case OperationCode.Switch:
					//    statement = this.ParseSwitchInstruction(currentOperation);
					//    break;

					//case OperationCode.Tail_:
					//    this.sawTailCall = true;
					//    break;

					//case OperationCode.Throw:
					//    statement = this.ParseThrow();
					//    break;

					//case OperationCode.Rethrow:
					//    statement = new RethrowStatement();
					//    break;

					//case OperationCode.Unaligned_:
					//    Contract.Assume(currentOperation.Value is byte);
					//    var alignment = (byte)currentOperation.Value;
					//    Contract.Assume(alignment == 1 || alignment == 2 || alignment == 4);
					//    this.alignment = alignment;
					//    break;

					//case OperationCode.Volatile_:
					//    this.sawVolatile = true;
					//    break;

					default:
						//throw new UnknownBytecodeException(op);
						System.Console.WriteLine("Unknown bytecode: {0}", op.OperationCode);
						break;
				}

				body.Instructions.Add(instruction);
			}

			return body;
		}

		private Instruction ProcessLocalAllocation(IOperation op)
		{
			var numberOfBytes = stack.Pop();
			var targetAddress = stack.Push();

			var instruction = new LocalAllocationInstruction(op.Offset, targetAddress, numberOfBytes);
			return instruction;
		}

		private Instruction ProcessInitializeMemory(IOperation op)
		{
			var numberOfBytes = stack.Pop();
			var fillValue = stack.Pop();
			var targetAddress = stack.Pop();

			var instruction = new InitializeMemoryInstruction(op.Offset, targetAddress, fillValue, numberOfBytes);
			return instruction;
		}

		private Instruction ProcessInitializeObject(IOperation op)
		{
			var targetAddress = stack.Pop();
			var instruction = new InitializeObjectInstruction(op.Offset, targetAddress);
			return instruction;
		}

		private Instruction ProcessCreateArray(IOperation op, bool withLowerBounds)
		{
			var arrayType = op.Value as IArrayTypeReference;
			var elementType = arrayType.ElementType;
			var rank = arrayType.Rank;
			var lowerBounds = new List<Operand>();
			var sizes = new List<Operand>();

			if (withLowerBounds)
			{
				for (uint i = 0; i < arrayType.Rank; i++)
				{
					var operand = stack.Pop();
					lowerBounds.Add(operand);
				}
			}
			else
			{
				for (uint i = 0; i < arrayType.Rank; i++)
				{
					var operand = new Constant(0);
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
			var instruction = new CreateArrayInstruction(op.Offset, result, elementType, rank, lowerBounds, sizes);
			return instruction;
		}

		private Instruction ProcessCopyObject(IOperation op)
		{
			var sourceAddress = stack.Pop();
			var targetAddress = stack.Pop();

			var instruction = new CopyObjectInstruction(op.Offset, targetAddress, sourceAddress);
			return instruction;
		}

		private Instruction ProcessCopyMemory(IOperation op)
		{
			var numberOfBytes = stack.Pop();
			var sourceAddress = stack.Pop();
			var targetAddress = stack.Pop();

			var instruction = new CopyMemoryInstruction(op.Offset, targetAddress, sourceAddress, numberOfBytes);
			return instruction;
		}

		private Instruction ProcessCreateObject(IOperation op)
		{
			var callee = op.Value as IMethodReference;
			var arguments = new List<Operand>();

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
			arguments.Add(result);
			arguments.Reverse();

			var instruction = new CreateObjectInstruction(op.Offset, result, callee, arguments);
			return instruction;
		}

		private Instruction ProcessMethodCall(IOperation op)
		{
			var callee = op.Value as IMethodReference;
			var arguments = new List<Operand>();
			Variable result = null;

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
				result = stack.Push();

			var instruction = new MethodCallInstruction(op.Offset, result, callee, arguments);
			return instruction;
		}

		private Instruction ProcessMethodCallIndirect(IOperation op)
		{
			var calleeType = op.Value as IFunctionPointerTypeReference;
			var calleePointer = stack.Pop();
			var arguments = new List<Operand>();
			Variable result = null;

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
				result = stack.Push();

			var instruction = new IndirectMethodCallInstruction(op.Offset, result, calleePointer, calleeType, arguments);
			return instruction;
		}

		private Instruction ProcessJumpCall(IOperation op)
		{
			var callee = op.Value as IMethodReference;
			var arguments = new List<Operand>();
			Variable result = null;

			if (!callee.IsStatic)
			{
				// Adding implicit this parameter
				arguments.Add(thisParameter);
			}

			foreach (var par in parameters)
			{
				var arg = par.Value;
				arguments.Add(arg);
			}

			if (callee.Type.TypeCode != PrimitiveTypeCode.Void)
				result = stack.Push();

			var instruction = new MethodCallInstruction(op.Offset, result, callee, arguments);
			return instruction;
		}

		private Instruction ProcessSizeof(IOperation op)
		{
			var type = op.Value as ITypeReference;
			var result = stack.Push();
			var instruction = new SizeofInstruction(op.Offset, result, type);
			return instruction;
		}

		private Instruction ProcessUnaryConditionalBranch(IOperation op, bool value)
		{
			var right = new Constant(value);
			var left = stack.Pop();
			var target = (uint)op.Value;
			var instruction = new ConditionalBranchInstruction(op.Offset, left, BranchCondition.Eq, right, target);
			return instruction;
		}

		private Instruction ProcessBinaryConditionalBranch(IOperation op, BranchCondition condition)
		{
			var right = stack.Pop();
			var left = stack.Pop();
			var target = (uint)op.Value;
			var instruction = new ConditionalBranchInstruction(op.Offset, left, condition, right, target);
			return instruction;
		}

		private Instruction ProcessLeave(IOperation op, string target)
		{
			stack.Clear();
			var instruction = new UnconditionalBranchInstruction(op.Offset, 0);
			instruction.Target = target;
			return instruction;
		}

		private Instruction ProcessUnconditionalBranch(IOperation op)
		{
			var target = (uint)op.Value;
			var instruction = new UnconditionalBranchInstruction(op.Offset, target);
			return instruction;
		}

		private Instruction ProcessReturn(IOperation op)
		{
			Operand operand = null;

			if (method.Type.TypeCode != PrimitiveTypeCode.Void)
				operand = stack.Pop();

			var instruction = new ReturnInstruction(op.Offset, operand);
			return instruction;
		}

		private Instruction ProcessLoadConstant(IOperation op)
		{
			var source = new Constant(op.Value);
			var dest = stack.Push();
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Assign, source);
			return instruction;
		}

		private Instruction ProcessLoadArgument(IOperation op)
		{
			var source = thisParameter;

			if (op.Value is IParameterDefinition)
			{
				var argument = op.Value as IParameterDefinition;
				source = parameters[argument];
			}

			var dest = stack.Push();
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Assign, source);
			return instruction;
		}

		private Instruction ProcessLoadArgumentAddress(IOperation op)
		{
			var source = thisParameter;

			if (op.Value is IParameterDefinition)
			{
				var argument = op.Value as IParameterDefinition;
				source = parameters[argument];
			}

			var dest = stack.Push();
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.AddressOf, source);
			return instruction;
		}

		private Instruction ProcessLoadLocal(IOperation op)
		{
			var local = op.Value as ILocalDefinition;
			var source = locals[local];
			var dest = stack.Push();
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Assign, source);
			return instruction;
		}

		private Instruction ProcessLoadLocalAddress(IOperation op)
		{
			var local = op.Value as ILocalDefinition;
			var source = locals[local];
			var dest = stack.Push();
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.AddressOf, source);
			return instruction;
		}

		private Instruction ProcessLoadIndirect(IOperation op)
		{
			var source = stack.Pop();
			var dest = stack.Push();
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.GetValueAt, source);
			return instruction;
		}

		private Instruction ProcessLoadInstanceField(IOperation op)
		{
			var field = op.Value as IFieldDefinition;
			var obj = stack.Pop();
			var dest = stack.Push();
			var source = new InstanceFieldAccess(obj, field.Name.Value);
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Assign, source);
			return instruction;
		}

		private Instruction ProcessLoadStaticField(IOperation op)
		{
			var field = op.Value as IFieldDefinition;
			var dest = stack.Push();
			var source = new StaticFieldAccess(field.ContainingType, field.Name.Value);
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Assign, source);
			return instruction;
		}

		private Instruction ProcessLoadInstanceFieldAddress(IOperation op)
		{
			var field = op.Value as IFieldDefinition;
			var obj = stack.Pop();
			var dest = stack.Push();
			var source = new InstanceFieldAccess(obj, field.Name.Value);
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.AddressOf, source);
			return instruction;
		}

		private Instruction ProcessLoadStaticFieldAddress(IOperation op)
		{
			var field = op.Value as IFieldDefinition;
			var dest = stack.Push();
			var source = new StaticFieldAccess(field.ContainingType, field.Name.Value);
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.AddressOf, source);
			return instruction;
		}

		private Instruction ProcessLoadArrayLength(IOperation op)
		{
			var array = stack.Pop();
			var dest = stack.Push();
			var length = new InstanceFieldAccess(array, "Length");
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Assign, length);
			return instruction;
		}

		private Instruction ProcessLoadArrayElement(IOperation op)
		{
			var index = stack.Pop();
			var array = stack.Pop();			
			var dest = stack.Push();
			var source = new ArrayElementAccess(array, index);
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Assign, source);
			return instruction;
		}

		private Instruction ProcessLoadArrayElementAddress(IOperation op)
		{
			var index = stack.Pop();
			var array = stack.Pop();
			var dest = stack.Push();
			var source = new ArrayElementAccess(array, index);
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.AddressOf, source);
			return instruction;
		}

		private Instruction ProcessLoadMethodAddress(IOperation op)
		{
			var method = op.Value as IMethodReference;
			var source = new StaticMethod(method);
			var dest = stack.Push();
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.AddressOf, source);
			return instruction;
		}

		private Instruction ProcessLoadVirtualMethodAddress(IOperation op)
		{
			var method = op.Value as IMethodReference;
			var obj = stack.Pop();
			var dest = stack.Push();
			var source = new VirtualMethod(obj, method);
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.AddressOf, source);
			return instruction;
		}

		private Instruction ProcessStoreArgument(IOperation op)
		{
			var dest = thisParameter;

			if (op.Value is IParameterDefinition)
			{
				var argument = op.Value as IParameterDefinition;
				dest = parameters[argument];
			}
			
			var source = stack.Pop();
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Assign, source);
			return instruction;
		}

		private Instruction ProcessStoreLocal(IOperation op)
		{
			var local = op.Value as ILocalDefinition;
			var dest = locals[local];
			var source = stack.Pop();
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Assign, source);
			return instruction;
		}

		private Instruction ProcessStoreIndirect(IOperation op)
		{
			var source = stack.Pop();
			var dest = stack.Pop();
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.SetValueAt, source);
			return instruction;
		}

		private Instruction ProcessStoreInstanceField(IOperation op)
		{
			var field = op.Value as IFieldDefinition;
			var source = stack.Pop();
			var obj = stack.Pop();
			var dest = new InstanceFieldAccess(obj, field.Name.Value);
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Assign, source);
			return instruction;
		}

		private Instruction ProcessStoreStaticField(IOperation op)
		{
			var field = op.Value as IFieldDefinition;
			var source = stack.Pop();
			var dest = new StaticFieldAccess(field.ContainingType, field.Name.Value);
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Assign, source);
			return instruction;
		}

		private Instruction ProcessStoreArrayElement(IOperation op)
		{
			var operand = stack.Pop();
			var index = stack.Pop();
			var array = stack.Pop();
			var result = new ArrayElementAccess(array, index);
			var instruction = new UnaryInstruction(op.Offset, result, UnaryOperation.Assign, operand);
			return instruction;
		}

		private Instruction ProcessEmptyOperation(IOperation op)
		{
			var instruction = new EmptyInstruction(op.Offset);
			return instruction;
		}

		private Instruction ProcessBreakpointOperation(IOperation op)
		{
			var instruction = new BreakpointInstruction(op.Offset);
			return instruction;
		}

		private Instruction ProcessUnaryOperation(IOperation op, UnaryOperation operation)
		{
			var operand = stack.Pop();
			var result = stack.Push();
			var instruction = new UnaryInstruction(op.Offset, result, operation, operand);
			return instruction;
		}

		private Instruction ProcessBinaryOperation(IOperation op, BinaryOperation operation)
		{
			var right = stack.Pop();
			var left = stack.Pop();
			var result = stack.Push();
			var instruction = new BinaryInstruction(op.Offset, result, left, operation, right);
			return instruction;
		}

		private Instruction ProcessConversion(IOperation op)
		{
			var type = op.Value as ITypeReference;
			return ProcessConversion(op, type);
		}

		private Instruction ProcessConversion(IOperation op, ITypeReference type)
		{
			var operand = stack.Pop();
			var result = stack.Push();
			var instruction = new ConvertInstruction(op.Offset, result, type, operand);
			return instruction;
		}

		private Instruction ProcessDup(IOperation op)
		{
			var source = stack.Peek();
			var dest = stack.Push();
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Assign, source);
			return instruction;
		}

		private string GetLocalSourceName(ILocalDefinition local)
		{
			var name = local.Name.Value;

			if (this.sourceLocationProvider != null)
			{
				bool isCompilerGenerated;
				name = this.sourceLocationProvider.GetSourceNameFor(local, out isCompilerGenerated);
			}

			return name;
		}
	}
}
