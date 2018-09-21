// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cci = Microsoft.Cci;
using Model.Bytecode;
using Model.Types;
using Model;

namespace CCIProvider
{
	public static class OperationHelper
	{
		public static BasicOperation ToBasicOperation(Cci.OperationCode opcode)
		{
			switch (opcode)
			{
				case Cci.OperationCode.Neg:			return BasicOperation.Neg;
				case Cci.OperationCode.Not:			return BasicOperation.Not;
				case Cci.OperationCode.Add:
				case Cci.OperationCode.Add_Ovf:
				case Cci.OperationCode.Add_Ovf_Un:	return BasicOperation.Add;
				case Cci.OperationCode.And:			return BasicOperation.And;
				case Cci.OperationCode.Ceq:			return BasicOperation.Eq;
				case Cci.OperationCode.Cgt:
				case Cci.OperationCode.Cgt_Un:		return BasicOperation.Gt;
				case Cci.OperationCode.Clt:
				case Cci.OperationCode.Clt_Un:		return BasicOperation.Lt;
				case Cci.OperationCode.Div:
				case Cci.OperationCode.Div_Un:		return BasicOperation.Div;
				case Cci.OperationCode.Mul:
				case Cci.OperationCode.Mul_Ovf:
				case Cci.OperationCode.Mul_Ovf_Un:	return BasicOperation.Mul;
				case Cci.OperationCode.Or:			return BasicOperation.Or;
				case Cci.OperationCode.Rem:
				case Cci.OperationCode.Rem_Un:		return BasicOperation.Rem;
				case Cci.OperationCode.Shl:			return BasicOperation.Shl;
				case Cci.OperationCode.Shr:
				case Cci.OperationCode.Shr_Un:		return BasicOperation.Shr;
				case Cci.OperationCode.Sub:
				case Cci.OperationCode.Sub_Ovf:
				case Cci.OperationCode.Sub_Ovf_Un:	return BasicOperation.Sub;
				case Cci.OperationCode.Xor:			return BasicOperation.Xor;
				case Cci.OperationCode.Endfilter:	return BasicOperation.EndFilter;
				case Cci.OperationCode.Endfinally:	return BasicOperation.EndFinally;
				case Cci.OperationCode.Throw:		return BasicOperation.Throw;
				case Cci.OperationCode.Rethrow:		return BasicOperation.Rethrow;
				case Cci.OperationCode.Nop:			return BasicOperation.Nop;
				case Cci.OperationCode.Pop:			return BasicOperation.Pop;
				case Cci.OperationCode.Dup:			return BasicOperation.Dup;
				case Cci.OperationCode.Localloc:	return BasicOperation.LocalAllocation;
				case Cci.OperationCode.Initblk:		return BasicOperation.InitBlock;
				case Cci.OperationCode.Initobj:		return BasicOperation.InitObject;
				case Cci.OperationCode.Cpblk:		return BasicOperation.CopyBlock;
				case Cci.OperationCode.Cpobj:		return BasicOperation.CopyObject;
				case Cci.OperationCode.Ret:			return BasicOperation.Return;
				case Cci.OperationCode.Ldlen:		return BasicOperation.LoadArrayLength;
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
				case Cci.OperationCode.Ldobj:		return BasicOperation.IndirectLoad;
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
				case Cci.OperationCode.Ldelem_Ref:	return BasicOperation.LoadArrayElement;
				case Cci.OperationCode.Ldelema:		return BasicOperation.LoadArrayElementAddress;
				case Cci.OperationCode.Stind_I:
				case Cci.OperationCode.Stind_I1:
				case Cci.OperationCode.Stind_I2:
				case Cci.OperationCode.Stind_I4:
				case Cci.OperationCode.Stind_I8:
				case Cci.OperationCode.Stind_R4:
				case Cci.OperationCode.Stind_R8:
				case Cci.OperationCode.Stind_Ref:
				case Cci.OperationCode.Stobj:		return BasicOperation.IndirectStore;
				case Cci.OperationCode.Stelem:
				case Cci.OperationCode.Stelem_I:
				case Cci.OperationCode.Stelem_I1:
				case Cci.OperationCode.Stelem_I2:
				case Cci.OperationCode.Stelem_I4:
				case Cci.OperationCode.Stelem_I8:
				case Cci.OperationCode.Stelem_R4:
				case Cci.OperationCode.Stelem_R8:
				case Cci.OperationCode.Stelem_Ref:	return BasicOperation.StoreArrayElement;
				case Cci.OperationCode.Break:		return BasicOperation.Breakpoint;
				
				default: throw opcode.ToUnknownValueException();
			}
		}

