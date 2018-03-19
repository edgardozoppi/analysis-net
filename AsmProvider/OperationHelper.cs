// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Bytecode;
using Model.Types;
using Model;
using AsmCore = Asm.Core;

namespace AsmProvider
{
	public static class OperationHelper
	{
		public static BasicOperation ToBasicOperation(int opcode)
		{
			switch (opcode)
			{
				case AsmCore.Opcodes.NOP:			return BasicOperation.Nop;
				case AsmCore.Opcodes.IALOAD:
				case AsmCore.Opcodes.LALOAD:
				case AsmCore.Opcodes.FALOAD:
				case AsmCore.Opcodes.DALOAD:
				case AsmCore.Opcodes.AALOAD:
				case AsmCore.Opcodes.BALOAD:
				case AsmCore.Opcodes.CALOAD:
				case AsmCore.Opcodes.SALOAD:		return BasicOperation.LoadArrayElement;
				case AsmCore.Opcodes.IASTORE:
				case AsmCore.Opcodes.LASTORE:
				case AsmCore.Opcodes.FASTORE:
				case AsmCore.Opcodes.DASTORE:
				case AsmCore.Opcodes.AASTORE:
				case AsmCore.Opcodes.BASTORE:
				case AsmCore.Opcodes.CASTORE:
				case AsmCore.Opcodes.SASTORE:		return BasicOperation.StoreArrayElement;
				case AsmCore.Opcodes.POP:
				case AsmCore.Opcodes.POP2:			return BasicOperation.Pop;
				case AsmCore.Opcodes.DUP:
				case AsmCore.Opcodes.DUP_X1:
				case AsmCore.Opcodes.DUP_X2:
				case AsmCore.Opcodes.DUP2:
				case AsmCore.Opcodes.DUP2_X1:
				case AsmCore.Opcodes.DUP2_X2:		return BasicOperation.Dup;
				//case AsmCore.Opcodes.SWAP:			return BasicOperation.Swap;
				case AsmCore.Opcodes.IADD:
				case AsmCore.Opcodes.LADD:
				case AsmCore.Opcodes.FADD:
				case AsmCore.Opcodes.DADD:			return BasicOperation.Add;
				case AsmCore.Opcodes.ISUB:
				case AsmCore.Opcodes.LSUB:
				case AsmCore.Opcodes.FSUB:
				case AsmCore.Opcodes.DSUB:			return BasicOperation.Sub;
				case AsmCore.Opcodes.IMUL:
				case AsmCore.Opcodes.LMUL:
				case AsmCore.Opcodes.FMUL:
				case AsmCore.Opcodes.DMUL:			return BasicOperation.Mul;
				case AsmCore.Opcodes.IDIV:
				case AsmCore.Opcodes.LDIV:
				case AsmCore.Opcodes.FDIV:
				case AsmCore.Opcodes.DDIV:			return BasicOperation.Div;
				case AsmCore.Opcodes.IREM:
				case AsmCore.Opcodes.LREM:
				case AsmCore.Opcodes.FREM:
				case AsmCore.Opcodes.DREM:			return BasicOperation.Rem;
				case AsmCore.Opcodes.INEG:
				case AsmCore.Opcodes.LNEG:
				case AsmCore.Opcodes.FNEG:
				case AsmCore.Opcodes.DNEG:			return BasicOperation.Neg;
				case AsmCore.Opcodes.ISHL:
				case AsmCore.Opcodes.LSHL:			return BasicOperation.Shl;
				case AsmCore.Opcodes.ISHR:
				case AsmCore.Opcodes.LSHR:
				case AsmCore.Opcodes.IUSHR:
				case AsmCore.Opcodes.LUSHR:			return BasicOperation.Shr;
				case AsmCore.Opcodes.IAND:
				case AsmCore.Opcodes.LAND:			return BasicOperation.And;
				case AsmCore.Opcodes.IOR:
				case AsmCore.Opcodes.LOR:			return BasicOperation.Or;
				case AsmCore.Opcodes.IXOR:
				case AsmCore.Opcodes.LXOR:			return BasicOperation.Xor;
				//case AsmCore.Opcodes.LCMP:
				//case AsmCore.Opcodes.FCMPL:
				//case AsmCore.Opcodes.FCMPG:
				//case AsmCore.Opcodes.DCMPL:
				//case AsmCore.Opcodes.DCMPG:			return BasicOperation.Cmp;
				case AsmCore.Opcodes.IRETURN:
				case AsmCore.Opcodes.LRETURN:
				case AsmCore.Opcodes.FRETURN:
				case AsmCore.Opcodes.DRETURN:
				case AsmCore.Opcodes.ARETURN:
				case AsmCore.Opcodes.RETURN:		return BasicOperation.Return;
				case AsmCore.Opcodes.ARRAYLENGTH:	return BasicOperation.LoadArrayLength;
				case AsmCore.Opcodes.ATHROW:		return BasicOperation.Throw;
				//case AsmCore.Opcodes.MONITORENTER:
				//case AsmCore.Opcodes.MONITOREXIT:	return BasicOperation.Monitor;

				default: throw opcode.ToUnknownValueException();
			}
		}

