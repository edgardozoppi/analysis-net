// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.ThreeAddressCode;
using Model.ThreeAddressCode.Values;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cci = Microsoft.Cci;
using Model;
using Model.Bytecode;

namespace CCIProvider
{
	internal class CodeProvider
	{
		private TypeExtractor typeExtractor;
		private Cci.ISourceLocationProvider sourceLocationProvider;
		private IDictionary<Cci.IParameterDefinition, IVariable> parameters;
		private IDictionary<Cci.ILocalDefinition, IVariable> locals;
		private IVariable thisParameter;

		public CodeProvider(TypeExtractor typeExtractor, Cci.ISourceLocationProvider sourceLocationProvider)
		{
			this.typeExtractor = typeExtractor;
			this.sourceLocationProvider = sourceLocationProvider;
			this.parameters = new Dictionary<Cci.IParameterDefinition, IVariable>();
			this.locals = new Dictionary<Cci.ILocalDefinition, IVariable>();
		}
		
		public MethodBody ExtractBody(Cci.IMethodBody cciBody)
		{
			var ourBody = new MethodBody(MethodBodyKind.Bytecode);

			ourBody.MaxStack = cciBody.MaxStack;
			ExtractParameters(cciBody.MethodDefinition, ourBody.Parameters);
			ExtractLocalVariables(cciBody.LocalVariables, ourBody.LocalVariables);
			ExtractExceptionInformation(cciBody.OperationExceptionInformation, ourBody.ExceptionInformation);
			ExtractInstructions(cciBody.Operations, ourBody.Instructions);

			return ourBody;
		}

		private void ExtractParameters(Cci.IMethodDefinition methoddef, IList<IVariable> ourParameters)
		{
			if (!methoddef.IsStatic)
			{
				var type = typeExtractor.ExtractType(methoddef.ContainingType);
				var v = new LocalVariable("this", true) { Type = type };

				ourParameters.Add(v);
				thisParameter = v;
			}

			foreach (var parameter in methoddef.Parameters)
			{
				var type = typeExtractor.ExtractType(parameter.Type, parameter.IsByReference);
				var v = new LocalVariable(parameter.Name.Value, true) { Type = type };

				ourParameters.Add(v);
				parameters.Add(parameter, v);
			}
		}

		private void ExtractLocalVariables(IEnumerable<Cci.ILocalDefinition> cciLocalVariables, ISet<IVariable> ourLocalVariables)
		{
			foreach (var local in cciLocalVariables)
			{
				var name = GetLocalSourceName(local);
				var type = typeExtractor.ExtractType(local.Type, local.IsReference);
				var v = new LocalVariable(name) { Type = type };

				ourLocalVariables.Add(v);
				locals.Add(local, v);
			}
		}

		private void ExtractExceptionInformation(IEnumerable<Cci.IOperationExceptionInformation> cciExceptionInformation, IList<ProtectedBlock> ourExceptionInformation)
		{
			foreach (var cciExceptionInfo in cciExceptionInformation)
			{
				var tryHandler = new ProtectedBlock(cciExceptionInfo.TryStartOffset, cciExceptionInfo.TryEndOffset);

				switch (cciExceptionInfo.HandlerKind)
				{
					case Cci.HandlerKind.Catch:
						var exceptionType = typeExtractor.ExtractType(cciExceptionInfo.ExceptionType);
						var catchHandler = new CatchExceptionHandler(cciExceptionInfo.HandlerStartOffset, cciExceptionInfo.HandlerEndOffset, exceptionType);
						tryHandler.Handler = catchHandler;
						break;

					case Cci.HandlerKind.Fault:
						var faultHandler = new FaultExceptionHandler(cciExceptionInfo.HandlerStartOffset, cciExceptionInfo.HandlerEndOffset);
						tryHandler.Handler = faultHandler;
						break;

					case Cci.HandlerKind.Finally:
						var finallyHandler = new FinallyExceptionHandler(cciExceptionInfo.HandlerStartOffset, cciExceptionInfo.HandlerEndOffset);
						tryHandler.Handler = finallyHandler;
						break;

					default:
						throw new Exception("Unknown exception handler block kind");
				}

				ourExceptionInformation.Add(tryHandler);
			}
		}

