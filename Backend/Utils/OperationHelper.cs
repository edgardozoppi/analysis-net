using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.ThreeAddressCode.Instructions;
using Microsoft.Cci;

namespace Backend.Utils
{
	public static class OperationHelper
	{
		public static ConvertOperation ToConvertOperation(OperationCode opcode)
		{
			switch (opcode)
			{
				case OperationCode.Castclass:
				case OperationCode.Isinst:		return ConvertOperation.Cast;
				case OperationCode.Box:			return ConvertOperation.Box;
				case OperationCode.Unbox:
				case OperationCode.Unbox_Any:	return ConvertOperation.Unbox;
				case OperationCode.Conv_I:
				case OperationCode.Conv_Ovf_I:
				case OperationCode.Conv_Ovf_I_Un:
				case OperationCode.Conv_I1:
				case OperationCode.Conv_Ovf_I1:
				case OperationCode.Conv_Ovf_I1_Un:
				case OperationCode.Conv_I2:
				case OperationCode.Conv_Ovf_I2:
				case OperationCode.Conv_Ovf_I2_Un:
				case OperationCode.Conv_I4:
				case OperationCode.Conv_Ovf_I4:
				case OperationCode.Conv_Ovf_I4_Un:
				case OperationCode.Conv_I8:
				case OperationCode.Conv_Ovf_I8:
				case OperationCode.Conv_Ovf_I8_Un:
				case OperationCode.Conv_U:
				case OperationCode.Conv_Ovf_U:
				case OperationCode.Conv_Ovf_U_Un:
				case OperationCode.Conv_U1:
				case OperationCode.Conv_Ovf_U1:
				case OperationCode.Conv_Ovf_U1_Un:
				case OperationCode.Conv_U2:
				case OperationCode.Conv_Ovf_U2:
				case OperationCode.Conv_Ovf_U2_Un:
				case OperationCode.Conv_U4:
				case OperationCode.Conv_Ovf_U4:
				case OperationCode.Conv_Ovf_U4_Un:
				case OperationCode.Conv_U8:
				case OperationCode.Conv_Ovf_U8:
				case OperationCode.Conv_Ovf_U8_Un:
				case OperationCode.Conv_R4:
				case OperationCode.Conv_R8:
				case OperationCode.Conv_R_Un:	return ConvertOperation.Conv;

				default: throw new UnknownBytecodeException(opcode);
			}
		}

		public static BranchOperation ToBranchOperation(OperationCode opcode)
		{
			switch (opcode)
			{
				case OperationCode.Beq:
				case OperationCode.Beq_S:	 return BranchOperation.Eq;
				case OperationCode.Bne_Un:
				case OperationCode.Bne_Un_S: return BranchOperation.Neq;
				case OperationCode.Bge:
				case OperationCode.Bge_S:
				case OperationCode.Bge_Un:
				case OperationCode.Bge_Un_S: return BranchOperation.Ge;
				case OperationCode.Bgt:
				case OperationCode.Bgt_S:
				case OperationCode.Bgt_Un:
				case OperationCode.Bgt_Un_S: return BranchOperation.Gt;
				case OperationCode.Ble:
				case OperationCode.Ble_S:
				case OperationCode.Ble_Un:
				case OperationCode.Ble_Un_S: return BranchOperation.Le;
				case OperationCode.Blt:
				case OperationCode.Blt_S:
				case OperationCode.Blt_Un:
				case OperationCode.Blt_Un_S: return BranchOperation.Lt;

				default: throw new UnknownBytecodeException(opcode);
			}
		}

		public static UnaryOperation ToUnaryOperation(OperationCode opcode)
		{
			switch (opcode)
			{
				case OperationCode.Neg: return UnaryOperation.Neg;
				case OperationCode.Not: return UnaryOperation.Not;

				default: throw new UnknownBytecodeException(opcode);
			}
		}

		public static BinaryOperation ToBinaryOperation(OperationCode opcode)
		{
			switch (opcode)
			{
				case OperationCode.Add:
				case OperationCode.Add_Ovf:
				case OperationCode.Add_Ovf_Un:	return BinaryOperation.Add;
				case OperationCode.And:			return BinaryOperation.And;
				case OperationCode.Ceq:			return BinaryOperation.Eq;
				case OperationCode.Cgt:
				case OperationCode.Cgt_Un:		return BinaryOperation.Gt;
				case OperationCode.Clt:
				case OperationCode.Clt_Un:		return BinaryOperation.Lt;
				case OperationCode.Div:
				case OperationCode.Div_Un:		return BinaryOperation.Div;
				case OperationCode.Mul:
				case OperationCode.Mul_Ovf:
				case OperationCode.Mul_Ovf_Un:	return BinaryOperation.Mul;
				case OperationCode.Or:			return BinaryOperation.Or;
				case OperationCode.Rem:
				case OperationCode.Rem_Un:		return BinaryOperation.Rem;
				case OperationCode.Shl:			return BinaryOperation.Shl;
				case OperationCode.Shr:
				case OperationCode.Shr_Un:		return BinaryOperation.Shr;
				case OperationCode.Sub:
				case OperationCode.Sub_Ovf:
				case OperationCode.Sub_Ovf_Un:	return BinaryOperation.Sub;
				case OperationCode.Xor:			return BinaryOperation.Xor;

				default: throw new UnknownBytecodeException(opcode);
			}
		}

