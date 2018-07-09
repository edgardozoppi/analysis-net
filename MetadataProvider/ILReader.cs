using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using SRM = System.Reflection.Metadata;

namespace MetadataProvider
{
	internal enum OperandType
	{
		None,
		Immediate,
		MemberReference,
		TypeReference,
		TypeDefinition,
		TypeSpecification,
		MethodDefinition,
		MethodSpecification,
		FieldDefinition,
		//ParameterDefinition,
		UserString
	}

	internal struct ILInstruction
	{
		public uint Offset;
		public SRM.ILOpCode Opcode;
		public OperandType OperandType;
		public object Operand;

		public ILInstruction(uint offset, SRM.ILOpCode opcode, OperandType operandType, object operand)
		{
			this.Offset = offset;
			this.Opcode = opcode;
			this.OperandType = operandType;
			this.Operand = operand;
		}

		public override string ToString()
		{
			return string.Format("L_{0:X4}  {1} {2}", this.Offset, this.Opcode, this.Operand);
		}
	}

	internal class ILReader
	{
		private enum ILTokenType
		{
			Module = 0x00,
			TypeReference = 0x01,
			TypeDefinition = 0x02,
			FieldDefinition = 0x04,
			MethodDefinition = 0x06,
			ParameterDefinition = 0x08,
			InterfaceImplementation = 0x09,
			MemberReference = 0x0A,
			CustomAttribute = 0x0C,
			Permission = 0x0E,
			Signature = 0x11,
			Event = 0x14,
			Property = 0x17,
			ModuleReference = 0x1A,
			TypeSpecification = 0x1B,
			Assembly = 0x20,
			AssemblyReference = 0x23,
			File = 0x26,
			ExportedType = 0x27,
			ManifestResource = 0x28,
			GenericParameter = 0x2A,
			MethodSpecification = 0x2B,
			GenericParameterConstraint = 0x2C,
			UserString = 0x70,
		}

		private SRM.BlobReader reader;
		
		public ILReader(SRM.MethodBodyBlock bodyBlock)
		{
			this.reader = bodyBlock.GetILReader();
		}

		public IEnumerable<ILInstruction> ReadInstructions()
		{
			reader.Reset();

			while (reader.RemainingBytes > 0)
			{
				var instruction = ReadInstruction();
				yield return instruction;
			}
		}

