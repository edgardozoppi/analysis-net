using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;

namespace Backend
{
	public class Disassembler
	{
		private IMethodDefinition methodDef;
		private LocalVariable thisParameter;
		private IDictionary<IParameterDefinition, LocalVariable> parametersDef;
		private IDictionary<ILocalDefinition, LocalVariable> localsDef;
		private Stack<Operand> stack;
		private uint tempCount;

		public Disassembler(IMethodDefinition methodDefinition)
		{
			methodDef = methodDefinition;
			parametersDef = new Dictionary<IParameterDefinition, LocalVariable>();
			localsDef = new Dictionary<ILocalDefinition, LocalVariable>();
			stack = new Stack<Operand>(methodDef.Body.MaxStack);

			if (!methodDef.IsStatic)
				thisParameter = new LocalVariable("this");

			foreach (var parameter in methodDef.Parameters)
			{
				var p = new LocalVariable(parameter.Name.Value);
				parametersDef.Add(parameter, p);
			}

			foreach (var local in methodDef.Body.LocalVariables)
			{
				var l = new LocalVariable(local.Name.Value);
				localsDef.Add(local, l);
			}
		}

		public MethodBody Execute()
		{
			var body = new MethodBody(methodDef);
			stack.Clear();
			tempCount = 1;

			foreach (var op in methodDef.Body.Operations)
			{
				Instruction instruction = null;
				BinaryOperation binaryOperation;
				EmptyOperation emptyOperation;
				UnaryOperation unaryOperation;

				switch (op.OperationCode)
				{
					case OperationCode.Add:
					case OperationCode.Add_Ovf:
					case OperationCode.Add_Ovf_Un:
						binaryOperation = BinaryOperation.Add;
						goto binaryOperation;

					case OperationCode.And:
						binaryOperation = BinaryOperation.And;
						goto binaryOperation;

					case OperationCode.Ceq:
						binaryOperation = BinaryOperation.Eq;
						goto binaryOperation;

					case OperationCode.Cgt:
					case OperationCode.Cgt_Un:
						binaryOperation = BinaryOperation.Gt;
						goto binaryOperation;

					case OperationCode.Clt:
					case OperationCode.Clt_Un:
						binaryOperation = BinaryOperation.Lt;
						goto binaryOperation;

					case OperationCode.Div:
					case OperationCode.Div_Un:
						binaryOperation = BinaryOperation.Div;
						goto binaryOperation;

					case OperationCode.Mul:
					case OperationCode.Mul_Ovf:
					case OperationCode.Mul_Ovf_Un:
						binaryOperation = BinaryOperation.Mul;
						goto binaryOperation;

					case OperationCode.Or:
						binaryOperation = BinaryOperation.Or;
						goto binaryOperation;

					case OperationCode.Rem:
					case OperationCode.Rem_Un:
						binaryOperation = BinaryOperation.Rem;
						goto binaryOperation;

					case OperationCode.Shl:
						binaryOperation = BinaryOperation.Shl;
						goto binaryOperation;

					case OperationCode.Shr:
					case OperationCode.Shr_Un:
						binaryOperation = BinaryOperation.Shr;
						goto binaryOperation;

					case OperationCode.Sub:
					case OperationCode.Sub_Ovf:
					case OperationCode.Sub_Ovf_Un:
						binaryOperation = BinaryOperation.Sub;
						goto binaryOperation;

					case OperationCode.Xor:
						binaryOperation = BinaryOperation.Xor;
						goto binaryOperation;

				binaryOperation:
						instruction = this.ProcessBinaryOperation(op, binaryOperation);
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

					//case OperationCode.Beq:
					//case OperationCode.Beq_S:
					//case OperationCode.Bge:
					//case OperationCode.Bge_S:
					//case OperationCode.Bge_Un:
					//case OperationCode.Bge_Un_S:
					//case OperationCode.Bgt:
					//case OperationCode.Bgt_S:
					//case OperationCode.Bgt_Un:
					//case OperationCode.Bgt_Un_S:
					//case OperationCode.Ble:
					//case OperationCode.Ble_S:
					//case OperationCode.Ble_Un:
					//case OperationCode.Ble_Un_S:
					//case OperationCode.Blt:
					//case OperationCode.Blt_S:
					//case OperationCode.Blt_Un:
					//case OperationCode.Blt_Un_S:
					//case OperationCode.Bne_Un:
					//case OperationCode.Bne_Un_S:
					//    statement = this.ParseBinaryConditionalBranch(currentOperation);
					//    break;

					//case OperationCode.Box:
					//    expression = this.ParseConversion(currentOperation);
					//    break;

					//case OperationCode.Br:
					//case OperationCode.Br_S:
					//case OperationCode.Leave:
					//case OperationCode.Leave_S:
					//    statement = this.ParseUnconditionalBranch(currentOperation);
					//    break;

					case OperationCode.Break:
						emptyOperation = EmptyOperation.Break;
						goto emptyOperation;

					case OperationCode.Nop:
						emptyOperation = EmptyOperation.Nop;
						goto emptyOperation;

				emptyOperation:
						instruction = this.ProcessEmptyOperation(op, emptyOperation);
						break;

					//case OperationCode.Brfalse:
					//case OperationCode.Brfalse_S:
					//case OperationCode.Brtrue:
					//case OperationCode.Brtrue_S:
					//    statement = this.ParseUnaryConditionalBranch(currentOperation);
					//    break;

					//case OperationCode.Call:
					//case OperationCode.Callvirt:
					//    MethodCall call = this.ParseCall(currentOperation);
					//    if (call.MethodToCall.Type.TypeCode == PrimitiveTypeCode.Void)
					//    {
					//        call.Locations.Add(currentOperation.Location); // turning it into a statement prevents the location from being attached to the expresssion
					//        ExpressionStatement es = new ExpressionStatement();
					//        es.Expression = call;
					//        statement = es;
					//    }
					//    else
					//        expression = call;
					//    break;

					//case OperationCode.Calli:
					//    expression = this.ParsePointerCall(currentOperation);
					//    break;

					//case OperationCode.Castclass:
					//case OperationCode.Conv_I:
					//case OperationCode.Conv_I1:
					//case OperationCode.Conv_I2:
					//case OperationCode.Conv_I4:
					//case OperationCode.Conv_I8:
					//case OperationCode.Conv_Ovf_I:
					//case OperationCode.Conv_Ovf_I_Un:
					//case OperationCode.Conv_Ovf_I1:
					//case OperationCode.Conv_Ovf_I1_Un:
					//case OperationCode.Conv_Ovf_I2:
					//case OperationCode.Conv_Ovf_I2_Un:
					//case OperationCode.Conv_Ovf_I4:
					//case OperationCode.Conv_Ovf_I4_Un:
					//case OperationCode.Conv_Ovf_I8:
					//case OperationCode.Conv_Ovf_I8_Un:
					//case OperationCode.Conv_Ovf_U:
					//case OperationCode.Conv_Ovf_U_Un:
					//case OperationCode.Conv_Ovf_U1:
					//case OperationCode.Conv_Ovf_U1_Un:
					//case OperationCode.Conv_Ovf_U2:
					//case OperationCode.Conv_Ovf_U2_Un:
					//case OperationCode.Conv_Ovf_U4:
					//case OperationCode.Conv_Ovf_U4_Un:
					//case OperationCode.Conv_Ovf_U8:
					//case OperationCode.Conv_Ovf_U8_Un:
					//case OperationCode.Conv_R_Un:
					//case OperationCode.Conv_R4:
					//case OperationCode.Conv_R8:
					//case OperationCode.Conv_U:
					//case OperationCode.Conv_U1:
					//case OperationCode.Conv_U2:
					//case OperationCode.Conv_U4:
					//case OperationCode.Conv_U8:
					//case OperationCode.Unbox:
					//case OperationCode.Unbox_Any:
					//    expression = this.ParseConversion(currentOperation);
					//    break;

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

					//case OperationCode.Dup:
					//    expression = this.ParseDup(instruction.Type);
					//    break;

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
					case OperationCode.Ldloc:
					case OperationCode.Ldloc_0:
					case OperationCode.Ldloc_1:
					case OperationCode.Ldloc_2:
					case OperationCode.Ldloc_3:
					case OperationCode.Ldloc_S:
					//case OperationCode.Ldfld:
					//case OperationCode.Ldsfld:
					    instruction = this.ProcessVariable(op);
					    break;

					//case OperationCode.Ldarga:
					//case OperationCode.Ldarga_S:
					//case OperationCode.Ldflda:
					//case OperationCode.Ldsflda:
					//case OperationCode.Ldloca:
					//case OperationCode.Ldloca_S:
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
						instruction = this.ProcessConstant(op);
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
						unaryOperation = UnaryOperation.Neg;
						goto unaryOperation;

					case OperationCode.Not:
						unaryOperation = UnaryOperation.Not;
						goto unaryOperation;

				unaryOperation:
						instruction = this.ProcessUnaryOperation(op, unaryOperation);
						break;

					//case OperationCode.Newobj:
					//    expression = this.ParseCreateObjectInstance(currentOperation);
					//    break;

					//case OperationCode.No_:
					//    Contract.Assume(false); //if code out there actually uses this, I need to know sooner rather than later.
					//    //TODO: need object model support
					//    break;

					//case OperationCode.Pop:
					//    statement = this.ParsePop();
					//    break;

					//case OperationCode.Readonly_:
					//    this.sawReadonly = true;
					//    break;

					//case OperationCode.Refanytype:
					//    expression = this.ParseGetTypeOfTypedReference();
					//    break;

					//case OperationCode.Refanyval:
					//    expression = this.ParseGetValueOfTypedReference(currentOperation);
					//    break;

					//case OperationCode.Ret:
					//    statement = this.ParseReturn();
					//    break;

					//case OperationCode.Rethrow:
					//    statement = new RethrowStatement();
					//    break;

					//case OperationCode.Sizeof:
					//    expression = ParseSizeOf(currentOperation);
					//    break;

					case OperationCode.Starg:
					case OperationCode.Starg_S:
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
					//case OperationCode.Stobj:
					//case OperationCode.Stsfld:
					    instruction = this.ProcessAssignment(op);
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

		private Instruction ProcessConstant(IOperation op)
		{
			var source = new Constant(op.Value);
			var dest = new TemporalVariable(tempCount++);
			var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Copy, source);

			stack.Push(dest);
			return instruction;
		}

		private Instruction ProcessUnaryOperation(IOperation op, UnaryOperation operation)
		{
			var operand = stack.Pop();
			var dest = new TemporalVariable(tempCount++);
			var instruction = new UnaryInstruction(op.Offset, dest, operation, operand);

			stack.Push(dest);
			return instruction;
		}

		private Instruction ProcessVariable(IOperation op)
		{
			if (op.Value == null)
			{
				var source = thisParameter;
				var dest = new TemporalVariable(tempCount++);
				var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Copy, source);

				stack.Push(dest);
				return instruction;
			}
			else if (op.Value is IParameterDefinition)
			{
				var paramDef = op.Value as IParameterDefinition;
				var source = parametersDef[paramDef];
				var dest = new TemporalVariable(tempCount++);
				var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Copy, source);

				stack.Push(dest);
				return instruction;
			}
			else if (op.Value is ILocalDefinition)
			{
				var localDef = op.Value as ILocalDefinition;
				var source = localsDef[localDef];
				var dest = new TemporalVariable(tempCount++);
				var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Copy, source);

				stack.Push(dest);
				return instruction;
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		private Instruction ProcessAssignment(IOperation op)
		{
			if (op.Value == null)
			{
				var dest = thisParameter;
				var source = stack.Pop();
				var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Copy, source);

				return instruction;
			}
			else if (op.Value is IParameterDefinition)
			{
				var paramDef = op.Value as IParameterDefinition;
				var dest = parametersDef[paramDef];
				var source = stack.Pop();
				var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Copy, source);

				return instruction;
			}
			else if (op.Value is ILocalDefinition)
			{
				var localDef = op.Value as ILocalDefinition;
				var dest = localsDef[localDef];
				var source = stack.Pop();
				var instruction = new UnaryInstruction(op.Offset, dest, UnaryOperation.Copy, source);

				return instruction;
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		private Instruction ProcessEmptyOperation(IOperation op, EmptyOperation operation)
		{
			var instruction = new EmptyInstruction(op.Offset, operation);
			return instruction;
		}

		private Instruction ProcessBinaryOperation(IOperation op, BinaryOperation operation)
		{
			var leftOp = stack.Pop();
			var rightOp = stack.Pop();
			var dest = new TemporalVariable(tempCount++);
			var instruction = new BinaryInstruction(op.Offset, dest, leftOp, operation, rightOp);

			stack.Push(dest);
			return instruction;
		}
	}
}