		private void ExtractInstructions(IEnumerable<Cci.IOperation> operations, IList<IInstruction> instructions)
		{
			foreach (var op in operations)
			{
				var instruction = ExtractInstruction(op);
				instructions.Add(instruction);
			}
		}

		private IInstruction ExtractInstruction(Cci.IOperation operation)
		{
			IInstruction instruction = null;

			switch (operation.OperationCode)
			{
				case Cci.OperationCode.Add:
				case Cci.OperationCode.Add_Ovf:
				case Cci.OperationCode.Add_Ovf_Un:
				case Cci.OperationCode.And:
				case Cci.OperationCode.Ceq:
				case Cci.OperationCode.Cgt:
				case Cci.OperationCode.Cgt_Un:
				case Cci.OperationCode.Clt:
				case Cci.OperationCode.Clt_Un:
				case Cci.OperationCode.Div:
				case Cci.OperationCode.Div_Un:
				case Cci.OperationCode.Mul:
				case Cci.OperationCode.Mul_Ovf:
				case Cci.OperationCode.Mul_Ovf_Un:
				case Cci.OperationCode.Or:
				case Cci.OperationCode.Rem:
				case Cci.OperationCode.Rem_Un:
				case Cci.OperationCode.Shl:
				case Cci.OperationCode.Shr:
				case Cci.OperationCode.Shr_Un:
				case Cci.OperationCode.Sub:
				case Cci.OperationCode.Sub_Ovf:
				case Cci.OperationCode.Sub_Ovf_Un:
				case Cci.OperationCode.Xor:
					instruction = ProcessBasic(operation);
					break;

				//case Cci.OperationCode.Arglist:
				//    //expression = new RuntimeArgumentHandleExpression();
				//    break;

				case Cci.OperationCode.Array_Create_WithLowerBound:
				case Cci.OperationCode.Array_Create:
				case Cci.OperationCode.Newarr:
					instruction = ProcessCreateArray(operation);
					break;

				case Cci.OperationCode.Array_Get:
                    instruction = ProcessGetArray(operation);
                    break;

				case Cci.OperationCode.Ldelem:
				case Cci.OperationCode.Ldelem_I:
				case Cci.OperationCode.Ldelem_I1:
				case Cci.OperationCode.Ldelem_I2:
				case Cci.OperationCode.Ldelem_I4:
				case Cci.OperationCode.Ldelem_I8:
				case Cci.OperationCode.Ldelem_R4:
				case Cci.OperationCode.Ldelem_R8:
				case Cci.OperationCode.Ldelem_U1:
				case Cci.OperationCode.Ldelem_U2:
				case Cci.OperationCode.Ldelem_U4:
				case Cci.OperationCode.Ldelem_Ref:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Array_Addr:
				case Cci.OperationCode.Ldelema:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Beq:
				case Cci.OperationCode.Beq_S:
				case Cci.OperationCode.Bne_Un:
				case Cci.OperationCode.Bne_Un_S:
				case Cci.OperationCode.Bge:
				case Cci.OperationCode.Bge_S:
				case Cci.OperationCode.Bge_Un:
				case Cci.OperationCode.Bge_Un_S:
				case Cci.OperationCode.Bgt:
				case Cci.OperationCode.Bgt_S:
				case Cci.OperationCode.Bgt_Un:
				case Cci.OperationCode.Bgt_Un_S:
				case Cci.OperationCode.Ble:
				case Cci.OperationCode.Ble_S:
				case Cci.OperationCode.Ble_Un:
				case Cci.OperationCode.Ble_Un_S:
				case Cci.OperationCode.Blt:
				case Cci.OperationCode.Blt_S:
				case Cci.OperationCode.Blt_Un:
				case Cci.OperationCode.Blt_Un_S:
					instruction = ProcessBinaryConditionalBranch(operation);
					break;

				case Cci.OperationCode.Br:
				case Cci.OperationCode.Br_S:
					instruction = ProcessUnconditionalBranch(operation);
					break;

				case Cci.OperationCode.Leave:
				case Cci.OperationCode.Leave_S:
					instruction = ProcessLeave(operation);
					break;

				case Cci.OperationCode.Break:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Nop:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Brfalse:
				case Cci.OperationCode.Brfalse_S:
				case Cci.OperationCode.Brtrue:
				case Cci.OperationCode.Brtrue_S:
					instruction = ProcessUnaryConditionalBranch(operation);
					break;

				case Cci.OperationCode.Call:
				case Cci.OperationCode.Callvirt:
				case Cci.OperationCode.Jmp:
					instruction = ProcessMethodCall(operation);
					break;

				case Cci.OperationCode.Calli:
					instruction = ProcessMethodCallIndirect(operation);
					break;

				case Cci.OperationCode.Castclass:
				case Cci.OperationCode.Isinst:
				case Cci.OperationCode.Box:
				case Cci.OperationCode.Unbox:
				case Cci.OperationCode.Unbox_Any:
				case Cci.OperationCode.Conv_I:
				case Cci.OperationCode.Conv_Ovf_I:
				case Cci.OperationCode.Conv_Ovf_I_Un:
				case Cci.OperationCode.Conv_I1:
				case Cci.OperationCode.Conv_Ovf_I1:
				case Cci.OperationCode.Conv_Ovf_I1_Un:
				case Cci.OperationCode.Conv_I2:
				case Cci.OperationCode.Conv_Ovf_I2:
				case Cci.OperationCode.Conv_Ovf_I2_Un:
				case Cci.OperationCode.Conv_I4:
				case Cci.OperationCode.Conv_Ovf_I4:
				case Cci.OperationCode.Conv_Ovf_I4_Un:
				case Cci.OperationCode.Conv_I8:
				case Cci.OperationCode.Conv_Ovf_I8:
				case Cci.OperationCode.Conv_Ovf_I8_Un:
				case Cci.OperationCode.Conv_U:
				case Cci.OperationCode.Conv_Ovf_U:
				case Cci.OperationCode.Conv_Ovf_U_Un:
				case Cci.OperationCode.Conv_U1:
				case Cci.OperationCode.Conv_Ovf_U1:
				case Cci.OperationCode.Conv_Ovf_U1_Un:
				case Cci.OperationCode.Conv_U2:
				case Cci.OperationCode.Conv_Ovf_U2:
				case Cci.OperationCode.Conv_Ovf_U2_Un:
				case Cci.OperationCode.Conv_U4:
				case Cci.OperationCode.Conv_Ovf_U4:
				case Cci.OperationCode.Conv_Ovf_U4_Un:
				case Cci.OperationCode.Conv_U8:
				case Cci.OperationCode.Conv_Ovf_U8:
				case Cci.OperationCode.Conv_Ovf_U8_Un:
				case Cci.OperationCode.Conv_R4:
				case Cci.OperationCode.Conv_R8:
				case Cci.OperationCode.Conv_R_Un:
					instruction = ProcessConversion(operation);
					break;

				//case Cci.OperationCode.Ckfinite:
				//    var operand = result = PopOperandStack();
				//    var chkfinite = new MutableCodeModel.MethodReference()
				//    {
				//        CallingConvention = Cci.CallingConvention.FastCall,
				//        ContainingType = host.PlatformType.SystemFloat64,
				//        Name = result = host.NameTable.GetNameFor("__ckfinite__"),
				//        Type = host.PlatformType.SystemFloat64,
				//        InternFactory = host.InternFactory,
				//    };
				//    expression = new MethodCall() { Arguments = new List<IExpression>(1) { operand }, IsStaticCall = true, Type = operand.Type, MethodToCall = chkfinite };
				//    break;

				//case Cci.OperationCode.Constrained_:
				//	// This prefix is redundant and is not represented in the code model.
				//	break;

				case Cci.OperationCode.Cpblk:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Cpobj:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Dup:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Endfilter:
				case Cci.OperationCode.Endfinally:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Initblk:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Initobj:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Ldarg:
				case Cci.OperationCode.Ldarg_0:
				case Cci.OperationCode.Ldarg_1:
				case Cci.OperationCode.Ldarg_2:
				case Cci.OperationCode.Ldarg_3:
				case Cci.OperationCode.Ldarg_S:
				case Cci.OperationCode.Ldarga:
				case Cci.OperationCode.Ldarga_S:
					instruction = ProcessLoadArgument(operation);
					break;

				case Cci.OperationCode.Ldloc:
				case Cci.OperationCode.Ldloc_0:
				case Cci.OperationCode.Ldloc_1:
				case Cci.OperationCode.Ldloc_2:
				case Cci.OperationCode.Ldloc_3:
				case Cci.OperationCode.Ldloc_S:
				case Cci.OperationCode.Ldloca:
				case Cci.OperationCode.Ldloca_S:
					instruction = ProcessLoadLocal(operation);
					break;

				case Cci.OperationCode.Ldfld:
				case Cci.OperationCode.Ldsfld:
				case Cci.OperationCode.Ldflda:
				case Cci.OperationCode.Ldsflda:
					instruction = ProcessLoadField(operation);
					break;

				case Cci.OperationCode.Ldftn:
				case Cci.OperationCode.Ldvirtftn:
					instruction = ProcessLoadMethodAddress(operation);
					break;

				case Cci.OperationCode.Ldc_I4:
				case Cci.OperationCode.Ldc_I4_0:
				case Cci.OperationCode.Ldc_I4_1:
				case Cci.OperationCode.Ldc_I4_2:
				case Cci.OperationCode.Ldc_I4_3:
				case Cci.OperationCode.Ldc_I4_4:
				case Cci.OperationCode.Ldc_I4_5:
				case Cci.OperationCode.Ldc_I4_6:
				case Cci.OperationCode.Ldc_I4_7:
				case Cci.OperationCode.Ldc_I4_8:
				case Cci.OperationCode.Ldc_I4_M1:
				case Cci.OperationCode.Ldc_I4_S:
				case Cci.OperationCode.Ldc_I8:
				case Cci.OperationCode.Ldc_R4:
				case Cci.OperationCode.Ldc_R8:
				case Cci.OperationCode.Ldnull:
				case Cci.OperationCode.Ldstr:
					instruction = ProcessLoadConstant(operation);
					break;

				case Cci.OperationCode.Ldind_I:
				case Cci.OperationCode.Ldind_I1:
				case Cci.OperationCode.Ldind_I2:
				case Cci.OperationCode.Ldind_I4:
				case Cci.OperationCode.Ldind_I8:
				case Cci.OperationCode.Ldind_R4:
				case Cci.OperationCode.Ldind_R8:
				case Cci.OperationCode.Ldind_Ref:
				case Cci.OperationCode.Ldind_U1:
				case Cci.OperationCode.Ldind_U2:
				case Cci.OperationCode.Ldind_U4:
				case Cci.OperationCode.Ldobj:
					instruction = ProcessLoadIndirect(operation);
					break;

				case Cci.OperationCode.Ldlen:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Ldtoken:
					instruction = ProcessLoadToken(operation);
					break;

				case Cci.OperationCode.Localloc:
					instruction = ProcessBasic(operation);
					break;

				//case Cci.OperationCode.Mkrefany:
				//    expression = result = ParseMakeTypedReference(currentOperation);
				//    break;

				case Cci.OperationCode.Neg:
				case Cci.OperationCode.Not:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Newobj:
					instruction = ProcessCreateObject(operation);
					break;

				//case Cci.OperationCode.No_:
				//	// If code out there actually uses this, I need to know sooner rather than later.
				//	// TODO: need object model support
				//	throw new NotImplementedException("Invalid opcode: No.");

				case Cci.OperationCode.Pop:
					instruction = ProcessBasic(operation);
					break;

				//case Cci.OperationCode.Readonly_:
				//    result = sawReadonly = true;
				//    break;

				//case Cci.OperationCode.Refanytype:
				//    expression = result = ParseGetTypeOfTypedReference();
				//    break;

				//case Cci.OperationCode.Refanyval:
				//    expression = result = ParseGetValueOfTypedReference(currentOperation);
				//    break;

				case Cci.OperationCode.Ret:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Sizeof:
					instruction = ProcessSizeof(operation);
					break;

				case Cci.OperationCode.Starg:
				case Cci.OperationCode.Starg_S:
					instruction = ProcessStoreArgument(operation);
					break;

				case Cci.OperationCode.Array_Set:
				case Cci.OperationCode.Stelem:
				case Cci.OperationCode.Stelem_I:
				case Cci.OperationCode.Stelem_I1:
				case Cci.OperationCode.Stelem_I2:
				case Cci.OperationCode.Stelem_I4:
				case Cci.OperationCode.Stelem_I8:
				case Cci.OperationCode.Stelem_R4:
				case Cci.OperationCode.Stelem_R8:
				case Cci.OperationCode.Stelem_Ref:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Stfld:
				case Cci.OperationCode.Stsfld:
					instruction = ProcessStoreField(operation);
					break;

				case Cci.OperationCode.Stind_I:
				case Cci.OperationCode.Stind_I1:
				case Cci.OperationCode.Stind_I2:
				case Cci.OperationCode.Stind_I4:
				case Cci.OperationCode.Stind_I8:
				case Cci.OperationCode.Stind_R4:
				case Cci.OperationCode.Stind_R8:
				case Cci.OperationCode.Stind_Ref:
				case Cci.OperationCode.Stobj:
					instruction = ProcessBasic(operation);
					break;

				case Cci.OperationCode.Stloc:
				case Cci.OperationCode.Stloc_0:
				case Cci.OperationCode.Stloc_1:
				case Cci.OperationCode.Stloc_2:
				case Cci.OperationCode.Stloc_3:
				case Cci.OperationCode.Stloc_S:
					instruction = ProcessStoreLocal(operation);
					break;

				case Cci.OperationCode.Switch:
					instruction = ProcessSwitch(operation);
					break;

				//case Cci.OperationCode.Tail_:
				//    result = sawTailCall = true;
				//    break;

				case Cci.OperationCode.Throw:
				case Cci.OperationCode.Rethrow:
					instruction = ProcessBasic(operation);
					break;

				//case Cci.OperationCode.Unaligned_:
				//    Contract.Assume(currentOperation.Value is byte);
				//    var alignment = (byte)currentOperation.Value;
				//    Contract.Assume(alignment == 1 || alignment == 2 || alignment == 4);
				//    result = alignment = alignment;
				//    break;

				//case Cci.OperationCode.Volatile_:
				//    result = sawVolatile = true;
				//    break;

				default:
					//Console.WriteLine("Unknown bytecode: {0}", operation.OperationCode);
					//throw new UnknownBytecodeException(operation);
					//continue;

					// Quick fix to preserve the offset in case it is a target location of some jump
					// Otherwise it will break the control-flow analysis later.
					instruction = new BasicInstruction(operation.Offset, BasicOperation.Nop);
					break;
			}

			return instruction;
		}