		public ILInstruction ReadInstruction()
		{
			const int C_i4_M1 = -1;
			const int C_i4_0 = 0;
			const int C_i4_1 = 1;
			const int C_i4_2 = 2;
			const int C_i4_3 = 3;
			const int C_i4_4 = 4;
			const int C_i4_5 = 5;
			const int C_i4_6 = 6;
			const int C_i4_7 = 7;
			const int C_i4_8 = 8;

			var maxOffset = (uint)(reader.Offset + reader.Length);
			var offset = (uint)reader.Offset;
			ushort value = reader.ReadByte();

			if (value == 0xFE)
			{
				value = (ushort)(reader.ReadByte() | 0xFE00);
			}

			var opcode = (SRM.ILOpCode)value;
			var operandType = OperandType.None;
			object operand = null;

			switch (opcode)
			{
				case SRM.ILOpCode.Nop:
				case SRM.ILOpCode.Break:
					break;

				case SRM.ILOpCode.Ldarg_0:
				case SRM.ILOpCode.Ldarg_1:
				case SRM.ILOpCode.Ldarg_2:
				case SRM.ILOpCode.Ldarg_3:
					operandType = OperandType.Immediate;
					operand = (int)(value - SRM.ILOpCode.Ldarg_0);
					//operand = GetParameter((uint)(value - SRM.ILOpCode.Ldarg_0));
					break;

				case SRM.ILOpCode.Ldloc_0:
				case SRM.ILOpCode.Ldloc_1:
				case SRM.ILOpCode.Ldloc_2:
				case SRM.ILOpCode.Ldloc_3:
					operandType = OperandType.Immediate;
					operand = (int)(value - SRM.ILOpCode.Ldloc_0);
					//operand = GetLocal((uint)(value - SRM.ILOpCode.Ldloc_0));
					break;

				case SRM.ILOpCode.Stloc_0:
				case SRM.ILOpCode.Stloc_1:
				case SRM.ILOpCode.Stloc_2:
				case SRM.ILOpCode.Stloc_3:
					operandType = OperandType.Immediate;
					operand = (int)(value - SRM.ILOpCode.Stloc_0);
					//operand = GetLocal((uint)(value - SRM.ILOpCode.Stloc_0));
					break;

				case SRM.ILOpCode.Ldarg_s:
				case SRM.ILOpCode.Ldarga_s:
				case SRM.ILOpCode.Starg_s:
					operandType = OperandType.Immediate;
					operand = (int)reader.ReadByte();
					//operand = GetParameter(reader.ReadByte());
					break;

				case SRM.ILOpCode.Ldloc_s:
				case SRM.ILOpCode.Ldloca_s:
				case SRM.ILOpCode.Stloc_s:
					operandType = OperandType.Immediate;
					operand = (int)reader.ReadByte();
					//operand = GetLocal(reader.ReadByte());
					break;

				case SRM.ILOpCode.Ldnull:
					break;

				case SRM.ILOpCode.Ldc_i4_m1:
					operandType = OperandType.Immediate;
					operand = C_i4_M1;
					break;

				case SRM.ILOpCode.Ldc_i4_0:
					operandType = OperandType.Immediate;
					operand = C_i4_0;
					break;

				case SRM.ILOpCode.Ldc_i4_1:
					operandType = OperandType.Immediate;
					operand = C_i4_1;
					break;

				case SRM.ILOpCode.Ldc_i4_2:
					operandType = OperandType.Immediate;
					operand = C_i4_2;
					break;

				case SRM.ILOpCode.Ldc_i4_3:
					operandType = OperandType.Immediate;
					operand = C_i4_3;
					break;

				case SRM.ILOpCode.Ldc_i4_4:
					operandType = OperandType.Immediate;
					operand = C_i4_4;
					break;

				case SRM.ILOpCode.Ldc_i4_5:
					operandType = OperandType.Immediate;
					operand = C_i4_5;
					break;

				case SRM.ILOpCode.Ldc_i4_6:
					operandType = OperandType.Immediate;
					operand = C_i4_6;
					break;

				case SRM.ILOpCode.Ldc_i4_7:
					operandType = OperandType.Immediate;
					operand = C_i4_7;
					break;

				case SRM.ILOpCode.Ldc_i4_8:
					operandType = OperandType.Immediate;
					operand = C_i4_8;
					break;

				case SRM.ILOpCode.Ldc_i4_s:
					operandType = OperandType.Immediate;
					operand = (int)reader.ReadSByte();
					break;

				case SRM.ILOpCode.Ldc_i4:
					operandType = OperandType.Immediate;
					operand = reader.ReadInt32();
					break;

				case SRM.ILOpCode.Ldc_i8:
					operandType = OperandType.Immediate;
					operand = reader.ReadInt64();
					break;

				case SRM.ILOpCode.Ldc_r4:
					operandType = OperandType.Immediate;
					operand = reader.ReadSingle();
					break;

				case SRM.ILOpCode.Ldc_r8:
					operandType = OperandType.Immediate;
					operand = reader.ReadDouble();
					break;

				case SRM.ILOpCode.Dup:
				case SRM.ILOpCode.Pop:
					break;

				case SRM.ILOpCode.Jmp:
					operand = ReadHandle(ref operandType);
					//operand = GetMethod(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Call:
					{
						operand = ReadHandle(ref operandType);

						//IMethodReference methodReference = GetMethod(reader.ReadUInt32());
						//IArrayTypeReference arrayType = methodReference.ContainingType as IArrayTypeReference;
						//if (arrayType != null)
						//{
						//	// For Get(), Set() and Address() on arrays, the runtime provides method implementations.
						//	// Hence, CCI2 replaces these with pseudo instructions Array_set, Array_Get and Array_Addr.
						//	// All other methods on arrays will not use pseudo instruction and will have methodReference as their operand. 
						//	if (methodReference.Name.UniqueKey == PEFileToObjectModel.NameTable.Set.UniqueKey)
						//	{
						//		value = SRM.ILOpCode.Array_set;
						//		operand = arrayType;
						//	}
						//	else if (methodReference.Name.UniqueKey == PEFileToObjectModel.NameTable.Get.UniqueKey)
						//	{
						//		value = SRM.ILOpCode.Array_Get;
						//		operand = arrayType;
						//	}
						//	else if (methodReference.Name.UniqueKey == PEFileToObjectModel.NameTable.Address.UniqueKey)
						//	{
						//		value = SRM.ILOpCode.Array_Addr;
						//		operand = arrayType;
						//	}
						//	else
						//	{
						//		operand = methodReference;
						//	}
						//}
						//else
						//{
						//	operand = methodReference;
						//}
					}
					break;

				case SRM.ILOpCode.Calli:
					operand = ReadHandle(ref operandType);
					//operand = GetFunctionPointerType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Ret:
					break;

				case SRM.ILOpCode.Br_s:
				case SRM.ILOpCode.Brfalse_s:
				case SRM.ILOpCode.Brtrue_s:
				case SRM.ILOpCode.Beq_s:
				case SRM.ILOpCode.Bge_s:
				case SRM.ILOpCode.Bgt_s:
				case SRM.ILOpCode.Ble_s:
				case SRM.ILOpCode.Blt_s:
				case SRM.ILOpCode.Bne_un_s:
				case SRM.ILOpCode.Bge_un_s:
				case SRM.ILOpCode.Bgt_un_s:
				case SRM.ILOpCode.Ble_un_s:
				case SRM.ILOpCode.Blt_un_s:
					{
						var jumpOffset = (uint)(reader.Offset + 1 + reader.ReadSByte());
						if (jumpOffset >= maxOffset)
						{
							//  Error...
						}
						operandType = OperandType.Immediate;
						operand = jumpOffset;
					}
					break;

				case SRM.ILOpCode.Br:
				case SRM.ILOpCode.Brfalse:
				case SRM.ILOpCode.Brtrue:
				case SRM.ILOpCode.Beq:
				case SRM.ILOpCode.Bge:
				case SRM.ILOpCode.Bgt:
				case SRM.ILOpCode.Ble:
				case SRM.ILOpCode.Blt:
				case SRM.ILOpCode.Bne_un:
				case SRM.ILOpCode.Bge_un:
				case SRM.ILOpCode.Bgt_un:
				case SRM.ILOpCode.Ble_un:
				case SRM.ILOpCode.Blt_un:
					{
						var jumpOffset = (uint)(reader.Offset + 4 + reader.ReadInt32());
						if (jumpOffset >= maxOffset)
						{
							//  Error...
						}
						operandType = OperandType.Immediate;
						operand = jumpOffset;
					}
					break;

				case SRM.ILOpCode.Switch:
					{
						var numTargets = reader.ReadUInt32();
						var targets = new uint[numTargets];
						var asOffset = (uint)reader.Offset + numTargets * 4;
						for (var i = 0; i < numTargets; i++)
						{
							var targetAddress = reader.ReadUInt32() + asOffset;
							if (targetAddress >= maxOffset)
							{
								//  Error...
							}
							targets[i] = targetAddress;
						}
						operandType = OperandType.Immediate;
						operand = targets;
					}
					break;

				case SRM.ILOpCode.Ldind_i1:
				case SRM.ILOpCode.Ldind_u1:
				case SRM.ILOpCode.Ldind_i2:
				case SRM.ILOpCode.Ldind_u2:
				case SRM.ILOpCode.Ldind_i4:
				case SRM.ILOpCode.Ldind_u4:
				case SRM.ILOpCode.Ldind_i8:
				case SRM.ILOpCode.Ldind_i:
				case SRM.ILOpCode.Ldind_r4:
				case SRM.ILOpCode.Ldind_r8:
				case SRM.ILOpCode.Ldind_ref:
				case SRM.ILOpCode.Stind_ref:
				case SRM.ILOpCode.Stind_i1:
				case SRM.ILOpCode.Stind_i2:
				case SRM.ILOpCode.Stind_i4:
				case SRM.ILOpCode.Stind_i8:
				case SRM.ILOpCode.Stind_r4:
				case SRM.ILOpCode.Stind_r8:
				case SRM.ILOpCode.Add:
				case SRM.ILOpCode.Sub:
				case SRM.ILOpCode.Mul:
				case SRM.ILOpCode.Div:
				case SRM.ILOpCode.Div_un:
				case SRM.ILOpCode.Rem:
				case SRM.ILOpCode.Rem_un:
				case SRM.ILOpCode.And:
				case SRM.ILOpCode.Or:
				case SRM.ILOpCode.Xor:
				case SRM.ILOpCode.Shl:
				case SRM.ILOpCode.Shr:
				case SRM.ILOpCode.Shr_un:
				case SRM.ILOpCode.Neg:
				case SRM.ILOpCode.Not:
				case SRM.ILOpCode.Conv_i1:
				case SRM.ILOpCode.Conv_i2:
				case SRM.ILOpCode.Conv_i4:
				case SRM.ILOpCode.Conv_i8:
				case SRM.ILOpCode.Conv_r4:
				case SRM.ILOpCode.Conv_r8:
				case SRM.ILOpCode.Conv_u4:
				case SRM.ILOpCode.Conv_u8:
					break;

				case SRM.ILOpCode.Callvirt:
					{
						operand = ReadHandle(ref operandType);

						//IMethodReference methodReference = GetMethod(reader.ReadUInt32());
						//IArrayTypeReference arrayType = methodReference.ContainingType as IArrayTypeReference;
						//if (arrayType != null)
						//{
						//	// For Get(), Set() and Address() on arrays, the runtime provides method implementations.
						//	// Hence, CCI2 replaces these with pseudo instructions Array_set, Array_Get and Array_Addr.
						//	// All other methods on arrays will not use pseudo instruction and will have methodReference as their operand. 
						//	if (methodReference.Name.UniqueKey == PEFileToObjectModel.NameTable.Set.UniqueKey)
						//	{
						//		value = SRM.ILOpCode.Array_set;
						//		operand = arrayType;
						//	}
						//	else if (methodReference.Name.UniqueKey == PEFileToObjectModel.NameTable.Get.UniqueKey)
						//	{
						//		value = SRM.ILOpCode.Array_Get;
						//		operand = arrayType;
						//	}
						//	else if (methodReference.Name.UniqueKey == PEFileToObjectModel.NameTable.Address.UniqueKey)
						//	{
						//		value = SRM.ILOpCode.Array_Addr;
						//		operand = arrayType;
						//	}
						//	else
						//	{
						//		operand = methodReference;
						//	}
						//}
						//else
						//{
						//	operand = methodReference;
						//}
					}
					break;

				case SRM.ILOpCode.Cpobj:
				case SRM.ILOpCode.Ldobj:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Ldstr:
					operand = ReadHandle(ref operandType);
					//operand = GetUserStringForToken(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Newobj:
					{
						operand = ReadHandle(ref operandType);

						//IMethodReference methodReference = GetMethod(reader.ReadUInt32());
						//IArrayTypeReference arrayType = methodReference.ContainingType as IArrayTypeReference;
						//if (arrayType != null && !arrayType.IsVector)
						//{
						//	var numParam = methodReference.Parameters.Length;
						//	if (numParam != arrayType.Rank)
						//		value = SRM.ILOpCode.Array_Create_WithLowerBound;
						//	else
						//		value = SRM.ILOpCode.Array_Create;
						//	operand = arrayType;
						//}
						//else
						//{
						//	operand = methodReference;
						//}
					}
					break;

				case SRM.ILOpCode.Castclass:
				case SRM.ILOpCode.Isinst:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Conv_r_un:
					break;

				case SRM.ILOpCode.Unbox:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Throw:
					break;

				case SRM.ILOpCode.Ldfld:
				case SRM.ILOpCode.Ldflda:
				case SRM.ILOpCode.Stfld:
					operand = ReadHandle(ref operandType);
					//operand = GetField(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Ldsfld:
				case SRM.ILOpCode.Ldsflda:
				case SRM.ILOpCode.Stsfld:
					operand = ReadHandle(ref operandType);

					//operand = GetField(reader.ReadUInt32());
					//var fieldRef = operand as FieldReference;
					//if (fieldRef != null) fieldRef.isStatic = true;
					break;

				case SRM.ILOpCode.Stobj:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Conv_ovf_i1_un:
				case SRM.ILOpCode.Conv_ovf_i2_un:
				case SRM.ILOpCode.Conv_ovf_i4_un:
				case SRM.ILOpCode.Conv_ovf_i8_un:
				case SRM.ILOpCode.Conv_ovf_u1_un:
				case SRM.ILOpCode.Conv_ovf_u2_un:
				case SRM.ILOpCode.Conv_ovf_u4_un:
				case SRM.ILOpCode.Conv_ovf_u8_un:
				case SRM.ILOpCode.Conv_ovf_i_un:
				case SRM.ILOpCode.Conv_ovf_u_un:
					break;

				case SRM.ILOpCode.Box:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Newarr:
					{
						operand = ReadHandle(ref operandType);

						//var elementType = GetType(reader.ReadUInt32());
						//if (elementType != null)
						//	operand = Vector.GetVector(elementType, PEFileToObjectModel.InternFactory);
					}
					break;

				case SRM.ILOpCode.Ldlen:
					break;

				case SRM.ILOpCode.Ldelema:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Ldelem_i1:
				case SRM.ILOpCode.Ldelem_u1:
				case SRM.ILOpCode.Ldelem_i2:
				case SRM.ILOpCode.Ldelem_u2:
				case SRM.ILOpCode.Ldelem_i4:
				case SRM.ILOpCode.Ldelem_u4:
				case SRM.ILOpCode.Ldelem_i8:
				case SRM.ILOpCode.Ldelem_i:
				case SRM.ILOpCode.Ldelem_r4:
				case SRM.ILOpCode.Ldelem_r8:
				case SRM.ILOpCode.Ldelem_ref:
				case SRM.ILOpCode.Stelem_i:
				case SRM.ILOpCode.Stelem_i1:
				case SRM.ILOpCode.Stelem_i2:
				case SRM.ILOpCode.Stelem_i4:
				case SRM.ILOpCode.Stelem_i8:
				case SRM.ILOpCode.Stelem_r4:
				case SRM.ILOpCode.Stelem_r8:
				case SRM.ILOpCode.Stelem_ref:
					break;

				case SRM.ILOpCode.Ldelem:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Stelem:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Unbox_any:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Conv_ovf_i1:
				case SRM.ILOpCode.Conv_ovf_u1:
				case SRM.ILOpCode.Conv_ovf_i2:
				case SRM.ILOpCode.Conv_ovf_u2:
				case SRM.ILOpCode.Conv_ovf_i4:
				case SRM.ILOpCode.Conv_ovf_u4:
				case SRM.ILOpCode.Conv_ovf_i8:
				case SRM.ILOpCode.Conv_ovf_u8:
					break;

				case SRM.ILOpCode.Refanyval:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Ckfinite:
					break;

				case SRM.ILOpCode.Mkrefany:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Ldtoken:
					operand = ReadHandle(ref operandType);
					//operand = GetRuntimeHandleFromToken(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Conv_u2:
				case SRM.ILOpCode.Conv_u1:
				case SRM.ILOpCode.Conv_i:
				case SRM.ILOpCode.Conv_ovf_i:
				case SRM.ILOpCode.Conv_ovf_u:
				case SRM.ILOpCode.Add_ovf:
				case SRM.ILOpCode.Add_ovf_un:
				case SRM.ILOpCode.Mul_ovf:
				case SRM.ILOpCode.Mul_ovf_un:
				case SRM.ILOpCode.Sub_ovf:
				case SRM.ILOpCode.Sub_ovf_un:
				case SRM.ILOpCode.Endfinally:
					break;

				case SRM.ILOpCode.Leave:
					{
						var leaveOffset = (uint)(reader.Offset + 4 + reader.ReadInt32());
						if (leaveOffset >= maxOffset)
						{
							//  Error...
						}
						operandType = OperandType.Immediate;
						operand = leaveOffset;
					}
					break;

				case SRM.ILOpCode.Leave_s:
					{
						var leaveOffset = (uint)(reader.Offset + 1 + reader.ReadSByte());
						if (leaveOffset >= maxOffset)
						{
							//  Error...
						}
						operandType = OperandType.Immediate;
						operand = leaveOffset;
					}
					break;

				case SRM.ILOpCode.Stind_i:
				case SRM.ILOpCode.Conv_u:
				case SRM.ILOpCode.Arglist:
				case SRM.ILOpCode.Ceq:
				case SRM.ILOpCode.Cgt:
				case SRM.ILOpCode.Cgt_un:
				case SRM.ILOpCode.Clt:
				case SRM.ILOpCode.Clt_un:
					break;

				case SRM.ILOpCode.Ldftn:
				case SRM.ILOpCode.Ldvirtftn:
					operand = ReadHandle(ref operandType);
					//operand = GetMethod(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Ldarg:
				case SRM.ILOpCode.Ldarga:
				case SRM.ILOpCode.Starg:
					operandType = OperandType.Immediate;
					operand = (int)reader.ReadUInt16();
					//operand = GetParameter(reader.ReadUInt16());
					break;

				case SRM.ILOpCode.Ldloc:
				case SRM.ILOpCode.Ldloca:
				case SRM.ILOpCode.Stloc:
					operandType = OperandType.Immediate;
					operand = (int)reader.ReadUInt16();
					//operand = GetLocal(reader.ReadUInt16());
					break;

				case SRM.ILOpCode.Localloc:
					break;

				case SRM.ILOpCode.Endfilter:
					break;

				case SRM.ILOpCode.Unaligned:
					operandType = OperandType.Immediate;
					operand = reader.ReadByte();
					break;

				case SRM.ILOpCode.Volatile:
				case SRM.ILOpCode.Tail:
					break;

				case SRM.ILOpCode.Initobj:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Constrained:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Cpblk:
				case SRM.ILOpCode.Initblk:
					break;

				//case SRM.ILOpCode.No_:
				//	operand = (OperationCheckFlags)reader.ReadByte();
				//	break;

				case SRM.ILOpCode.Rethrow:
					break;

				case SRM.ILOpCode.Sizeof:
					operand = ReadHandle(ref operandType);
					//operand = GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Refanytype:
				case SRM.ILOpCode.Readonly:
					break;

				default:
					throw opcode.ToUnknownValueException();
			}

			var result = new ILInstruction(offset, opcode, operandType, operand);
			return result;
		}

