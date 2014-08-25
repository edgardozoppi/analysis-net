using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Backend.ThreeAddressCode;

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

			public TemporalVariable Top()
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
		private ContextKind contextKind;
		private TryInformation tryInfo;
		private MethodBody body;

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
			tryInfo = null;
			contextKind = ContextKind.None;
			body = new MethodBody(method);

			this.FillBodyVariables();

			foreach (var op in method.Body.Operations)
			{
				this.ProcessExceptionHandling(op.Offset);

				switch (op.OperationCode)
				{
					case OperationCode.Add:
					case OperationCode.Add_Ovf:
					case OperationCode.Add_Ovf_Un:
						this.ProcessBinaryOperation(op, BinaryOperation.Add);
						break;

					case OperationCode.And:
						this.ProcessBinaryOperation(op, BinaryOperation.And);
						break;

					case OperationCode.Ceq:
						this.ProcessBinaryOperation(op, BinaryOperation.Eq);
						break;

					case OperationCode.Cgt:
					case OperationCode.Cgt_Un:
						this.ProcessBinaryOperation(op, BinaryOperation.Gt);
						break;

					case OperationCode.Clt:
					case OperationCode.Clt_Un:
						this.ProcessBinaryOperation(op, BinaryOperation.Lt);
						break;

					case OperationCode.Div:
					case OperationCode.Div_Un:
						this.ProcessBinaryOperation(op, BinaryOperation.Div);
						break;

					case OperationCode.Mul:
					case OperationCode.Mul_Ovf:
					case OperationCode.Mul_Ovf_Un:
						this.ProcessBinaryOperation(op, BinaryOperation.Mul);
						break;

					case OperationCode.Or:
						this.ProcessBinaryOperation(op, BinaryOperation.Or);
						break;

					case OperationCode.Rem:
					case OperationCode.Rem_Un:
						this.ProcessBinaryOperation(op, BinaryOperation.Rem);
						break;

					case OperationCode.Shl:
						this.ProcessBinaryOperation(op, BinaryOperation.Shl);
						break;

					case OperationCode.Shr:
					case OperationCode.Shr_Un:
						this.ProcessBinaryOperation(op, BinaryOperation.Shr);
						break;

					case OperationCode.Sub:
					case OperationCode.Sub_Ovf:
					case OperationCode.Sub_Ovf_Un:
						this.ProcessBinaryOperation(op, BinaryOperation.Sub);
						break;

					case OperationCode.Xor:
						this.ProcessBinaryOperation(op, BinaryOperation.Xor);
						break;

					//case OperationCode.Arglist:
					//    //expression = new RuntimeArgumentHandleExpression();
					//    break;

					case OperationCode.Array_Create_WithLowerBound:
						this.ProcessCreateArray(op, true);
						break;

					case OperationCode.Array_Create:
					case OperationCode.Newarr:
						this.ProcessCreateArray(op, false);
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
						this.ProcessLoadArrayElement(op);
						break;

					case OperationCode.Array_Addr:
					case OperationCode.Ldelema:
						this.ProcessLoadArrayElementAddress(op);
						break;

					case OperationCode.Beq:
					case OperationCode.Beq_S:
						this.ProcessBinaryConditionalBranch(op, BinaryOperation.Eq);
						break;

					case OperationCode.Bne_Un:
					case OperationCode.Bne_Un_S:
						this.ProcessBinaryConditionalBranch(op, BinaryOperation.Neq);
						break;

					case OperationCode.Bge:
					case OperationCode.Bge_S:
					case OperationCode.Bge_Un:
					case OperationCode.Bge_Un_S:
						this.ProcessBinaryConditionalBranch(op, BinaryOperation.Ge);
						break;

					case OperationCode.Bgt:
					case OperationCode.Bgt_S:
					case OperationCode.Bgt_Un:
					case OperationCode.Bgt_Un_S:
						this.ProcessBinaryConditionalBranch(op, BinaryOperation.Gt);
						break;

					case OperationCode.Ble:
					case OperationCode.Ble_S:
					case OperationCode.Ble_Un:
					case OperationCode.Ble_Un_S:
						this.ProcessBinaryConditionalBranch(op, BinaryOperation.Le);
						break;

					case OperationCode.Blt:
					case OperationCode.Blt_S:
					case OperationCode.Blt_Un:
					case OperationCode.Blt_Un_S:
						this.ProcessBinaryConditionalBranch(op, BinaryOperation.Lt);
						break;

					case OperationCode.Br:
					case OperationCode.Br_S:
						this.ProcessUnconditionalBranch(op);
						break;

					case OperationCode.Leave:
					case OperationCode.Leave_S:
						this.ProcessLeave(op);
						break;

					case OperationCode.Break:
						this.ProcessBreakpointOperation(op);
						break;

					case OperationCode.Nop:
						this.ProcessEmptyOperation(op);
						break;

					case OperationCode.Brfalse:
					case OperationCode.Brfalse_S:
						this.ProcessUnaryConditionalBranch(op, false);
						break;

					case OperationCode.Brtrue:
					case OperationCode.Brtrue_S:
						this.ProcessUnaryConditionalBranch(op, true);
						break;

					case OperationCode.Call:
					case OperationCode.Callvirt:
						this.ProcessMethodCall(op);
						break;

					case OperationCode.Jmp:
						this.ProcessJumpCall(op);
						break;

					case OperationCode.Calli:
						this.ProcessMethodCallIndirect(op);
						break;

					case OperationCode.Castclass:
					case OperationCode.Isinst:
					case OperationCode.Box:
					case OperationCode.Unbox:
					case OperationCode.Unbox_Any:
						this.ProcessConversion(op);
						break;

					case OperationCode.Conv_I:
					case OperationCode.Conv_Ovf_I:
					case OperationCode.Conv_Ovf_I_Un:
						this.ProcessConversion(op, host.PlatformType.SystemIntPtr);
						break;

					case OperationCode.Conv_I1:
					case OperationCode.Conv_Ovf_I1:
					case OperationCode.Conv_Ovf_I1_Un:
						this.ProcessConversion(op, host.PlatformType.SystemInt8);
						break;

					case OperationCode.Conv_I2:
					case OperationCode.Conv_Ovf_I2:
					case OperationCode.Conv_Ovf_I2_Un:
						this.ProcessConversion(op, host.PlatformType.SystemInt16);
						break;

					case OperationCode.Conv_I4:
					case OperationCode.Conv_Ovf_I4:
					case OperationCode.Conv_Ovf_I4_Un:
						this.ProcessConversion(op, host.PlatformType.SystemInt32);
						break;

					case OperationCode.Conv_I8:
					case OperationCode.Conv_Ovf_I8:
					case OperationCode.Conv_Ovf_I8_Un:
						this.ProcessConversion(op, host.PlatformType.SystemInt64);
						break;

					case OperationCode.Conv_U:
					case OperationCode.Conv_Ovf_U:
					case OperationCode.Conv_Ovf_U_Un:
						this.ProcessConversion(op, host.PlatformType.SystemUIntPtr);
						break;

					case OperationCode.Conv_U1:
					case OperationCode.Conv_Ovf_U1:
					case OperationCode.Conv_Ovf_U1_Un:
						this.ProcessConversion(op, host.PlatformType.SystemUInt8);
						break;

					case OperationCode.Conv_U2:
					case OperationCode.Conv_Ovf_U2:
					case OperationCode.Conv_Ovf_U2_Un:
						this.ProcessConversion(op, host.PlatformType.SystemUInt16);
						break;

					case OperationCode.Conv_U4:
					case OperationCode.Conv_Ovf_U4:
					case OperationCode.Conv_Ovf_U4_Un:
						this.ProcessConversion(op, host.PlatformType.SystemUInt32);
						break;

					case OperationCode.Conv_U8:
					case OperationCode.Conv_Ovf_U8:
					case OperationCode.Conv_Ovf_U8_Un:
						this.ProcessConversion(op, host.PlatformType.SystemUInt64);
						break;

					case OperationCode.Conv_R4:
						this.ProcessConversion(op, host.PlatformType.SystemFloat32);
						break;

					case OperationCode.Conv_R8:
					case OperationCode.Conv_R_Un:
						this.ProcessConversion(op, host.PlatformType.SystemFloat64);
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
						break;

					case OperationCode.Cpblk:
						this.ProcessCopyMemory(op);
						break;

					case OperationCode.Cpobj:
						this.ProcessCopyObject(op);
						break;

					case OperationCode.Dup:
						this.ProcessDup(op);
						break;

					//case OperationCode.Endfilter:
					//    statement = this.ParseEndfilter();
					//    break;

					case OperationCode.Endfinally:
						this.ProcessEndFinally(op);
						break;

					case OperationCode.Initblk:
						this.ProcessInitializeMemory(op);
						break;

					case OperationCode.Initobj:
						this.ProcessInitializeObject(op);
						break;

					case OperationCode.Ldarg:
					case OperationCode.Ldarg_0:
					case OperationCode.Ldarg_1:
					case OperationCode.Ldarg_2:
					case OperationCode.Ldarg_3:
					case OperationCode.Ldarg_S:
						this.ProcessLoadArgument(op);
					    break;

					case OperationCode.Ldarga:
					case OperationCode.Ldarga_S:
						this.ProcessLoadArgumentAddress(op);
						break;

					case OperationCode.Ldloc:
					case OperationCode.Ldloc_0:
					case OperationCode.Ldloc_1:
					case OperationCode.Ldloc_2:
					case OperationCode.Ldloc_3:
					case OperationCode.Ldloc_S:
					    this.ProcessLoadLocal(op);
					    break;

					case OperationCode.Ldloca:
					case OperationCode.Ldloca_S:
						this.ProcessLoadLocalAddress(op);
					    break;

					case OperationCode.Ldfld:
						this.ProcessLoadInstanceField(op);
						break;

					case OperationCode.Ldsfld:
						this.ProcessLoadStaticField(op);
						break;

					case OperationCode.Ldflda:
						this.ProcessLoadInstanceFieldAddress(op);
						break;

					case OperationCode.Ldsflda:
						this.ProcessLoadStaticFieldAddress(op);
						break;

					case OperationCode.Ldftn:
						this.ProcessLoadMethodAddress(op);
						break;

					case OperationCode.Ldvirtftn:
						this.ProcessLoadVirtualMethodAddress(op);
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
						this.ProcessLoadConstant(op);
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
						this.ProcessLoadIndirect(op);
						break;

					case OperationCode.Ldlen:
						this.ProcessLoadArrayLength(op);
						break;

					//case OperationCode.Ldtoken:
					//    expression = ParseToken(currentOperation);
					//    break;

					case OperationCode.Localloc:
						this.ProcessLocalAllocation(op);
						break;

					//case OperationCode.Mkrefany:
					//    expression = this.ParseMakeTypedReference(currentOperation);
					//    break;

					case OperationCode.Neg:
						this.ProcessUnaryOperation(op, UnaryOperation.Neg);
						break;

					case OperationCode.Not:
						this.ProcessUnaryOperation(op, UnaryOperation.Not);
						break;

					case OperationCode.Newobj:
						this.ProcessCreateObject(op);
						break;

					case OperationCode.No_:
						//if code out there actually uses this, I need to know sooner rather than later.
						//TODO: need object model support
						throw new NotImplementedException("Invalid opcode: No.");

					case OperationCode.Pop:
						stack.Pop();
						break;

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
						this.ProcessReturn(op);
						break;

					case OperationCode.Sizeof:
						this.ProcessSizeof(op);
						break;

					case OperationCode.Starg:
					case OperationCode.Starg_S:
						this.ProcessStoreArgument(op);
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
						this.ProcessStoreArrayElement(op);
						break;

					case OperationCode.Stfld:
						this.ProcessStoreInstanceField(op);
						break;

					case OperationCode.Stsfld:
						this.ProcessStoreStaticField(op);
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
						this.ProcessStoreIndirect(op);
					    break;

					case OperationCode.Stloc:
					case OperationCode.Stloc_0:
					case OperationCode.Stloc_1:
					case OperationCode.Stloc_2:
					case OperationCode.Stloc_3:
					case OperationCode.Stloc_S:
					    this.ProcessStoreLocal(op);
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
			}

			return body;
		}

		private void FillBodyVariables()
		{
			if (thisParameter != null)
			{
				body.Variables.Add(thisParameter);
			}

			body.Variables.UnionWith(parameters.Values);
			body.Variables.UnionWith(locals.Values);
			body.Variables.UnionWith(stack.Variables);
		}

		private void ProcessExceptionHandling(uint offset)
		{
			if (trys.ContainsKey(offset))
			{
				contextKind = ContextKind.Try;
				tryInfo = trys[offset];					

				var instruction = new TryInstruction(offset);
				body.Instructions.Add(instruction);
			}

			if (tryInfo != null)
			{
				if (tryInfo.ExceptionHandlers.ContainsKey(offset))
				{
					contextKind = ContextKind.Catch;
					var catchInfo = tryInfo.ExceptionHandlers[offset];						
					// push the exception into the stack
					var ex = stack.Push();

					var instruction = new CatchInstruction(offset, ex, catchInfo.ExceptionType);
					body.Instructions.Add(instruction);
				}

				if (tryInfo.Finally != null && tryInfo.Finally.BeginOffset == offset)
				{
					contextKind = ContextKind.Finally;
					var instruction = new FinallyInstruction(offset);
					body.Instructions.Add(instruction);
				}
			}
		}

		private void ProcessLocalAllocation(IOperation op)
		{
			var numberOfBytes = stack.Pop();
			var targetAddress = stack.Push();

			var instruction = new LocalAllocationInstruction(op.Offset, targetAddress, numberOfBytes);
			body.Instructions.Add(instruction);
		}

		private void ProcessInitializeMemory(IOperation op)
		{
			var numberOfBytes = stack.Pop();
			var fillValue = stack.Pop();
			var targetAddress = stack.Pop();

			var instruction = new InitializeMemoryInstruction(op.Offset, targetAddress, fillValue, numberOfBytes);
			body.Instructions.Add(instruction);
		}

		private void ProcessInitializeObject(IOperation op)
		{
			var targetAddress = stack.Pop();
			var instruction = new InitializeObjectInstruction(op.Offset, targetAddress);
			body.Instructions.Add(instruction);
		}

		private void ProcessCreateArray(IOperation op, bool withLowerBounds)
		{
			var arrayType = op.Value as IArrayTypeReference;
			var elementType = arrayType.ElementType;
			var rank = arrayType.Rank;
			var lowerBounds = new List<Variable>();
			var sizes = new List<Variable>();

			if (withLowerBounds)
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
			var instruction = new CreateArrayInstruction(op.Offset, result, elementType, rank, lowerBounds, sizes);
			body.Instructions.Add(instruction);
		}

		private void ProcessCopyObject(IOperation op)
		{
			var sourceAddress = stack.Pop();
			var targetAddress = stack.Pop();

			var instruction = new CopyObjectInstruction(op.Offset, targetAddress, sourceAddress);
			body.Instructions.Add(instruction);
		}

		private void ProcessCopyMemory(IOperation op)
		{
			var numberOfBytes = stack.Pop();
			var sourceAddress = stack.Pop();
			var targetAddress = stack.Pop();

			var instruction = new CopyMemoryInstruction(op.Offset, targetAddress, sourceAddress, numberOfBytes);
			body.Instructions.Add(instruction);
		}

		private void ProcessCreateObject(IOperation op)
		{
			var callee = op.Value as IMethodReference;
			var arguments = new List<Variable>();

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
			body.Instructions.Add(instruction);
		}

		private void ProcessMethodCall(IOperation op)
		{
			var callee = op.Value as IMethodReference;
			var arguments = new List<Variable>();
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
			body.Instructions.Add(instruction);
		}

		private void ProcessMethodCallIndirect(IOperation op)
		{
			var calleeType = op.Value as IFunctionPointerTypeReference;
			var calleePointer = stack.Pop();
			var arguments = new List<Variable>();
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
			body.Instructions.Add(instruction);
		}

		private void ProcessJumpCall(IOperation op)
		{
			var callee = op.Value as IMethodReference;
			var arguments = new List<Variable>();
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
			body.Instructions.Add(instruction);
		}

		private void ProcessSizeof(IOperation op)
		{
			var type = op.Value as ITypeReference;
			var result = stack.Push();
			var instruction = new SizeofInstruction(op.Offset, result, type);
			body.Instructions.Add(instruction);
		}

		private void ProcessUnaryConditionalBranch(IOperation op, bool value)
		{
			var source = new Constant(value);
			var dest = stack.Push();
			var load = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(load);

			var right = stack.Pop();
			var left = stack.Pop();
			dest = stack.Push();
			var compare = new BinaryInstruction(op.Offset, dest, left, BinaryOperation.Eq, right);
			body.Instructions.Add(compare);

			var operand = stack.Pop();
			var target = (uint)op.Value;
			var instruction = new ConditionalBranchInstruction(op.Offset, operand, target);
			body.Instructions.Add(instruction);
		}

		private void ProcessBinaryConditionalBranch(IOperation op, BinaryOperation condition)
		{
			var right = stack.Pop();
			var left = stack.Pop();
			var dest = stack.Push();
			var compare = new BinaryInstruction(op.Offset, dest, left, condition, right);
			body.Instructions.Add(compare);

			var operand = stack.Pop();
			var target = (uint)op.Value;
			var instruction = new ConditionalBranchInstruction(op.Offset, operand, target);
			body.Instructions.Add(instruction);
		}

		private void ProcessEndFinally(IOperation op)
		{
			var target = string.Format("L_{0:X4}", tryInfo.Finally.EndOffset);
			contextKind = ContextKind.None;
			stack.Clear();

			var instruction = new UnconditionalBranchInstruction(op.Offset, 0);
			instruction.Target = target;
			body.Instructions.Add(instruction);
		}

		private void ProcessLeave(IOperation op)
		{
			BranchInstruction instruction;
			var target = string.Format("L_{0:X4}", op.Value);

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
			stack.Clear();

			instruction = new UnconditionalBranchInstruction(op.Offset, 0);
			instruction.Target = target;
			body.Instructions.Add(instruction);
		}

		private void ProcessUnconditionalBranch(IOperation op)
		{
			var target = (uint)op.Value;
			var instruction = new UnconditionalBranchInstruction(op.Offset, target);
			body.Instructions.Add(instruction);
		}

		private void ProcessReturn(IOperation op)
		{
			Variable operand = null;

			if (method.Type.TypeCode != PrimitiveTypeCode.Void)
				operand = stack.Pop();

			var instruction = new ReturnInstruction(op.Offset, operand);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadConstant(IOperation op)
		{
			var source = new Constant(op.Value);
			var dest = stack.Push();
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadArgument(IOperation op)
		{
			var source = thisParameter;

			if (op.Value is IParameterDefinition)
			{
				var argument = op.Value as IParameterDefinition;
				source = parameters[argument];
			}

			var dest = stack.Push();
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadArgumentAddress(IOperation op)
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
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadLocal(IOperation op)
		{
			var local = op.Value as ILocalDefinition;
			var source = locals[local];
			var dest = stack.Push();
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadLocalAddress(IOperation op)
		{
			var local = op.Value as ILocalDefinition;
			var operand = locals[local];
			var dest = stack.Push();
			var source = new Reference(operand);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadIndirect(IOperation op)
		{
			var address = stack.Pop();
			var dest = stack.Push();
			var source = new Dereference(address);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadInstanceField(IOperation op)
		{
			var field = op.Value as IFieldDefinition;
			var obj = stack.Pop();
			var dest = stack.Push();
			var source = new InstanceFieldAccess(obj, field.Name.Value);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadStaticField(IOperation op)
		{
			var field = op.Value as IFieldDefinition;
			var dest = stack.Push();
			var source = new StaticFieldAccess(field.ContainingType, field.Name.Value);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadInstanceFieldAddress(IOperation op)
		{
			var field = op.Value as IFieldDefinition;
			var obj = stack.Pop();
			var dest = stack.Push();
			var access = new InstanceFieldAccess(obj, field.Name.Value);
			var source = new Reference(access);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadStaticFieldAddress(IOperation op)
		{
			var field = op.Value as IFieldDefinition;
			var dest = stack.Push();
			var access = new StaticFieldAccess(field.ContainingType, field.Name.Value);
			var source = new Reference(access);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadArrayLength(IOperation op)
		{
			var array = stack.Pop();
			var dest = stack.Push();
			var length = new InstanceFieldAccess(array, "Length");
			var instruction = new LoadInstruction(op.Offset, dest, length);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadArrayElement(IOperation op)
		{
			var index = stack.Pop();
			var array = stack.Pop();			
			var dest = stack.Push();
			var source = new ArrayElementAccess(array, index);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadArrayElementAddress(IOperation op)
		{
			var index = stack.Pop();
			var array = stack.Pop();
			var dest = stack.Push();
			var access = new ArrayElementAccess(array, index);
			var source = new Reference(access);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadMethodAddress(IOperation op)
		{
			var method = op.Value as IMethodReference;
			var dest = stack.Push();
			var signature = new StaticMethod(method);
			var source = new Reference(signature);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessLoadVirtualMethodAddress(IOperation op)
		{
			var method = op.Value as IMethodReference;
			var obj = stack.Pop();
			var dest = stack.Push();
			var signature = new VirtualMethod(obj, method);
			var source = new Reference(signature);
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessStoreArgument(IOperation op)
		{
			var dest = thisParameter;

			if (op.Value is IParameterDefinition)
			{
				var argument = op.Value as IParameterDefinition;
				dest = parameters[argument];
			}
			
			var source = stack.Pop();
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessStoreLocal(IOperation op)
		{
			var local = op.Value as ILocalDefinition;
			var dest = locals[local];
			var source = stack.Pop();
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessStoreIndirect(IOperation op)
		{
			var source = stack.Pop();
			var address = stack.Pop();
			var dest = new Dereference(address);
			var instruction = new StoreInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessStoreInstanceField(IOperation op)
		{
			var field = op.Value as IFieldDefinition;
			var source = stack.Pop();
			var obj = stack.Pop();
			var dest = new InstanceFieldAccess(obj, field.Name.Value);
			var instruction = new StoreInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessStoreStaticField(IOperation op)
		{
			var field = op.Value as IFieldDefinition;
			var source = stack.Pop();
			var dest = new StaticFieldAccess(field.ContainingType, field.Name.Value);
			var instruction = new StoreInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessStoreArrayElement(IOperation op)
		{
			var source = stack.Pop();
			var index = stack.Pop();
			var array = stack.Pop();
			var dest = new ArrayElementAccess(array, index);
			var instruction = new StoreInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
		}

		private void ProcessEmptyOperation(IOperation op)
		{
			var instruction = new NopInstruction(op.Offset);
			body.Instructions.Add(instruction);
		}

		private void ProcessBreakpointOperation(IOperation op)
		{
			var instruction = new BreakpointInstruction(op.Offset);
			body.Instructions.Add(instruction);
		}

		private void ProcessUnaryOperation(IOperation op, UnaryOperation operation)
		{
			var operand = stack.Pop();
			var dest = stack.Push();
			var instruction = new UnaryInstruction(op.Offset, dest, operation, operand);
			body.Instructions.Add(instruction);
		}

		private void ProcessBinaryOperation(IOperation op, BinaryOperation operation)
		{
			var right = stack.Pop();
			var left = stack.Pop();
			var dest = stack.Push();
			var instruction = new BinaryInstruction(op.Offset, dest, left, operation, right);
			body.Instructions.Add(instruction);
		}

		private void ProcessConversion(IOperation op)
		{
			var type = op.Value as ITypeReference;
			this.ProcessConversion(op, type);
		}

		private void ProcessConversion(IOperation op, ITypeReference type)
		{
			var operand = stack.Pop();
			var result = stack.Push();
			var instruction = new ConvertInstruction(op.Offset, result, type, operand);
			body.Instructions.Add(instruction);
		}

		private void ProcessDup(IOperation op)
		{
			var source = stack.Top();
			var dest = stack.Push();
			var instruction = new LoadInstruction(op.Offset, dest, source);
			body.Instructions.Add(instruction);
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