		public static ITypeReference GetOperationType(OperationCode opcode)
		{
			switch (opcode)
			{
				case OperationCode.Ldind_I:
				case OperationCode.Stind_I:
				case OperationCode.Stelem_I:
				case OperationCode.Conv_I:
				case OperationCode.Conv_Ovf_I:
				case OperationCode.Conv_Ovf_I_Un:	return Types.Instance.PlatformType.SystemIntPtr;
				case OperationCode.Ldind_I1:
				case OperationCode.Stind_I1:
				case OperationCode.Stelem_I1:
				case OperationCode.Conv_I1:
				case OperationCode.Conv_Ovf_I1:
				case OperationCode.Conv_Ovf_I1_Un:	return Types.Instance.PlatformType.SystemInt8;
				case OperationCode.Ldind_I2:
				case OperationCode.Stind_I2:
				case OperationCode.Stelem_I2:
				case OperationCode.Conv_I2:
				case OperationCode.Conv_Ovf_I2:
				case OperationCode.Conv_Ovf_I2_Un:	return Types.Instance.PlatformType.SystemInt16;
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
				case OperationCode.Ldind_I4:
				case OperationCode.Stind_I4:
				case OperationCode.Stelem_I4:
				case OperationCode.Conv_I4:
				case OperationCode.Conv_Ovf_I4:
				case OperationCode.Conv_Ovf_I4_Un:	return Types.Instance.PlatformType.SystemInt32;
				case OperationCode.Ldc_I8:
				case OperationCode.Ldind_I8:
				case OperationCode.Stind_I8:
				case OperationCode.Stelem_I8:
				case OperationCode.Conv_I8:
				case OperationCode.Conv_Ovf_I8:
				case OperationCode.Conv_Ovf_I8_Un:	return Types.Instance.PlatformType.SystemInt64;
				case OperationCode.Conv_U:
				case OperationCode.Conv_Ovf_U:
				case OperationCode.Conv_Ovf_U_Un:
				case OperationCode.Ldlen:			return Types.Instance.PlatformType.SystemUIntPtr;
				case OperationCode.Ldind_U1:
				case OperationCode.Conv_U1:
				case OperationCode.Conv_Ovf_U1:
				case OperationCode.Conv_Ovf_U1_Un:	return Types.Instance.PlatformType.SystemUInt8;
				case OperationCode.Ldind_U2:
				case OperationCode.Conv_U2:
				case OperationCode.Conv_Ovf_U2:
				case OperationCode.Conv_Ovf_U2_Un:	return Types.Instance.PlatformType.SystemUInt16;
				case OperationCode.Ldind_U4:
				case OperationCode.Conv_U4:
				case OperationCode.Conv_Ovf_U4:
				case OperationCode.Conv_Ovf_U4_Un:
				case OperationCode.Sizeof:			return Types.Instance.PlatformType.SystemUInt32;
				case OperationCode.Conv_U8:
				case OperationCode.Conv_Ovf_U8:
				case OperationCode.Conv_Ovf_U8_Un:	return Types.Instance.PlatformType.SystemUInt64;
				case OperationCode.Ldc_R4:
				case OperationCode.Ldind_R4:
				case OperationCode.Stind_R4:
				case OperationCode.Stelem_R4:
				case OperationCode.Conv_R4:			return Types.Instance.PlatformType.SystemFloat32;
				case OperationCode.Ldc_R8:
				case OperationCode.Ldind_R8:
				case OperationCode.Stind_R8:
				case OperationCode.Stelem_R8:
				case OperationCode.Conv_R8:
				case OperationCode.Conv_R_Un:		return Types.Instance.PlatformType.SystemFloat64;
				case OperationCode.Ldnull:			return Types.Instance.PlatformType.SystemObject;
				case OperationCode.Ldstr:			return Types.Instance.PlatformType.SystemString;

				default: return null;
			}
		}

		public static bool GetUnaryConditionalBranchValue(OperationCode opcode)
		{
			switch (opcode)
			{
				case OperationCode.Brfalse:
				case OperationCode.Brfalse_S: return false;
				case OperationCode.Brtrue:
				case OperationCode.Brtrue_S:  return true;

				default: throw new UnknownBytecodeException(opcode);
			}
		}