		private object ReadHandle(ref OperandType operandType)
		{
			object result;
			var token = reader.ReadInt32();
			var tokenType = GetTokenType(token);

			switch (tokenType)
			{
				case ILTokenType.MemberReference:
					operandType = OperandType.MemberReference;
					result = MetadataTokens.MemberReferenceHandle(token);
					break;

				case ILTokenType.TypeReference:
					operandType = OperandType.TypeReference;
					result = MetadataTokens.TypeReferenceHandle(token);
					break;

				case ILTokenType.FieldDefinition:
					operandType = OperandType.FieldDefinition;
					result = MetadataTokens.FieldDefinitionHandle(token);
					break;

				case ILTokenType.MethodDefinition:
					operandType = OperandType.MethodDefinition;
					result = MetadataTokens.MethodDefinitionHandle(token);
					break;

				case ILTokenType.MethodSpecification:
					operandType = OperandType.MethodSpecification;
					result = MetadataTokens.MethodSpecificationHandle(token);
					break;

				case ILTokenType.TypeDefinition:
					operandType = OperandType.TypeDefinition;
					result = MetadataTokens.TypeDefinitionHandle(token);
					break;

				case ILTokenType.TypeSpecification:
					operandType = OperandType.TypeSpecification;
					result = MetadataTokens.TypeSpecificationHandle(token);
					break;

				//case ILTokenType.ParameterDefinition:
				//	operandType = OperandType.ParameterDefinition;
				//	result = MetadataTokens.ParameterHandle(token);
				//	break;

				case ILTokenType.UserString:
					operandType = OperandType.UserString;
					result = MetadataTokens.UserStringHandle(token);
					break;

				default:
					result = MetadataTokens.EntityHandle(token);
					break;
			}

