// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SRM = System.Reflection.Metadata;
using Model.Bytecode;
using Model.Types;
using Model;

namespace MetadataProvider
{
	internal static class OperationHelper
	{
		public static BasicOperation ToBasicOperation(SRM.ILOpCode opcode)
		{
			switch (opcode)
			{
				case SRM.ILOpCode.Neg:			return BasicOperation.Neg;
				case SRM.ILOpCode.Not:			return BasicOperation.Not;
				case SRM.ILOpCode.Add:
				case SRM.ILOpCode.Add_ovf:
				case SRM.ILOpCode.Add_ovf_un:	return BasicOperation.Add;
				case SRM.ILOpCode.And:			return BasicOperation.And;
				case SRM.ILOpCode.Ceq:			return BasicOperation.Eq;
				case SRM.ILOpCode.Cgt:
				case SRM.ILOpCode.Cgt_un:		return BasicOperation.Gt;
				case SRM.ILOpCode.Clt:
				case SRM.ILOpCode.Clt_un:		return BasicOperation.Lt;
				case SRM.ILOpCode.Div:
				case SRM.ILOpCode.Div_un:		return BasicOperation.Div;
				case SRM.ILOpCode.Mul:
				case SRM.ILOpCode.Mul_ovf:
				case SRM.ILOpCode.Mul_ovf_un:	return BasicOperation.Mul;
				case SRM.ILOpCode.Or:			return BasicOperation.Or;
				case SRM.ILOpCode.Rem:
				case SRM.ILOpCode.Rem_un:		return BasicOperation.Rem;
				case SRM.ILOpCode.Shl:			return BasicOperation.Shl;
				case SRM.ILOpCode.Shr:
				case SRM.ILOpCode.Shr_un:		return BasicOperation.Shr;
				case SRM.ILOpCode.Sub:
				case SRM.ILOpCode.Sub_ovf:
				case SRM.ILOpCode.Sub_ovf_un:	return BasicOperation.Sub;
				case SRM.ILOpCode.Xor:			return BasicOperation.Xor;
				case SRM.ILOpCode.Endfilter:	return BasicOperation.EndFilter;
				case SRM.ILOpCode.Endfinally:	return BasicOperation.EndFinally;
				case SRM.ILOpCode.Throw:		return BasicOperation.Throw;
				case SRM.ILOpCode.Rethrow:		return BasicOperation.Rethrow;
				case SRM.ILOpCode.Nop:			return BasicOperation.Nop;
				case SRM.ILOpCode.Pop:			return BasicOperation.Pop;
				case SRM.ILOpCode.Dup:			return BasicOperation.Dup;
				case SRM.ILOpCode.Localloc:		return BasicOperation.LocalAllocation;
				case SRM.ILOpCode.Initblk:		return BasicOperation.InitBlock;
				case SRM.ILOpCode.Initobj:		return BasicOperation.InitObject;
				case SRM.ILOpCode.Cpblk:		return BasicOperation.CopyBlock;
				case SRM.ILOpCode.Cpobj:		return BasicOperation.CopyObject;
				case SRM.ILOpCode.Ret:			return BasicOperation.Return;
				case SRM.ILOpCode.Ldlen:		return BasicOperation.LoadArrayLength;
				case SRM.ILOpCode.Ldind_i:
				case SRM.ILOpCode.Ldind_i1:
				case SRM.ILOpCode.Ldind_i2:
				case SRM.ILOpCode.Ldind_i4:
				case SRM.ILOpCode.Ldind_i8:
				case SRM.ILOpCode.Ldind_r4:
				case SRM.ILOpCode.Ldind_r8:
				case SRM.ILOpCode.Ldind_ref:
				case SRM.ILOpCode.Ldind_u1:
				case SRM.ILOpCode.Ldind_u2:
				case SRM.ILOpCode.Ldind_u4:
				case SRM.ILOpCode.Ldobj:		return BasicOperation.IndirectLoad;
				case SRM.ILOpCode.Ldelem:
				case SRM.ILOpCode.Ldelem_i:
				case SRM.ILOpCode.Ldelem_i1:
				case SRM.ILOpCode.Ldelem_i2:
				case SRM.ILOpCode.Ldelem_i4:
				case SRM.ILOpCode.Ldelem_i8:
				case SRM.ILOpCode.Ldelem_r4:
				case SRM.ILOpCode.Ldelem_r8:
				case SRM.ILOpCode.Ldelem_u1:
				case SRM.ILOpCode.Ldelem_u2:
				case SRM.ILOpCode.Ldelem_u4:
				case SRM.ILOpCode.Ldelem_ref:	return BasicOperation.LoadArrayElement;
				case SRM.ILOpCode.Ldelema:		return BasicOperation.LoadArrayElementAddress;
				case SRM.ILOpCode.Stind_i:
				case SRM.ILOpCode.Stind_i1:
				case SRM.ILOpCode.Stind_i2:
				case SRM.ILOpCode.Stind_i4:
				case SRM.ILOpCode.Stind_i8:
				case SRM.ILOpCode.Stind_r4:
				case SRM.ILOpCode.Stind_r8:
				case SRM.ILOpCode.Stind_ref:
				case SRM.ILOpCode.Stobj:		return BasicOperation.IndirectStore;
				case SRM.ILOpCode.Stelem:
				case SRM.ILOpCode.Stelem_i:
				case SRM.ILOpCode.Stelem_i1:
				case SRM.ILOpCode.Stelem_i2:
				case SRM.ILOpCode.Stelem_i4:
				case SRM.ILOpCode.Stelem_i8:
				case SRM.ILOpCode.Stelem_r4:
				case SRM.ILOpCode.Stelem_r8:
				case SRM.ILOpCode.Stelem_ref:	return BasicOperation.StoreArrayElement;
				case SRM.ILOpCode.Break:		return BasicOperation.Breakpoint;
				
				default: throw opcode.ToUnknownValueException();
			}
		}