		private IInstruction ProcessSwitch(Cci.IOperation op)
		{
			var targets = op.Value as uint[];

			var instruction = new SwitchInstruction(op.Offset, targets);
			return instruction;
		}

		private IInstruction ProcessCreateArray(Cci.IOperation op)
		{
			var withLowerBound = OperationHelper.CreateArrayWithLowerBounds(op.OperationCode);
			var cciArrayType = op.Value as Cci.IArrayTypeReference;
			var ourArrayType = typeExtractor.ExtractType(cciArrayType);

			var instruction = new CreateArrayInstruction(op.Offset, ourArrayType);
			instruction.WithLowerBound = withLowerBound;
			return instruction;
		}

        private IInstruction ProcessGetArray(Cci.IOperation op)
        {
            //var getArray = OperationHelper.GetArrayWithLowerBounds(op.OperationCode);
            var cciArrayType = op.Value as Cci.IArrayTypeReference;
            var ourArrayType = typeExtractor.ExtractType(cciArrayType);
            var instruction = new GetArrayInstruction(op.Offset, ourArrayType);
            //instruction.WithLowerBound = withLowerBound;
            return instruction;
        }

		private IInstruction ProcessCreateObject(Cci.IOperation op)
		{
			var cciMethod = op.Value as Cci.IMethodReference;
			var ourMethod = typeExtractor.ExtractReference(cciMethod);

			var instruction = new CreateObjectInstruction(op.Offset, ourMethod);
			return instruction;
		}

