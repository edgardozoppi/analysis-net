using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;

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

		private IMetadataHost host;
		private IMethodDefinition method;
		private ISourceLocationProvider sourceLocationProvider;
		private LocalVariable thisParameter;
		private IDictionary<IParameterDefinition, LocalVariable> parameters;
		private IDictionary<ILocalDefinition, LocalVariable> locals;
		private OperandStack stack;

		public Disassembler(IMetadataHost host, IMethodDefinition methodDefinition, ISourceLocationProvider sourceLocationProvider)
		{
			this.host = host;
			this.method = methodDefinition;
			this.sourceLocationProvider = sourceLocationProvider;
			this.parameters = new Dictionary<IParameterDefinition, LocalVariable>();
			this.locals = new Dictionary<ILocalDefinition, LocalVariable>();
			this.stack = new OperandStack(method.Body.MaxStack);

			if (!method.IsStatic)
				this.thisParameter = new LocalVariable("this");

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
		}

		public MethodBody Execute()
		{
			var body = new MethodBody(method);

			foreach (var op in method.Body.Operations)
			{
				Instruction instruction = null;

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

					//case OperationCode.Array_Addr:
					//    //elementType = ((IArrayTypeReference)currentOperation.Value).ElementType;
					//    //expression = this.ParseArrayElementAddres(currentOperation, elementType);
					//    break;

					//case OperationCode.Ldelema:
					//    //elementType = (ITypeReference)currentOperation.Value;
					//    //expression = this.ParseArrayElementAddres(currentOperation, elementType, treatArrayAsSingleDimensioned: true);
					//    break;

					//case OperationCode.Array_Create:
					//case OperationCode.Array_Create_WithLowerBound:
					//case OperationCode.Newarr:
					//    //expression = this.ParseArrayCreate(currentOperation);
					//    break;

					//case OperationCode.Array_Get:
					//    //elementType = ((IArrayTypeReference)currentOperation.Value).ElementType;
					//    //expression = this.ParseArrayIndexer(currentOperation, elementType ?? this.platformType.SystemObject, treatArrayAsSingleDimensioned: false);
					//    break;

					//case OperationCode.Ldelem:
					//    elementType = (ITypeReference)currentOperation.Value;
					//    goto case OperationCode.Ldelem_Ref;
					//case OperationCode.Ldelem_I:
					//    elementType = this.platformType.SystemIntPtr;
					//    goto case OperationCode.Ldelem_Ref;
					//case OperationCode.Ldelem_I1:
					//    elementType = this.platformType.SystemInt8;
					//    goto case OperationCode.Ldelem_Ref;
					//case OperationCode.Ldelem_I2:
					//    elementType = this.platformType.SystemInt16;
					//    goto case OperationCode.Ldelem_Ref;
					//case OperationCode.Ldelem_I4:
					//    elementType = this.platformType.SystemInt32;
					//    goto case OperationCode.Ldelem_Ref;
					//case OperationCode.Ldelem_I8:
					//    elementType = this.platformType.SystemInt64;
					//    goto case OperationCode.Ldelem_Ref;
					//case OperationCode.Ldelem_R4:
					//    elementType = this.platformType.SystemFloat32;
					//    goto case OperationCode.Ldelem_Ref;
					//case OperationCode.Ldelem_R8:
					//    elementType = this.platformType.SystemFloat64;
					//    goto case OperationCode.Ldelem_Ref;
					//case OperationCode.Ldelem_U1:
					//    elementType = this.platformType.SystemUInt8;
					//    goto case OperationCode.Ldelem_Ref;
					//case OperationCode.Ldelem_U2:
					//    elementType = this.platformType.SystemUInt16;
					//    goto case OperationCode.Ldelem_Ref;
					//case OperationCode.Ldelem_U4:
					//    elementType = this.platformType.SystemUInt32;
					//    goto case OperationCode.Ldelem_Ref;
					//case OperationCode.Ldelem_Ref:
					//    expression = this.ParseArrayIndexer(currentOperation, elementType ?? this.platformType.SystemObject, treatArrayAsSingleDimensioned: true);
					//    break;

					//case OperationCode.Array_Set:
					//    statement = this.ParseArraySet(currentOperation);
					//    break;

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
						instruction = this.ProcessLeave(op);
						break;

					case OperationCode.Break:
						instruction = this.ProcessEmptyOperation(op, EmptyOperation.Break);
						break;

					case OperationCode.Nop:
						instruction = this.ProcessEmptyOperation(op, EmptyOperation.Nop);
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
						instruction = this.ProcessCall(op);
						break;

					//case OperationCode.Calli:
					//    expression = this.ParsePointerCall(currentOperation);
					//    break;

					case OperationCode.Castclass:
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

					//case OperationCode.Constrained_:
					//    //This prefix is redundant and is not represented in the code model.
					//    break;

					//case OperationCode.Cpblk:
					//    var copyMemory = new CopyMemoryStatement();
					//    copyMemory.NumberOfBytesToCopy = this.PopOperandStack();
					//    copyMemory.SourceAddress = this.PopOperandStack();
					//    copyMemory.TargetAddress = this.PopOperandStack();
					//    statement = copyMemory;
					//    break;

					//case OperationCode.Cpobj:
					//    expression = this.ParseCopyObject();
					//    break;

					case OperationCode.Dup:
						instruction = this.ProcessDup(op);
						break;

					//case OperationCode.Endfilter:
					//    statement = this.ParseEndfilter();
					//    break;

					//case OperationCode.Endfinally:
					//    statement = new EndFinally();
					//    break;

					//case OperationCode.Initblk:
					//    var fillMemory = new FillMemoryStatement();
					//    fillMemory.NumberOfBytesToFill = this.PopOperandStack();
					//    fillMemory.FillValue = this.PopOperandStack();
					//    fillMemory.TargetAddress = this.PopOperandStack();
					//    statement = fillMemory;
					//    break;

					//case OperationCode.Initobj:
					//    statement = this.ParseInitObject(currentOperation);
					//    break;

					//case OperationCode.Isinst:
					//    expression = this.ParseCastIfPossible(currentOperation);
					//    break;

					//case OperationCode.Jmp:
					//    var methodToCall = (IMethodReference)currentOperation.Value;
					//    expression = new MethodCall() { IsJumpCall = true, MethodToCall = methodToCall, Type = methodToCall.Type };
					//    break;

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

					//case OperationCode.Ldfld:
					//case OperationCode.Ldsfld:

					//case OperationCode.Ldflda:
					//case OperationCode.Ldsflda:
					//case OperationCode.Ldftn:
					//case OperationCode.Ldvirtftn:
					//    expression = this.ParseAddressOf(instruction);
					//    break;

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

					//case OperationCode.Ldind_I:
					//case OperationCode.Ldind_I1:
					//case OperationCode.Ldind_I2:
					//case OperationCode.Ldind_I4:
					//case OperationCode.Ldind_I8:
					//case OperationCode.Ldind_R4:
					//case OperationCode.Ldind_R8:
					//case OperationCode.Ldind_Ref:
					//case OperationCode.Ldind_U1:
					//case OperationCode.Ldind_U2:
					//case OperationCode.Ldind_U4:
					//case OperationCode.Ldobj:
					//    expression = this.ParseAddressDereference(currentOperation);
					//    break;

					//case OperationCode.Ldlen:
					//    expression = this.ParseVectorLength();
					//    break;

					//case OperationCode.Ldtoken:
					//    expression = ParseToken(currentOperation);
					//    break;

					//case OperationCode.Localloc:
					//    expression = this.ParseStackArrayCreate();
					//    break;

					//case OperationCode.Mkrefany:
					//    expression = this.ParseMakeTypedReference(currentOperation);
					//    break;

					case OperationCode.Neg:
						instruction = this.ProcessUnaryOperation(op, UnaryOperation.Neg);
						break;

					case OperationCode.Not:
						instruction = this.ProcessUnaryOperation(op, UnaryOperation.Not);
						break;

					//case OperationCode.Newobj:
					//    expression = this.ParseCreateObjectInstance(currentOperation);
					//    break;

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

					//case OperationCode.Rethrow:
					//    statement = new RethrowStatement();
					//    break;

					case OperationCode.Sizeof:
						instruction = this.ProcessSizeOf(op);
						break;

					case OperationCode.Starg:
					case OperationCode.Starg_S:
						instruction = this.ProcessStoreArgument(op);
					    break;

					//case OperationCode.Stelem:
					//case OperationCode.Stelem_I:
					//case OperationCode.Stelem_I1:
					//case OperationCode.Stelem_I2:
					//case OperationCode.Stelem_I4:
					//case OperationCode.Stelem_I8:
					//case OperationCode.Stelem_R4:
					//case OperationCode.Stelem_R8:
					//case OperationCode.Stelem_Ref:
					//case OperationCode.Stfld:
					//case OperationCode.Stind_I:
					//case OperationCode.Stind_I1:
					//case OperationCode.Stind_I2:
					//case OperationCode.Stind_I4:
					//case OperationCode.Stind_I8:
					//case OperationCode.Stind_R4:
					//case OperationCode.Stind_R8:
					//case OperationCode.Stind_Ref:

					case OperationCode.Stloc:
					case OperationCode.Stloc_0:
					case OperationCode.Stloc_1:
					case OperationCode.Stloc_2:
					case OperationCode.Stloc_3:
					case OperationCode.Stloc_S:
					    instruction = this.ProcessStoreLocal(op);
					    break;

					//case OperationCode.Stobj:
					//case OperationCode.Stsfld:

					//case OperationCode.Switch:
					//    statement = this.ParseSwitchInstruction(currentOperation);
					//    break;

					//case OperationCode.Tail_:
					//    this.sawTailCall = true;
					//    break;

					//case OperationCode.Throw:
					//    statement = this.ParseThrow();
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
				}

				body.Instructions.Add(instruction);
			}

			return body;
		}

		private Instruction ProcessCall(IOperation op)
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
				var arg = stack.Pop();
				arguments.Add(arg);
			}

			arguments.Reverse();

			if (callee.Type.TypeCode != PrimitiveTypeCode.Void)
				result = stack.Push();

			var instruction = new CallInstruction(op.Offset, result, callee, arguments);
			return instruction;
		}

		private Instruction ProcessSizeOf(IOperation op)
		{
			var type = op.Value as ITypeReference;
			var result = stack.Push();
			var instruction = new SizeOfInstruction(op.Offset, result, type);
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

		private Instruction ProcessLeave(IOperation op)
		{
			stack.Clear();
			var target = (uint)op.Value;
			var instruction = new UnconditionalBranchInstruction(op.Offset, target);
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

		private Instruction ProcessEmptyOperation(IOperation op, EmptyOperation operation)
		{
			var instruction = new EmptyInstruction(op.Offset, operation);
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