			return result;
		}

		private static ILTokenType GetTokenType(int token)
		{
			return unchecked((ILTokenType)(token >> 24));
		}

		/*
		private static IParameterDefinition GetParameter(uint rawParamNum)
		{
			if (!MethodDefinition.IsStatic)
			{
				if (rawParamNum == 0) return null; //this
				rawParamNum--;
			}
			IParameterDefinition[] mpa = MethodDefinition.RequiredModuleParameters;
			if (mpa != null && rawParamNum < mpa.Length) return mpa[rawParamNum];
			//  Error...
			return Dummy.ParameterDefinition;
		}

		private static ILocalDefinition GetLocal(uint rawLocNum)
		{
			var locVarDef = MethodBody.LocalVariables;
			if (locVarDef != null && rawLocNum < locVarDef.Length)
				return locVarDef[rawLocNum];
			//  Error...
			return Dummy.LocalVariable;
		}

		private static IMethodReference GetMethod(uint methodToken)
		{
			IMethodReference mmr = PEFileToObjectModel.GetMethodReferenceForToken(MethodDefinition, methodToken);
			return mmr;
		}

		private static IFieldReference GetField(uint fieldToken)
		{
			IFieldReference mfr = PEFileToObjectModel.GetFieldReferenceForToken(MethodDefinition, fieldToken);
			return mfr;
		}

		private static ITypeReference GetType(uint typeToken)
		{
			ITypeReference mtr = PEFileToObjectModel.GetTypeReferenceForToken(MethodDefinition, typeToken);
			if (mtr != null)
				return mtr;
			//  Error...
			return Dummy.TypeReference;
		}

		private static IFunctionPointerTypeReference GetFunctionPointerType(uint standAloneSigToken)
		{
			FunctionPointerType fpt = GetStandAloneMethodSignature(standAloneSigToken);
			if (fpt != null)
				return fpt;
			//  Error...
			return Dummy.FunctionPointer;
		}

		private object GetRuntimeHandleFromToken(uint token)
		{
			return PEFileToObjectModel.GetReferenceForToken(MethodDefinition, token);
		}
		*/
	}
}