		private IInstruction ProcessMethodCall(Cci.IOperation op)
		{
			var operation = OperationHelper.ToMethodCallOperation(op.OperationCode);
			var cciMethod = op.Value as Cci.IMethodReference;
			var ourMethod = typeExtractor.ExtractReference(cciMethod);

			var instruction = new MethodCallInstruction(op.Offset, operation, ourMethod);
			return instruction;
		}

		private IInstruction ProcessMethodCallIndirect(Cci.IOperation op)
		{
			var cciFunctionPointer = op.Value as Cci.IFunctionPointerTypeReference;
			var ourFunctionPointer = typeExtractor.ExtractType(cciFunctionPointer);

			var instruction = new IndirectMethodCallInstruction(op.Offset, ourFunctionPointer);
			return instruction;
		}

		private IInstruction ProcessSizeof(Cci.IOperation op)
		{
			var cciType = op.Value as Cci.ITypeReference;
			var ourType = typeExtractor.ExtractType(cciType);

			var instruction = new SizeofInstruction(op.Offset, ourType);
			return instruction;
		}

		private IInstruction ProcessUnaryConditionalBranch(Cci.IOperation op)
		{
			var operation = OperationHelper.ToBranchOperation(op.OperationCode);
			var target = (uint)op.Value;

			var instruction = new BranchInstruction(op.Offset, operation, target);
			return instruction;
		}