		public static ConvertOperation ToConvertOperation(SRM.ILOpCode opcode)
		{
			switch (opcode)
			{
				case SRM.ILOpCode.Castclass:
				case SRM.ILOpCode.Isinst:		 return ConvertOperation.Cast;
				case SRM.ILOpCode.Box:			 return ConvertOperation.Box;
				case SRM.ILOpCode.Unbox:		 return ConvertOperation.UnboxPtr;
				case SRM.ILOpCode.Unbox_any:	 return ConvertOperation.Unbox;
				case SRM.ILOpCode.Conv_i:
				case SRM.ILOpCode.Conv_ovf_i:
				case SRM.ILOpCode.Conv_ovf_i_un:
				case SRM.ILOpCode.Conv_i1:
				case SRM.ILOpCode.Conv_ovf_i1:
				case SRM.ILOpCode.Conv_ovf_i1_un:
				case SRM.ILOpCode.Conv_i2:
				case SRM.ILOpCode.Conv_ovf_i2:
				case SRM.ILOpCode.Conv_ovf_i2_un:
				case SRM.ILOpCode.Conv_i4:
				case SRM.ILOpCode.Conv_ovf_i4:
				case SRM.ILOpCode.Conv_ovf_i4_un:
				case SRM.ILOpCode.Conv_i8:
				case SRM.ILOpCode.Conv_ovf_i8:
				case SRM.ILOpCode.Conv_ovf_i8_un:
				case SRM.ILOpCode.Conv_u:
				case SRM.ILOpCode.Conv_ovf_u:
				case SRM.ILOpCode.Conv_ovf_u_un:
				case SRM.ILOpCode.Conv_u1:
				case SRM.ILOpCode.Conv_ovf_u1:
				case SRM.ILOpCode.Conv_ovf_u1_un:
				case SRM.ILOpCode.Conv_u2:
				case SRM.ILOpCode.Conv_ovf_u2:
				case SRM.ILOpCode.Conv_ovf_u2_un:
				case SRM.ILOpCode.Conv_u4:
				case SRM.ILOpCode.Conv_ovf_u4:
				case SRM.ILOpCode.Conv_ovf_u4_un:
				case SRM.ILOpCode.Conv_u8:
				case SRM.ILOpCode.Conv_ovf_u8:
				case SRM.ILOpCode.Conv_ovf_u8_un:
				case SRM.ILOpCode.Conv_r4:
				case SRM.ILOpCode.Conv_r8:
				case SRM.ILOpCode.Conv_r_un:	 return ConvertOperation.Conv;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static BranchOperation ToBranchOperation(SRM.ILOpCode opcode)
		{
			switch (opcode)
			{
				case SRM.ILOpCode.Brfalse:
				case SRM.ILOpCode.Brfalse_s: return BranchOperation.False;
				case SRM.ILOpCode.Brtrue:
				case SRM.ILOpCode.Brtrue_s:	 return BranchOperation.True;
				case SRM.ILOpCode.Beq:
				case SRM.ILOpCode.Beq_s:	 return BranchOperation.Eq;
				case SRM.ILOpCode.Bne_un:
				case SRM.ILOpCode.Bne_un_s:	 return BranchOperation.Neq;
				case SRM.ILOpCode.Bge:
				case SRM.ILOpCode.Bge_s:
				case SRM.ILOpCode.Bge_un:
				case SRM.ILOpCode.Bge_un_s:	 return BranchOperation.Ge;
				case SRM.ILOpCode.Bgt:
				case SRM.ILOpCode.Bgt_s:
				case SRM.ILOpCode.Bgt_un:
				case SRM.ILOpCode.Bgt_un_s:	 return BranchOperation.Gt;
				case SRM.ILOpCode.Ble:
				case SRM.ILOpCode.Ble_s:
				case SRM.ILOpCode.Ble_un:
				case SRM.ILOpCode.Ble_un_s:	 return BranchOperation.Le;
				case SRM.ILOpCode.Blt:
				case SRM.ILOpCode.Blt_s:
				case SRM.ILOpCode.Blt_un:
				case SRM.ILOpCode.Blt_un_s:	 return BranchOperation.Lt;
				case SRM.ILOpCode.Leave:
				case SRM.ILOpCode.Leave_s:	 return BranchOperation.Leave;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static MethodCallOperation ToMethodCallOperation(SRM.ILOpCode opcode)
		{
			switch (opcode)
			{
				case SRM.ILOpCode.Call:		return MethodCallOperation.Static;
				case SRM.ILOpCode.Callvirt:	return MethodCallOperation.Virtual;
				case SRM.ILOpCode.Jmp:		return MethodCallOperation.Jump;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static LoadOperation ToLoadOperation(SRM.ILOpCode opcode)
		{
			switch (opcode)
			{
				case SRM.ILOpCode.Ldc_i4:
				case SRM.ILOpCode.Ldc_i4_0:
				case SRM.ILOpCode.Ldc_i4_1:
				case SRM.ILOpCode.Ldc_i4_2:
				case SRM.ILOpCode.Ldc_i4_3:
				case SRM.ILOpCode.Ldc_i4_4:
				case SRM.ILOpCode.Ldc_i4_5:
				case SRM.ILOpCode.Ldc_i4_6:
				case SRM.ILOpCode.Ldc_i4_7:
				case SRM.ILOpCode.Ldc_i4_8:
				case SRM.ILOpCode.Ldc_i4_m1:
				case SRM.ILOpCode.Ldc_i4_s:
				case SRM.ILOpCode.Ldc_i8:
				case SRM.ILOpCode.Ldc_r4:
				case SRM.ILOpCode.Ldc_r8:
				case SRM.ILOpCode.Ldnull:
				case SRM.ILOpCode.Ldstr:    return LoadOperation.Value;
				case SRM.ILOpCode.Ldarg:
				case SRM.ILOpCode.Ldarg_0:
				case SRM.ILOpCode.Ldarg_1:
				case SRM.ILOpCode.Ldarg_2:
				case SRM.ILOpCode.Ldarg_3:
				case SRM.ILOpCode.Ldarg_s:
				case SRM.ILOpCode.Ldloc:
				case SRM.ILOpCode.Ldloc_0:
				case SRM.ILOpCode.Ldloc_1:
				case SRM.ILOpCode.Ldloc_2:
				case SRM.ILOpCode.Ldloc_3:
				case SRM.ILOpCode.Ldloc_s:  return LoadOperation.Content;
				case SRM.ILOpCode.Ldarga:
				case SRM.ILOpCode.Ldarga_s:
				case SRM.ILOpCode.Ldloca:
				case SRM.ILOpCode.Ldloca_s: return LoadOperation.Address;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static LoadFieldOperation ToLoadFieldOperation(SRM.ILOpCode opcode)
		{
			switch (opcode)
			{
				case SRM.ILOpCode.Ldfld:
				case SRM.ILOpCode.Ldsfld:  return LoadFieldOperation.Content;
				case SRM.ILOpCode.Ldflda:
				case SRM.ILOpCode.Ldsflda: return LoadFieldOperation.Address;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static LoadArrayElementOperation ToLoadArrayElementOperation(string methodName)
		{
			LoadArrayElementOperation operation;

			if (methodName == "Get")
			{
				operation = LoadArrayElementOperation.Content;
			}
			else if (methodName == "Address")
			{
				operation = LoadArrayElementOperation.Address;
			}
			else
			{
				var msg = string.Format("Unknown array operation '{0}'", methodName);
				throw new Exception(msg);
			}

			return operation;
		}

		public static LoadMethodAddressOperation ToLoadMethodAddressOperation(SRM.ILOpCode opcode)
		{
			switch (opcode)
			{
				case SRM.ILOpCode.Ldftn:	 return LoadMethodAddressOperation.Static;
				case SRM.ILOpCode.Ldvirtftn: return LoadMethodAddressOperation.Virtual;

				default: throw opcode.ToUnknownValueException();
			}
		}

		public static IType GetOperationType(SRM.ILOpCode opcode)
		{
			switch (opcode)
			{
				case SRM.ILOpCode.Ldind_i:
				case SRM.ILOpCode.Stind_i:
				case SRM.ILOpCode.Stelem_i:
				case SRM.ILOpCode.Conv_i:
				case SRM.ILOpCode.Conv_ovf_i:
				case SRM.ILOpCode.Conv_ovf_i_un:	return PlatformTypes.IntPtr;
				case SRM.ILOpCode.Ldind_i1:
				case SRM.ILOpCode.Stind_i1:
				case SRM.ILOpCode.Stelem_i1:
				case SRM.ILOpCode.Conv_i1:
				case SRM.ILOpCode.Conv_ovf_i1:
				case SRM.ILOpCode.Conv_ovf_i1_un:	return PlatformTypes.Int8;
				case SRM.ILOpCode.Ldind_i2:
				case SRM.ILOpCode.Stind_i2:
				case SRM.ILOpCode.Stelem_i2:
				case SRM.ILOpCode.Conv_i2:
				case SRM.ILOpCode.Conv_ovf_i2:
				case SRM.ILOpCode.Conv_ovf_i2_un:	return PlatformTypes.Int16;
				case SRM.ILOpCode.Ldc_i4:
				case SRM.ILOpCode.Ldc_i4_0:
				case SRM.ILOpCode.Ldc_i4_1:
				case SRM.ILOpCode.Ldc_i4_2:
				case SRM.ILOpCode.Ldc_i4_3:
				case SRM.ILOpCode.Ldc_i4_4:
				case SRM.ILOpCode.Ldc_i4_5:
				case SRM.ILOpCode.Ldc_i4_6:
				case SRM.ILOpCode.Ldc_i4_7:
				case SRM.ILOpCode.Ldc_i4_8:
				case SRM.ILOpCode.Ldc_i4_m1:
				case SRM.ILOpCode.Ldc_i4_s:
				case SRM.ILOpCode.Ldind_i4:
				case SRM.ILOpCode.Stind_i4:
				case SRM.ILOpCode.Stelem_i4:
				case SRM.ILOpCode.Conv_i4:
				case SRM.ILOpCode.Conv_ovf_i4:
				case SRM.ILOpCode.Conv_ovf_i4_un:	return PlatformTypes.Int32;
				case SRM.ILOpCode.Ldc_i8:
				case SRM.ILOpCode.Ldind_i8:
				case SRM.ILOpCode.Stind_i8:
				case SRM.ILOpCode.Stelem_i8:
				case SRM.ILOpCode.Conv_i8:
				case SRM.ILOpCode.Conv_ovf_i8:
				case SRM.ILOpCode.Conv_ovf_i8_un:	return PlatformTypes.Int64;
				case SRM.ILOpCode.Conv_u:
				case SRM.ILOpCode.Conv_ovf_u:
				case SRM.ILOpCode.Conv_ovf_u_un:
				case SRM.ILOpCode.Ldlen:			return PlatformTypes.UIntPtr;
				case SRM.ILOpCode.Ldind_u1:
				case SRM.ILOpCode.Conv_u1:
				case SRM.ILOpCode.Conv_ovf_u1:
				case SRM.ILOpCode.Conv_ovf_u1_un:	return PlatformTypes.UInt8;
				case SRM.ILOpCode.Ldind_u2:
				case SRM.ILOpCode.Conv_u2:
				case SRM.ILOpCode.Conv_ovf_u2:
				case SRM.ILOpCode.Conv_ovf_u2_un:	return PlatformTypes.UInt16;
				case SRM.ILOpCode.Ldind_u4:
				case SRM.ILOpCode.Conv_u4:
				case SRM.ILOpCode.Conv_ovf_u4:
				case SRM.ILOpCode.Conv_ovf_u4_un:
				case SRM.ILOpCode.Sizeof:			return PlatformTypes.UInt32;
				case SRM.ILOpCode.Conv_u8:
				case SRM.ILOpCode.Conv_ovf_u8:
				case SRM.ILOpCode.Conv_ovf_u8_un:	return PlatformTypes.UInt64;
				case SRM.ILOpCode.Ldc_r4:
				case SRM.ILOpCode.Ldind_r4:
				case SRM.ILOpCode.Stind_r4:
				case SRM.ILOpCode.Stelem_r4:
				case SRM.ILOpCode.Conv_r4:			return PlatformTypes.Float32;
				case SRM.ILOpCode.Ldc_r8:
				case SRM.ILOpCode.Ldind_r8:
				case SRM.ILOpCode.Stind_r8:
				case SRM.ILOpCode.Stelem_r8:
				case SRM.ILOpCode.Conv_r8:
				case SRM.ILOpCode.Conv_r_un:		return PlatformTypes.Float64;
				case SRM.ILOpCode.Ldnull:			return PlatformTypes.Object;
				case SRM.ILOpCode.Ldstr:			return PlatformTypes.String;

				default:							return null;
			}
		}

		public static bool OperandsAreUnsigned(SRM.ILOpCode opcode)
		{
			switch (opcode)
			{
				case SRM.ILOpCode.Add_ovf_un:
				case SRM.ILOpCode.Bge_un:
				case SRM.ILOpCode.Bge_un_s:
				case SRM.ILOpCode.Bgt_un:
				case SRM.ILOpCode.Bgt_un_s:
				case SRM.ILOpCode.Ble_un:
				case SRM.ILOpCode.Ble_un_s:
				case SRM.ILOpCode.Blt_un:
				case SRM.ILOpCode.Blt_un_s:
				case SRM.ILOpCode.Bne_un:
				case SRM.ILOpCode.Bne_un_s:
				case SRM.ILOpCode.Cgt_un:
				case SRM.ILOpCode.Clt_un:
				case SRM.ILOpCode.Conv_ovf_i1_un:
				case SRM.ILOpCode.Conv_ovf_i2_un:
				case SRM.ILOpCode.Conv_ovf_i4_un:
				case SRM.ILOpCode.Conv_ovf_i8_un:
				case SRM.ILOpCode.Conv_ovf_i_un:
				case SRM.ILOpCode.Conv_ovf_u1_un:
				case SRM.ILOpCode.Conv_ovf_u2_un:
				case SRM.ILOpCode.Conv_ovf_u4_un:
				case SRM.ILOpCode.Conv_ovf_u8_un:
				case SRM.ILOpCode.Conv_ovf_u_un:
				case SRM.ILOpCode.Conv_r_un:
				case SRM.ILOpCode.Div_un:
				case SRM.ILOpCode.Mul_ovf_un:
				case SRM.ILOpCode.Rem_un:
				case SRM.ILOpCode.Shr_un:
				case SRM.ILOpCode.Sub_ovf_un:		return true;

				default:							return false;
			}
		}

		public static bool PerformsOverflowCheck(SRM.ILOpCode opcode)
		{
			switch (opcode)
			{
				case SRM.ILOpCode.Add_ovf:
				case SRM.ILOpCode.Add_ovf_un:
				case SRM.ILOpCode.Conv_ovf_i:
				case SRM.ILOpCode.Conv_ovf_i1:
				case SRM.ILOpCode.Conv_ovf_i1_un:
				case SRM.ILOpCode.Conv_ovf_i2:
				case SRM.ILOpCode.Conv_ovf_i2_un:
				case SRM.ILOpCode.Conv_ovf_i4:
				case SRM.ILOpCode.Conv_ovf_i4_un:
				case SRM.ILOpCode.Conv_ovf_i8:
				case SRM.ILOpCode.Conv_ovf_i8_un:
				case SRM.ILOpCode.Conv_ovf_i_un:
				case SRM.ILOpCode.Conv_ovf_u:
				case SRM.ILOpCode.Conv_ovf_u1:
				case SRM.ILOpCode.Conv_ovf_u1_un:
				case SRM.ILOpCode.Conv_ovf_u2:
				case SRM.ILOpCode.Conv_ovf_u2_un:
				case SRM.ILOpCode.Conv_ovf_u4:
				case SRM.ILOpCode.Conv_ovf_u4_un:
				case SRM.ILOpCode.Conv_ovf_u8:
				case SRM.ILOpCode.Conv_ovf_u8_un:
				case SRM.ILOpCode.Conv_ovf_u_un:
				case SRM.ILOpCode.Mul_ovf:
				case SRM.ILOpCode.Mul_ovf_un:
				case SRM.ILOpCode.Sub_ovf:
				case SRM.ILOpCode.Sub_ovf_un:		return true;

				default:							return false;
			}
		}
	}
}