		/*
		public static ConvertOperation ToConvertOperation(int opcode)
		{
			switch (opcode)
			{
				case AsmCore.Opcodes.Castclass:
				case AsmCore.Opcodes.Isinst:		return ConvertOperation.Cast;
				case AsmCore.Opcodes.Box:			return ConvertOperation.Box;
				case AsmCore.Opcodes.Unbox:		return ConvertOperation.UnboxPtr;
				case AsmCore.Opcodes.Unbox_Any:	return ConvertOperation.Unbox;
				case AsmCore.Opcodes.Conv_I:
				case AsmCore.Opcodes.Conv_Ovf_I:
				case AsmCore.Opcodes.Conv_Ovf_I_Un:
				case AsmCore.Opcodes.Conv_I1:
				case AsmCore.Opcodes.Conv_Ovf_I1:
				case AsmCore.Opcodes.Conv_Ovf_I1_Un:
				case AsmCore.Opcodes.Conv_I2:
				case AsmCore.Opcodes.Conv_Ovf_I2:
				case AsmCore.Opcodes.Conv_Ovf_I2_Un:
				case AsmCore.Opcodes.Conv_I4:
				case AsmCore.Opcodes.Conv_Ovf_I4:
				case AsmCore.Opcodes.Conv_Ovf_I4_Un:
				case AsmCore.Opcodes.Conv_I8:
				case AsmCore.Opcodes.Conv_Ovf_I8:
				case AsmCore.Opcodes.Conv_Ovf_I8_Un:
				case AsmCore.Opcodes.Conv_U:
				case AsmCore.Opcodes.Conv_Ovf_U:
				case AsmCore.Opcodes.Conv_Ovf_U_Un:
				case AsmCore.Opcodes.Conv_U1:
				case AsmCore.Opcodes.Conv_Ovf_U1:
				case AsmCore.Opcodes.Conv_Ovf_U1_Un:
				case AsmCore.Opcodes.Conv_U2:
				case AsmCore.Opcodes.Conv_Ovf_U2:
				case AsmCore.Opcodes.Conv_Ovf_U2_Un:
				case AsmCore.Opcodes.Conv_U4:
				case AsmCore.Opcodes.Conv_Ovf_U4:
				case AsmCore.Opcodes.Conv_Ovf_U4_Un:
				case AsmCore.Opcodes.Conv_U8:
				case AsmCore.Opcodes.Conv_Ovf_U8:
				case AsmCore.Opcodes.Conv_Ovf_U8_Un:
				case AsmCore.Opcodes.Conv_R4:
				case AsmCore.Opcodes.Conv_R8:
				case AsmCore.Opcodes.Conv_R_Un:	return ConvertOperation.Conv;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static BranchOperation ToBranchOperation(int opcode)
		{
			switch (opcode)
			{
				case AsmCore.Opcodes.Brfalse:
				case AsmCore.Opcodes.Brfalse_S:	return BranchOperation.False;
				case AsmCore.Opcodes.Brtrue:
				case AsmCore.Opcodes.Brtrue_S:	return BranchOperation.True;
				case AsmCore.Opcodes.Beq:
				case AsmCore.Opcodes.Beq_S:		return BranchOperation.Eq;
				case AsmCore.Opcodes.Bne_Un:
				case AsmCore.Opcodes.Bne_Un_S:	return BranchOperation.Neq;
				case AsmCore.Opcodes.Bge:
				case AsmCore.Opcodes.Bge_S:
				case AsmCore.Opcodes.Bge_Un:
				case AsmCore.Opcodes.Bge_Un_S:	return BranchOperation.Ge;
				case AsmCore.Opcodes.Bgt:
				case AsmCore.Opcodes.Bgt_S:
				case AsmCore.Opcodes.Bgt_Un:
				case AsmCore.Opcodes.Bgt_Un_S:	return BranchOperation.Gt;
				case AsmCore.Opcodes.Ble:
				case AsmCore.Opcodes.Ble_S:
				case AsmCore.Opcodes.Ble_Un:
				case AsmCore.Opcodes.Ble_Un_S:	return BranchOperation.Le;
				case AsmCore.Opcodes.Blt:
				case AsmCore.Opcodes.Blt_S:
				case AsmCore.Opcodes.Blt_Un:
				case AsmCore.Opcodes.Blt_Un_S:	return BranchOperation.Lt;
				case AsmCore.Opcodes.Leave:
				case AsmCore.Opcodes.Leave_S:		return BranchOperation.Leave;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static MethodCallOperation ToMethodCallOperation(int opcode)
		{
			switch (opcode)
			{
				case AsmCore.Opcodes.Call:		return MethodCallOperation.Static;
				case AsmCore.Opcodes.Callvirt:	return MethodCallOperation.Virtual;
				case AsmCore.Opcodes.Jmp:			return MethodCallOperation.Jump;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static LoadOperation ToLoadOperation(int opcode)
		{
			switch (opcode)
			{
				case AsmCore.Opcodes.Ldc_I4:
				case AsmCore.Opcodes.Ldc_I4_0:
				case AsmCore.Opcodes.Ldc_I4_1:
				case AsmCore.Opcodes.Ldc_I4_2:
				case AsmCore.Opcodes.Ldc_I4_3:
				case AsmCore.Opcodes.Ldc_I4_4:
				case AsmCore.Opcodes.Ldc_I4_5:
				case AsmCore.Opcodes.Ldc_I4_6:
				case AsmCore.Opcodes.Ldc_I4_7:
				case AsmCore.Opcodes.Ldc_I4_8:
				case AsmCore.Opcodes.Ldc_I4_M1:
				case AsmCore.Opcodes.Ldc_I4_S:
				case AsmCore.Opcodes.Ldc_I8:
				case AsmCore.Opcodes.Ldc_R4:
				case AsmCore.Opcodes.Ldc_R8:
				case AsmCore.Opcodes.Ldnull:
				case AsmCore.Opcodes.Ldstr: return LoadOperation.Value;
				case AsmCore.Opcodes.Ldarg:
				case AsmCore.Opcodes.Ldarg_0:
				case AsmCore.Opcodes.Ldarg_1:
				case AsmCore.Opcodes.Ldarg_2:
				case AsmCore.Opcodes.Ldarg_3:
				case AsmCore.Opcodes.Ldarg_S:
				case AsmCore.Opcodes.Ldloc:
				case AsmCore.Opcodes.Ldloc_0:
				case AsmCore.Opcodes.Ldloc_1:
				case AsmCore.Opcodes.Ldloc_2:
				case AsmCore.Opcodes.Ldloc_3:
				case AsmCore.Opcodes.Ldloc_S: return LoadOperation.Content;
				case AsmCore.Opcodes.Ldarga:
				case AsmCore.Opcodes.Ldarga_S:
				case AsmCore.Opcodes.Ldloca:
				case AsmCore.Opcodes.Ldloca_S: return LoadOperation.Address;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static LoadFieldOperation ToLoadFieldOperation(int opcode)
		{
			switch (opcode)
			{
				case AsmCore.Opcodes.Ldfld:
				case AsmCore.Opcodes.Ldsfld: return LoadFieldOperation.Content;
				case AsmCore.Opcodes.Ldflda:
				case AsmCore.Opcodes.Ldsflda: return LoadFieldOperation.Address;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static LoadMethodAddressOperation ToLoadMethodAddressOperation(int opcode)
		{
			switch (opcode)
			{
				case AsmCore.Opcodes.Ldftn: return LoadMethodAddressOperation.Static;
				case AsmCore.Opcodes.Ldvirtftn: return LoadMethodAddressOperation.Virtual;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static object GetOperationConstant(Cci.IOperation op)
		{
			switch (op.OperationCode)
			{
				case AsmCore.Opcodes.Ldc_I4_0: return 0;
				case AsmCore.Opcodes.Ldc_I4_1: return 1;
				case AsmCore.Opcodes.Ldc_I4_2: return 2;
				case AsmCore.Opcodes.Ldc_I4_3: return 3;
				case AsmCore.Opcodes.Ldc_I4_4: return 4;
				case AsmCore.Opcodes.Ldc_I4_5: return 5;
				case AsmCore.Opcodes.Ldc_I4_6: return 6;
				case AsmCore.Opcodes.Ldc_I4_7: return 7;
				case AsmCore.Opcodes.Ldc_I4_8: return 8;
				case AsmCore.Opcodes.Ldc_I4_M1: return -1;
				case AsmCore.Opcodes.Ldc_I4:
				case AsmCore.Opcodes.Ldc_I4_S:
				case AsmCore.Opcodes.Ldc_I8:
				case AsmCore.Opcodes.Ldc_R4:
				case AsmCore.Opcodes.Ldc_R8:
				case AsmCore.Opcodes.Ldnull:
				case AsmCore.Opcodes.Ldstr: return op.Value;

				default: throw op.OperationCode.ToUnknownValueException();
			}
		}

		public static IType GetOperationType(int opcode)
		{
			switch (opcode)
			{
				case AsmCore.Opcodes.Ldind_I:
				case AsmCore.Opcodes.Stind_I:
				case AsmCore.Opcodes.Stelem_I:
				case AsmCore.Opcodes.Conv_I:
				case AsmCore.Opcodes.Conv_Ovf_I:
				case AsmCore.Opcodes.Conv_Ovf_I_Un:	return PlatformTypes.IntPtr;
				case AsmCore.Opcodes.Ldind_I1:
				case AsmCore.Opcodes.Stind_I1:
				case AsmCore.Opcodes.Stelem_I1:
				case AsmCore.Opcodes.Conv_I1:
				case AsmCore.Opcodes.Conv_Ovf_I1:
				case AsmCore.Opcodes.Conv_Ovf_I1_Un:	return PlatformTypes.Int8;
				case AsmCore.Opcodes.Ldind_I2:
				case AsmCore.Opcodes.Stind_I2:
				case AsmCore.Opcodes.Stelem_I2:
				case AsmCore.Opcodes.Conv_I2:
				case AsmCore.Opcodes.Conv_Ovf_I2:
				case AsmCore.Opcodes.Conv_Ovf_I2_Un:	return PlatformTypes.Int16;
				case AsmCore.Opcodes.Ldc_I4:
				case AsmCore.Opcodes.Ldc_I4_0:
				case AsmCore.Opcodes.Ldc_I4_1:
				case AsmCore.Opcodes.Ldc_I4_2:
				case AsmCore.Opcodes.Ldc_I4_3:
				case AsmCore.Opcodes.Ldc_I4_4:
				case AsmCore.Opcodes.Ldc_I4_5:
				case AsmCore.Opcodes.Ldc_I4_6:
				case AsmCore.Opcodes.Ldc_I4_7:
				case AsmCore.Opcodes.Ldc_I4_8:
				case AsmCore.Opcodes.Ldc_I4_M1:
				case AsmCore.Opcodes.Ldc_I4_S:
				case AsmCore.Opcodes.Ldind_I4:
				case AsmCore.Opcodes.Stind_I4:
				case AsmCore.Opcodes.Stelem_I4:
				case AsmCore.Opcodes.Conv_I4:
				case AsmCore.Opcodes.Conv_Ovf_I4:
				case AsmCore.Opcodes.Conv_Ovf_I4_Un:	return PlatformTypes.Int32;
				case AsmCore.Opcodes.Ldc_I8:
				case AsmCore.Opcodes.Ldind_I8:
				case AsmCore.Opcodes.Stind_I8:
				case AsmCore.Opcodes.Stelem_I8:
				case AsmCore.Opcodes.Conv_I8:
				case AsmCore.Opcodes.Conv_Ovf_I8:
				case AsmCore.Opcodes.Conv_Ovf_I8_Un:	return PlatformTypes.Int64;
				case AsmCore.Opcodes.Conv_U:
				case AsmCore.Opcodes.Conv_Ovf_U:
				case AsmCore.Opcodes.Conv_Ovf_U_Un:
				case AsmCore.Opcodes.Ldlen:			return PlatformTypes.UIntPtr;
				case AsmCore.Opcodes.Ldind_U1:
				case AsmCore.Opcodes.Conv_U1:
				case AsmCore.Opcodes.Conv_Ovf_U1:
				case AsmCore.Opcodes.Conv_Ovf_U1_Un:	return PlatformTypes.UInt8;
				case AsmCore.Opcodes.Ldind_U2:
				case AsmCore.Opcodes.Conv_U2:
				case AsmCore.Opcodes.Conv_Ovf_U2:
				case AsmCore.Opcodes.Conv_Ovf_U2_Un:	return PlatformTypes.UInt16;
				case AsmCore.Opcodes.Ldind_U4:
				case AsmCore.Opcodes.Conv_U4:
				case AsmCore.Opcodes.Conv_Ovf_U4:
				case AsmCore.Opcodes.Conv_Ovf_U4_Un:
				case AsmCore.Opcodes.Sizeof:			return PlatformTypes.UInt32;
				case AsmCore.Opcodes.Conv_U8:
				case AsmCore.Opcodes.Conv_Ovf_U8:
				case AsmCore.Opcodes.Conv_Ovf_U8_Un:	return PlatformTypes.UInt64;
				case AsmCore.Opcodes.Ldc_R4:
				case AsmCore.Opcodes.Ldind_R4:
				case AsmCore.Opcodes.Stind_R4:
				case AsmCore.Opcodes.Stelem_R4:
				case AsmCore.Opcodes.Conv_R4:			return PlatformTypes.Float32;
				case AsmCore.Opcodes.Ldc_R8:
				case AsmCore.Opcodes.Ldind_R8:
				case AsmCore.Opcodes.Stind_R8:
				case AsmCore.Opcodes.Stelem_R8:
				case AsmCore.Opcodes.Conv_R8:
				case AsmCore.Opcodes.Conv_R_Un:		return PlatformTypes.Float64;
				case AsmCore.Opcodes.Ldnull:			return PlatformTypes.Object;
				case AsmCore.Opcodes.Ldstr:			return PlatformTypes.String;

				default: return null;
			}
		}

		public static bool OperandsAreUnsigned(int opcode)
		{
			switch (opcode)
			{
				case AsmCore.Opcodes.Add_Ovf_Un:
				case AsmCore.Opcodes.Bge_Un:
				case AsmCore.Opcodes.Bge_Un_S:
				case AsmCore.Opcodes.Bgt_Un:
				case AsmCore.Opcodes.Bgt_Un_S:
				case AsmCore.Opcodes.Ble_Un:
				case AsmCore.Opcodes.Ble_Un_S:
				case AsmCore.Opcodes.Blt_Un:
				case AsmCore.Opcodes.Blt_Un_S:
				case AsmCore.Opcodes.Bne_Un:
				case AsmCore.Opcodes.Bne_Un_S:
				case AsmCore.Opcodes.Cgt_Un:
				case AsmCore.Opcodes.Clt_Un:
				case AsmCore.Opcodes.Conv_Ovf_I1_Un:
				case AsmCore.Opcodes.Conv_Ovf_I2_Un:
				case AsmCore.Opcodes.Conv_Ovf_I4_Un:
				case AsmCore.Opcodes.Conv_Ovf_I8_Un:
				case AsmCore.Opcodes.Conv_Ovf_I_Un:
				case AsmCore.Opcodes.Conv_Ovf_U1_Un:
				case AsmCore.Opcodes.Conv_Ovf_U2_Un:
				case AsmCore.Opcodes.Conv_Ovf_U4_Un:
				case AsmCore.Opcodes.Conv_Ovf_U8_Un:
				case AsmCore.Opcodes.Conv_Ovf_U_Un:
				case AsmCore.Opcodes.Conv_R_Un:
				case AsmCore.Opcodes.Div_Un:
				case AsmCore.Opcodes.Mul_Ovf_Un:
				case AsmCore.Opcodes.Rem_Un:
				case AsmCore.Opcodes.Shr_Un:
				case AsmCore.Opcodes.Sub_Ovf_Un:	return true;

				default:							return false;
			}
		}

		public static bool PerformsOverflowCheck(int opcode)
		{
			switch (opcode)
			{
				case AsmCore.Opcodes.Add_Ovf:
				case AsmCore.Opcodes.Add_Ovf_Un:
				case AsmCore.Opcodes.Conv_Ovf_I:
				case AsmCore.Opcodes.Conv_Ovf_I1:
				case AsmCore.Opcodes.Conv_Ovf_I1_Un:
				case AsmCore.Opcodes.Conv_Ovf_I2:
				case AsmCore.Opcodes.Conv_Ovf_I2_Un:
				case AsmCore.Opcodes.Conv_Ovf_I4:
				case AsmCore.Opcodes.Conv_Ovf_I4_Un:
				case AsmCore.Opcodes.Conv_Ovf_I8:
				case AsmCore.Opcodes.Conv_Ovf_I8_Un:
				case AsmCore.Opcodes.Conv_Ovf_I_Un:
				case AsmCore.Opcodes.Conv_Ovf_U:
				case AsmCore.Opcodes.Conv_Ovf_U1:
				case AsmCore.Opcodes.Conv_Ovf_U1_Un:
				case AsmCore.Opcodes.Conv_Ovf_U2:
				case AsmCore.Opcodes.Conv_Ovf_U2_Un:
				case AsmCore.Opcodes.Conv_Ovf_U4:
				case AsmCore.Opcodes.Conv_Ovf_U4_Un:
				case AsmCore.Opcodes.Conv_Ovf_U8:
				case AsmCore.Opcodes.Conv_Ovf_U8_Un:
				case AsmCore.Opcodes.Conv_Ovf_U_Un:
				case AsmCore.Opcodes.Mul_Ovf:
				case AsmCore.Opcodes.Mul_Ovf_Un:
				case AsmCore.Opcodes.Sub_Ovf:
				case AsmCore.Opcodes.Sub_Ovf_Un:	return true;

				default:							return false;
			}
		}
		*/
	}
}