		private IInstruction ProcessBinaryConditionalBranch(Cci.IOperation op)
		{
			var operation = OperationHelper.ToBranchOperation(op.OperationCode);
			var unsigned = OperationHelper.OperandsAreUnsigned(op.OperationCode);
			var target = (uint)op.Value;

			var instruction = new BranchInstruction(op.Offset, operation, target);
			instruction.UnsignedOperands = unsigned;
			return instruction;
		}

		private IInstruction ProcessLeave(Cci.IOperation op)
		{
			var target = (uint)op.Value;
			var instruction = new BranchInstruction(op.Offset, BranchOperation.Leave, target);
			return instruction;
		}

		private IInstruction ProcessUnconditionalBranch(Cci.IOperation op)
		{
			var target = (uint)op.Value;
			var instruction = new BranchInstruction(op.Offset, BranchOperation.Branch, target);
			return instruction;
		}

		private IInstruction ProcessLoadConstant(Cci.IOperation op)
		{
			var operation = OperationHelper.ToLoadOperation(op.OperationCode);
			var type = OperationHelper.GetOperationType(op.OperationCode);
			var value = OperationHelper.GetOperationConstant(op);
			var source = new Constant(value) { Type = type };

			var instruction = new LoadInstruction(op.Offset, operation, source);
			return instruction;
		}