		public static ConvertOperation ToConvertOperation(Cci.OperationCode opcode)
		{
			switch (opcode)
			{
				case Cci.OperationCode.Castclass:
				case Cci.OperationCode.Isinst:		return ConvertOperation.Cast;
				case Cci.OperationCode.Box:			return ConvertOperation.Box;
				case Cci.OperationCode.Unbox:		return ConvertOperation.UnboxPtr;
				case Cci.OperationCode.Unbox_Any:	return ConvertOperation.Unbox;
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
				case Cci.OperationCode.Conv_R_Un:	return ConvertOperation.Conv;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static BranchOperation ToBranchOperation(Cci.OperationCode opcode)
		{
			switch (opcode)
			{
				case Cci.OperationCode.Brfalse:
				case Cci.OperationCode.Brfalse_S:	return BranchOperation.False;
				case Cci.OperationCode.Brtrue:
				case Cci.OperationCode.Brtrue_S:	return BranchOperation.True;
				case Cci.OperationCode.Beq:
				case Cci.OperationCode.Beq_S:		return BranchOperation.Eq;
				case Cci.OperationCode.Bne_Un:
				case Cci.OperationCode.Bne_Un_S:	return BranchOperation.Neq;
				case Cci.OperationCode.Bge:
				case Cci.OperationCode.Bge_S:
				case Cci.OperationCode.Bge_Un:
				case Cci.OperationCode.Bge_Un_S:	return BranchOperation.Ge;
				case Cci.OperationCode.Bgt:
				case Cci.OperationCode.Bgt_S:
				case Cci.OperationCode.Bgt_Un:
				case Cci.OperationCode.Bgt_Un_S:	return BranchOperation.Gt;
				case Cci.OperationCode.Ble:
				case Cci.OperationCode.Ble_S:
				case Cci.OperationCode.Ble_Un:
				case Cci.OperationCode.Ble_Un_S:	return BranchOperation.Le;
				case Cci.OperationCode.Blt:
				case Cci.OperationCode.Blt_S:
				case Cci.OperationCode.Blt_Un:
				case Cci.OperationCode.Blt_Un_S:	return BranchOperation.Lt;
				case Cci.OperationCode.Leave:
				case Cci.OperationCode.Leave_S:		return BranchOperation.Leave;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static MethodCallOperation ToMethodCallOperation(Cci.OperationCode opcode)
		{
			switch (opcode)
			{
				case Cci.OperationCode.Call:		return MethodCallOperation.Static;
				case Cci.OperationCode.Callvirt:	return MethodCallOperation.Virtual;
				case Cci.OperationCode.Jmp:			return MethodCallOperation.Jump;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static LoadOperation ToLoadOperation(Cci.OperationCode opcode)
		{
			switch (opcode)
			{
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
				case Cci.OperationCode.Ldstr: return LoadOperation.Value;
				case Cci.OperationCode.Ldarg:
				case Cci.OperationCode.Ldarg_0:
				case Cci.OperationCode.Ldarg_1:
				case Cci.OperationCode.Ldarg_2:
				case Cci.OperationCode.Ldarg_3:
				case Cci.OperationCode.Ldarg_S:
				case Cci.OperationCode.Ldloc:
				case Cci.OperationCode.Ldloc_0:
				case Cci.OperationCode.Ldloc_1:
				case Cci.OperationCode.Ldloc_2:
				case Cci.OperationCode.Ldloc_3:
				case Cci.OperationCode.Ldloc_S: return LoadOperation.Content;
				case Cci.OperationCode.Ldarga:
				case Cci.OperationCode.Ldarga_S:
				case Cci.OperationCode.Ldloca:
				case Cci.OperationCode.Ldloca_S: return LoadOperation.Address;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static LoadFieldOperation ToLoadFieldOperation(Cci.OperationCode opcode)
		{
			switch (opcode)
			{
				case Cci.OperationCode.Ldfld:
				case Cci.OperationCode.Ldsfld: return LoadFieldOperation.Content;
				case Cci.OperationCode.Ldflda:
				case Cci.OperationCode.Ldsflda: return LoadFieldOperation.Address;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static LoadArrayElementOperation ToLoadArrayElementOperation(Cci.OperationCode opcode)
		{
			switch (opcode)
			{
				case Cci.OperationCode.Array_Get: return LoadArrayElementOperation.Content;
				case Cci.OperationCode.Array_Addr: return LoadArrayElementOperation.Address;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static LoadMethodAddressOperation ToLoadMethodAddressOperation(Cci.OperationCode opcode)
		{
			switch (opcode)
			{
				case Cci.OperationCode.Ldftn: return LoadMethodAddressOperation.Static;
				case Cci.OperationCode.Ldvirtftn: return LoadMethodAddressOperation.Virtual;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static object GetOperationConstant(Cci.IOperation op)
		{
			switch (op.OperationCode)
			{
				case Cci.OperationCode.Ldc_I4_0: return 0;
				case Cci.OperationCode.Ldc_I4_1: return 1;
				case Cci.OperationCode.Ldc_I4_2: return 2;
				case Cci.OperationCode.Ldc_I4_3: return 3;
				case Cci.OperationCode.Ldc_I4_4: return 4;
				case Cci.OperationCode.Ldc_I4_5: return 5;
				case Cci.OperationCode.Ldc_I4_6: return 6;
				case Cci.OperationCode.Ldc_I4_7: return 7;
				case Cci.OperationCode.Ldc_I4_8: return 8;
				case Cci.OperationCode.Ldc_I4_M1: return -1;
				case Cci.OperationCode.Ldc_I4:
				case Cci.OperationCode.Ldc_I4_S:
				case Cci.OperationCode.Ldc_I8:
				case Cci.OperationCode.Ldc_R4:
				case Cci.OperationCode.Ldc_R8:
				case Cci.OperationCode.Ldnull:
				case Cci.OperationCode.Ldstr: return op.Value;

				default: throw op.OperationCode.ToUnknownValueException();
			}
		}

		public static IType GetOperationType(Cci.OperationCode opcode)
		{
			switch (opcode)
			{
				case Cci.OperationCode.Ldind_I:
				case Cci.OperationCode.Stind_I:
				case Cci.OperationCode.Stelem_I:
				case Cci.OperationCode.Conv_I:
				case Cci.OperationCode.Conv_Ovf_I:
				case Cci.OperationCode.Conv_Ovf_I_Un:	return PlatformTypes.IntPtr;
				case Cci.OperationCode.Ldind_I1:
				case Cci.OperationCode.Stind_I1:
				case Cci.OperationCode.Stelem_I1:
				case Cci.OperationCode.Conv_I1:
				case Cci.OperationCode.Conv_Ovf_I1:
				case Cci.OperationCode.Conv_Ovf_I1_Un:	return PlatformTypes.Int8;
				case Cci.OperationCode.Ldind_I2:
				case Cci.OperationCode.Stind_I2:
				case Cci.OperationCode.Stelem_I2:
				case Cci.OperationCode.Conv_I2:
				case Cci.OperationCode.Conv_Ovf_I2:
				case Cci.OperationCode.Conv_Ovf_I2_Un:	return PlatformTypes.Int16;
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
				case Cci.OperationCode.Ldind_I4:
				case Cci.OperationCode.Stind_I4:
				case Cci.OperationCode.Stelem_I4:
				case Cci.OperationCode.Conv_I4:
				case Cci.OperationCode.Conv_Ovf_I4:
				case Cci.OperationCode.Conv_Ovf_I4_Un:	return PlatformTypes.Int32;
				case Cci.OperationCode.Ldc_I8:
				case Cci.OperationCode.Ldind_I8:
				case Cci.OperationCode.Stind_I8:
				case Cci.OperationCode.Stelem_I8:
				case Cci.OperationCode.Conv_I8:
				case Cci.OperationCode.Conv_Ovf_I8:
				case Cci.OperationCode.Conv_Ovf_I8_Un:	return PlatformTypes.Int64;
				case Cci.OperationCode.Conv_U:
				case Cci.OperationCode.Conv_Ovf_U:
				case Cci.OperationCode.Conv_Ovf_U_Un:
				case Cci.OperationCode.Ldlen:			return PlatformTypes.UIntPtr;
				case Cci.OperationCode.Ldind_U1:
				case Cci.OperationCode.Conv_U1:
				case Cci.OperationCode.Conv_Ovf_U1:
				case Cci.OperationCode.Conv_Ovf_U1_Un:	return PlatformTypes.UInt8;
				case Cci.OperationCode.Ldind_U2:
				case Cci.OperationCode.Conv_U2:
				case Cci.OperationCode.Conv_Ovf_U2:
				case Cci.OperationCode.Conv_Ovf_U2_Un:	return PlatformTypes.UInt16;
				case Cci.OperationCode.Ldind_U4:
				case Cci.OperationCode.Conv_U4:
				case Cci.OperationCode.Conv_Ovf_U4:
				case Cci.OperationCode.Conv_Ovf_U4_Un:
				case Cci.OperationCode.Sizeof:			return PlatformTypes.UInt32;
				case Cci.OperationCode.Conv_U8:
				case Cci.OperationCode.Conv_Ovf_U8:
				case Cci.OperationCode.Conv_Ovf_U8_Un:	return PlatformTypes.UInt64;
				case Cci.OperationCode.Ldc_R4:
				case Cci.OperationCode.Ldind_R4:
				case Cci.OperationCode.Stind_R4:
				case Cci.OperationCode.Stelem_R4:
				case Cci.OperationCode.Conv_R4:			return PlatformTypes.Float32;
				case Cci.OperationCode.Ldc_R8:
				case Cci.OperationCode.Ldind_R8:
				case Cci.OperationCode.Stind_R8:
				case Cci.OperationCode.Stelem_R8:
				case Cci.OperationCode.Conv_R8:
				case Cci.OperationCode.Conv_R_Un:		return PlatformTypes.Float64;
				case Cci.OperationCode.Ldnull:			return PlatformTypes.Object;
				case Cci.OperationCode.Ldstr:			return PlatformTypes.String;

				default: return null;
			}
		}

		public static bool OperandsAreUnsigned(Cci.OperationCode opcode)
		{
			switch (opcode)
			{
				case Cci.OperationCode.Add_Ovf_Un:
				case Cci.OperationCode.Bge_Un:
				case Cci.OperationCode.Bge_Un_S:
				case Cci.OperationCode.Bgt_Un:
				case Cci.OperationCode.Bgt_Un_S:
				case Cci.OperationCode.Ble_Un:
				case Cci.OperationCode.Ble_Un_S:
				case Cci.OperationCode.Blt_Un:
				case Cci.OperationCode.Blt_Un_S:
				case Cci.OperationCode.Bne_Un:
				case Cci.OperationCode.Bne_Un_S:
				case Cci.OperationCode.Cgt_Un:
				case Cci.OperationCode.Clt_Un:
				case Cci.OperationCode.Conv_Ovf_I1_Un:
				case Cci.OperationCode.Conv_Ovf_I2_Un:
				case Cci.OperationCode.Conv_Ovf_I4_Un:
				case Cci.OperationCode.Conv_Ovf_I8_Un:
				case Cci.OperationCode.Conv_Ovf_I_Un:
				case Cci.OperationCode.Conv_Ovf_U1_Un:
				case Cci.OperationCode.Conv_Ovf_U2_Un:
				case Cci.OperationCode.Conv_Ovf_U4_Un:
				case Cci.OperationCode.Conv_Ovf_U8_Un:
				case Cci.OperationCode.Conv_Ovf_U_Un:
				case Cci.OperationCode.Conv_R_Un:
				case Cci.OperationCode.Div_Un:
				case Cci.OperationCode.Mul_Ovf_Un:
				case Cci.OperationCode.Rem_Un:
				case Cci.OperationCode.Shr_Un:
				case Cci.OperationCode.Sub_Ovf_Un:	return true;

				default:							return false;
			}
		}

		public static bool PerformsOverflowCheck(Cci.OperationCode opcode)
		{
			switch (opcode)
			{
				case Cci.OperationCode.Add_Ovf:
				case Cci.OperationCode.Add_Ovf_Un:
				case Cci.OperationCode.Conv_Ovf_I:
				case Cci.OperationCode.Conv_Ovf_I1:
				case Cci.OperationCode.Conv_Ovf_I1_Un:
				case Cci.OperationCode.Conv_Ovf_I2:
				case Cci.OperationCode.Conv_Ovf_I2_Un:
				case Cci.OperationCode.Conv_Ovf_I4:
				case Cci.OperationCode.Conv_Ovf_I4_Un:
				case Cci.OperationCode.Conv_Ovf_I8:
				case Cci.OperationCode.Conv_Ovf_I8_Un:
				case Cci.OperationCode.Conv_Ovf_I_Un:
				case Cci.OperationCode.Conv_Ovf_U:
				case Cci.OperationCode.Conv_Ovf_U1:
				case Cci.OperationCode.Conv_Ovf_U1_Un:
				case Cci.OperationCode.Conv_Ovf_U2:
				case Cci.OperationCode.Conv_Ovf_U2_Un:
				case Cci.OperationCode.Conv_Ovf_U4:
				case Cci.OperationCode.Conv_Ovf_U4_Un:
				case Cci.OperationCode.Conv_Ovf_U8:
				case Cci.OperationCode.Conv_Ovf_U8_Un:
				case Cci.OperationCode.Conv_Ovf_U_Un:
				case Cci.OperationCode.Mul_Ovf:
				case Cci.OperationCode.Mul_Ovf_Un:
				case Cci.OperationCode.Sub_Ovf:
				case Cci.OperationCode.Sub_Ovf_Un:	return true;

				default:							return false;
			}
		}

		public static bool CreateArrayWithLowerBounds(Cci.OperationCode opcode)
		{
			var result = opcode == Cci.OperationCode.Array_Create_WithLowerBound;
			return result;
		}
	}
}