		public static bool OperandsAreUnsigned(OperationCode opcode)
		{
			switch (opcode)
			{
				case OperationCode.Add_Ovf_Un:
				case OperationCode.Bge_Un:
				case OperationCode.Bge_Un_S:
				case OperationCode.Bgt_Un:
				case OperationCode.Bgt_Un_S:
				case OperationCode.Ble_Un:
				case OperationCode.Ble_Un_S:
				case OperationCode.Blt_Un:
				case OperationCode.Blt_Un_S:
				case OperationCode.Bne_Un:
				case OperationCode.Bne_Un_S:
				case OperationCode.Cgt_Un:
				case OperationCode.Clt_Un:
				case OperationCode.Conv_Ovf_I1_Un:
				case OperationCode.Conv_Ovf_I2_Un:
				case OperationCode.Conv_Ovf_I4_Un:
				case OperationCode.Conv_Ovf_I8_Un:
				case OperationCode.Conv_Ovf_I_Un:
				case OperationCode.Conv_Ovf_U1_Un:
				case OperationCode.Conv_Ovf_U2_Un:
				case OperationCode.Conv_Ovf_U4_Un:
				case OperationCode.Conv_Ovf_U8_Un:
				case OperationCode.Conv_Ovf_U_Un:
				case OperationCode.Conv_R_Un:
				case OperationCode.Div_Un:
				case OperationCode.Mul_Ovf_Un:
				case OperationCode.Rem_Un:
				case OperationCode.Shr_Un:
				case OperationCode.Sub_Ovf_Un:	return true;

				default:						return false;
			}
		}

		public static bool PerformsOverflowCheck(OperationCode opcode)
		{
			switch (opcode)
			{
				case OperationCode.Add_Ovf:
				case OperationCode.Add_Ovf_Un:
				case OperationCode.Conv_Ovf_I:
				case OperationCode.Conv_Ovf_I1:
				case OperationCode.Conv_Ovf_I1_Un:
				case OperationCode.Conv_Ovf_I2:
				case OperationCode.Conv_Ovf_I2_Un:
				case OperationCode.Conv_Ovf_I4:
				case OperationCode.Conv_Ovf_I4_Un:
				case OperationCode.Conv_Ovf_I8:
				case OperationCode.Conv_Ovf_I8_Un:
				case OperationCode.Conv_Ovf_I_Un:
				case OperationCode.Conv_Ovf_U:
				case OperationCode.Conv_Ovf_U1:
				case OperationCode.Conv_Ovf_U1_Un:
				case OperationCode.Conv_Ovf_U2:
				case OperationCode.Conv_Ovf_U2_Un:
				case OperationCode.Conv_Ovf_U4:
				case OperationCode.Conv_Ovf_U4_Un:
				case OperationCode.Conv_Ovf_U8:
				case OperationCode.Conv_Ovf_U8_Un:
				case OperationCode.Conv_Ovf_U_Un:
				case OperationCode.Mul_Ovf:
				case OperationCode.Mul_Ovf_Un:
				case OperationCode.Sub_Ovf:
				case OperationCode.Sub_Ovf_Un:	return true;

				default:						return false;
			}
		}

		public static bool CanFallThroughNextOperation(OperationCode opcode)
		{
			switch (opcode)
			{
				case OperationCode.Ret:
				case OperationCode.Endfinally:
				case OperationCode.Endfilter:
				case OperationCode.Throw:
				case OperationCode.Rethrow:
				case OperationCode.Br:
				case OperationCode.Br_S:
				case OperationCode.Leave:
				case OperationCode.Leave_S: return false;

				default:					return true;
			}
		}

		public static bool IsBranch(OperationCode opcode)
		{
			switch (opcode)
			{
				case OperationCode.Br:
				case OperationCode.Br_S:
				case OperationCode.Leave:
				case OperationCode.Leave_S:
				case OperationCode.Beq:
				case OperationCode.Beq_S:
				case OperationCode.Bne_Un:
				case OperationCode.Bne_Un_S:
				case OperationCode.Bge:
				case OperationCode.Bge_S:
				case OperationCode.Bge_Un:
				case OperationCode.Bge_Un_S:
				case OperationCode.Bgt:
				case OperationCode.Bgt_S:
				case OperationCode.Bgt_Un:
				case OperationCode.Bgt_Un_S:
				case OperationCode.Ble:
				case OperationCode.Ble_S:
				case OperationCode.Ble_Un:
				case OperationCode.Ble_Un_S:
				case OperationCode.Blt:
				case OperationCode.Blt_S:
				case OperationCode.Blt_Un:
				case OperationCode.Blt_Un_S:
				case OperationCode.Brfalse:
				case OperationCode.Brfalse_S:
				case OperationCode.Brtrue:
				case OperationCode.Brtrue_S:
				case OperationCode.Switch:	return true;

				default:					return false;
			}
		}
	}
}