		private IInstruction ProcessLoadArgument(Cci.IOperation op)
		{
			var operation = OperationHelper.ToLoadOperation(op.OperationCode);
			var source = thisParameter;

			if (op.Value is Cci.IParameterDefinition)
			{
				var parameter = op.Value as Cci.IParameterDefinition;
				source = parameters[parameter];
			}

			var instruction = new LoadInstruction(op.Offset, operation, source);
			return instruction;
		}

		private IInstruction ProcessLoadLocal(Cci.IOperation op)
		{
			var operation = OperationHelper.ToLoadOperation(op.OperationCode);
			var local = op.Value as Cci.ILocalDefinition;
			var source = locals[local];

			var instruction = new LoadInstruction(op.Offset, operation, source);
			return instruction;
		}

		private IInstruction ProcessLoadIndirect(Cci.IOperation op)
		{
			var instruction = new BasicInstruction(op.Offset, BasicOperation.IndirectLoad);
			return instruction;
		}

		private IInstruction ProcessLoadField(Cci.IOperation op)
		{
			var operation = OperationHelper.ToLoadFieldOperation(op.OperationCode);
			var cciField = op.Value as Cci.IFieldReference;
			var ourField = typeExtractor.ExtractReference(cciField);

			var instruction = new LoadFieldInstruction(op.Offset, operation, ourField);
			return instruction;
		}

		private IInstruction ProcessLoadMethodAddress(Cci.IOperation op)
		{
			var operation = OperationHelper.ToLoadMethodAddressOperation(op.OperationCode);
			var cciMethod = op.Value as Cci.IMethodReference;
			var ourMethod = typeExtractor.ExtractReference(cciMethod);

			var instruction = new LoadMethodAddressInstruction(op.Offset, operation, ourMethod);
			return instruction;
		}

		private IInstruction ProcessLoadToken(Cci.IOperation op)
		{
			var cciToken = op.Value as Cci.IReference;
			var ourToken = typeExtractor.ExtractToken(cciToken);

			var instruction = new LoadTokenInstruction(op.Offset, ourToken);
			return instruction;
		}

		private IInstruction ProcessStoreArgument(Cci.IOperation op)
		{
			var dest = thisParameter;

			if (op.Value is Cci.IParameterDefinition)
			{
				var parameter = op.Value as Cci.IParameterDefinition;
				dest = parameters[parameter];
			}

			var instruction = new StoreInstruction(op.Offset, dest);
			return instruction;
		}

		private IInstruction ProcessStoreLocal(Cci.IOperation op)
		{
			var local = op.Value as Cci.ILocalDefinition;
			var dest = locals[local];

			var instruction = new StoreInstruction(op.Offset, dest);
			return instruction;
		}

		private IInstruction ProcessStoreField(Cci.IOperation op)
		{
			var cciField = op.Value as Cci.IFieldReference;
			var ourField = typeExtractor.ExtractReference(cciField);

			var instruction = new StoreFieldInstruction(op.Offset, ourField);
			return instruction;
		}

		private IInstruction ProcessBasic(Cci.IOperation op)
		{
			var operation = OperationHelper.ToBasicOperation(op.OperationCode);
			var overflow = OperationHelper.PerformsOverflowCheck(op.OperationCode);
			var unsigned = OperationHelper.OperandsAreUnsigned(op.OperationCode);

			var instruction = new BasicInstruction(op.Offset, operation);
			instruction.OverflowCheck = overflow;
			instruction.UnsignedOperands = unsigned;
			return instruction;
		}

		private IInstruction ProcessConversion(Cci.IOperation op)
		{
			var operation = OperationHelper.ToConvertOperation(op.OperationCode);
			var overflow = OperationHelper.PerformsOverflowCheck(op.OperationCode);
			var unsigned = OperationHelper.OperandsAreUnsigned(op.OperationCode);

			var cciType = op.Value as Cci.ITypeReference;
			var ourType = typeExtractor.ExtractType(cciType);

			if (operation == ConvertOperation.Box && cciType.IsValueType)
			{
				ourType = PlatformTypes.Object;
			}
			else if (operation == ConvertOperation.Conv)
			{
				ourType = OperationHelper.GetOperationType(op.OperationCode);
			}
			
			var instruction = new ConvertInstruction(op.Offset, operation, ourType);
			instruction.OverflowCheck = overflow;
			instruction.UnsignedOperands = unsigned;
			return instruction;
		}

		private string GetLocalSourceName(Cci.ILocalDefinition local)
		{
			var name = local.Name.Value;

			if (sourceLocationProvider != null)
			{
				bool isCompilerGenerated;
				name = sourceLocationProvider.GetSourceNameFor(local, out isCompilerGenerated);
			}

			return name;
		}
	}
}
