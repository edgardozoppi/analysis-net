using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRM = System.Reflection.Metadata;

namespace MetadataProvider
{
	internal struct ILInstruction
	{
		public uint Offset;
		public SRM.ILOpCode Opcode;
		public object Operand;

		public ILInstruction(uint offset, SRM.ILOpCode opcode, object operand)
		{
			this.Offset = offset;
			this.Opcode = opcode;
			this.Operand = operand;
		}
	}

	internal class ILReader
	{
		private readonly uint maxOffset;
		private readonly SRM.BlobReader reader;
		private readonly SRM.MetadataReader metadata;
		private readonly GenericContext genericContext;
		private readonly SignatureTypeProvider signatureTypeProvider;

		public ILReader(SRM.MetadataReader metadata, SignatureTypeProvider signatureTypeProvider, GenericContext genericContext, SRM.BlobReader reader)
		{
			this.metadata = metadata;
			this.signatureTypeProvider = signatureTypeProvider;
			this.genericContext = genericContext;
			this.reader = reader;
			this.maxOffset = (uint)(reader.Offset + reader.Length);
		}

		public IEnumerable<ILInstruction> ReadInstructions()
		{
			var result = new List<ILInstruction>();

			while (reader.Offset < maxOffset)
			{
				var instruction = ReadInstruction();
				result.Add(instruction);
			}

			return result;
		}

		private ILInstruction ReadInstruction()
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

			var offset = (uint)reader.Offset;
			ushort value = reader.ReadByte();

			if (value == 0xFE)
			{
				value = (ushort)(reader.ReadByte() | 0xFE00);
			}

			var opcode = (SRM.ILOpCode)value;
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
					operand = this.GetParameter((uint)(value - SRM.ILOpCode.Ldarg_0));
					break;

				case SRM.ILOpCode.Ldloc_0:
				case SRM.ILOpCode.Ldloc_1:
				case SRM.ILOpCode.Ldloc_2:
				case SRM.ILOpCode.Ldloc_3:
					operand = this.GetLocal((uint)(value - SRM.ILOpCode.Ldloc_0));
					break;

				case SRM.ILOpCode.Stloc_0:
				case SRM.ILOpCode.Stloc_1:
				case SRM.ILOpCode.Stloc_2:
				case SRM.ILOpCode.Stloc_3:
					operand = this.GetLocal((uint)(value - SRM.ILOpCode.Stloc_0));
					break;

				case SRM.ILOpCode.Ldarg_s:
				case SRM.ILOpCode.Ldarga_s:
				case SRM.ILOpCode.Starg_s:
					operand = this.GetParameter(reader.ReadByte());
					break;

				case SRM.ILOpCode.Ldloc_s:
				case SRM.ILOpCode.Ldloca_s:
				case SRM.ILOpCode.Stloc_s:
					operand = this.GetLocal(reader.ReadByte());
					break;

				case SRM.ILOpCode.Ldnull:
					break;

				case SRM.ILOpCode.Ldc_i4_m1:
					operand = C_i4_M1;
					break;

				case SRM.ILOpCode.Ldc_i4_0:
					operand = C_i4_0;
					break;

				case SRM.ILOpCode.Ldc_i4_1:
					operand = C_i4_1;
					break;

				case SRM.ILOpCode.Ldc_i4_2:
					operand = C_i4_2;
					break;

				case SRM.ILOpCode.Ldc_i4_3:
					operand = C_i4_3;
					break;

				case SRM.ILOpCode.Ldc_i4_4:
					operand = C_i4_4;
					break;

				case SRM.ILOpCode.Ldc_i4_5:
					operand = C_i4_5;
					break;

				case SRM.ILOpCode.Ldc_i4_6:
					operand = C_i4_6;
					break;

				case SRM.ILOpCode.Ldc_i4_7:
					operand = C_i4_7;
					break;

				case SRM.ILOpCode.Ldc_i4_8:
					operand = C_i4_8;
					break;

				case SRM.ILOpCode.Ldc_i4_s:
					operand = (int)reader.ReadSByte();
					break;

				case SRM.ILOpCode.Ldc_i4:
					operand = reader.ReadInt32();
					break;

				case SRM.ILOpCode.Ldc_i8:
					operand = reader.ReadInt64();
					break;

				case SRM.ILOpCode.Ldc_r4:
					operand = reader.ReadSingle();
					break;

				case SRM.ILOpCode.Ldc_r8:
					operand = reader.ReadDouble();
					break;

				case SRM.ILOpCode.Dup:
				case SRM.ILOpCode.Pop:
					break;

				case SRM.ILOpCode.Jmp:
					operand = this.GetMethod(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Call:
					{
						IMethodReference methodReference = this.GetMethod(reader.ReadUInt32());
						IArrayTypeReference arrayType = methodReference.ContainingType as IArrayTypeReference;
						if (arrayType != null)
						{
							// For Get(), Set() and Address() on arrays, the runtime provides method implementations.
							// Hence, CCI2 replaces these with pseudo instructions Array_set, Array_Get and Array_Addr.
							// All other methods on arrays will not use pseudo instruction and will have methodReference as their operand. 
							if (methodReference.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Set.UniqueKey)
							{
								value = SRM.ILOpCode.Array_set;
								operand = arrayType;
							}
							else if (methodReference.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Get.UniqueKey)
							{
								value = SRM.ILOpCode.Array_Get;
								operand = arrayType;
							}
							else if (methodReference.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Address.UniqueKey)
							{
								value = SRM.ILOpCode.Array_Addr;
								operand = arrayType;
							}
							else
							{
								operand = methodReference;
							}
						}
						else
						{
							operand = methodReference;
						}
					}
					break;

				case SRM.ILOpCode.Calli:
					operand = this.GetFunctionPointerType(reader.ReadUInt32());
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
						uint jumpOffset = (uint)(reader.Offset + 1 + reader.ReadSByte());
						if (jumpOffset >= maxOffset)
						{
							//  Error...
						}
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
						uint jumpOffset = (uint)(reader.Offset + 4 + reader.ReadInt32());
						if (jumpOffset >= maxOffset)
						{
							//  Error...
						}
						operand = jumpOffset;
					}
					break;

				case SRM.ILOpCode.Switch:
					{
						uint numTargets = reader.ReadUInt32();
						uint[] targets = new uint[numTargets];
						uint asOffset = (uint)reader.Offset + numTargets * 4;
						for (int i = 0; i < numTargets; i++)
						{
							uint targetAddress = reader.ReadUInt32() + asOffset;
							if (targetAddress >= maxOffset)
							{
								//  Error...
							}
							targets[i] = targetAddress;
						}
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
						IMethodReference methodReference = this.GetMethod(reader.ReadUInt32());
						IArrayTypeReference arrayType = methodReference.ContainingType as IArrayTypeReference;
						if (arrayType != null)
						{
							// For Get(), Set() and Address() on arrays, the runtime provides method implementations.
							// Hence, CCI2 replaces these with pseudo instructions Array_set, Array_Get and Array_Addr.
							// All other methods on arrays will not use pseudo instruction and will have methodReference as their operand. 
							if (methodReference.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Set.UniqueKey)
							{
								value = SRM.ILOpCode.Array_set;
								operand = arrayType;
							}
							else if (methodReference.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Get.UniqueKey)
							{
								value = SRM.ILOpCode.Array_Get;
								operand = arrayType;
							}
							else if (methodReference.Name.UniqueKey == this.PEFileToObjectModel.NameTable.Address.UniqueKey)
							{
								value = SRM.ILOpCode.Array_Addr;
								operand = arrayType;
							}
							else
							{
								operand = methodReference;
							}
						}
						else
						{
							operand = methodReference;
						}
					}
					break;

				case SRM.ILOpCode.Cpobj:
				case SRM.ILOpCode.Ldobj:
					operand = this.GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Ldstr:
					operand = this.GetUserStringForToken(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Newobj:
					{
						IMethodReference methodReference = this.GetMethod(reader.ReadUInt32());
						IArrayTypeReference arrayType = methodReference.ContainingType as IArrayTypeReference;
						if (arrayType != null && !arrayType.IsVector)
						{
							uint numParam = IteratorHelper.EnumerableCount(methodReference.Parameters);
							if (numParam != arrayType.Rank)
								value = SRM.ILOpCode.Array_Create_WithLowerBound;
							else
								value = SRM.ILOpCode.Array_Create;
							operand = arrayType;
						}
						else
						{
							operand = methodReference;
						}
					}
					break;

				case SRM.ILOpCode.Castclass:
				case SRM.ILOpCode.Isinst:
					operand = this.GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Conv_r_un:
					break;

				case SRM.ILOpCode.Unbox:
					operand = this.GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Throw:
					break;

				case SRM.ILOpCode.Ldfld:
				case SRM.ILOpCode.Ldflda:
				case SRM.ILOpCode.Stfld:
					operand = this.GetField(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Ldsfld:
				case SRM.ILOpCode.Ldsflda:
				case SRM.ILOpCode.Stsfld:
					operand = this.GetField(reader.ReadUInt32());
					var fieldRef = operand as FieldReference;
					if (fieldRef != null) fieldRef.isStatic = true;
					break;

				case SRM.ILOpCode.Stobj:
					operand = this.GetType(reader.ReadUInt32());
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
					operand = this.GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Newarr:
					{
						var elementType = this.GetType(reader.ReadUInt32());
						if (elementType != null)
							operand = Vector.GetVector(elementType, PEFileToObjectModel.InternFactory);
						else
							operand = Dummy.ArrayType;
					}
					break;

				case SRM.ILOpCode.Ldlen:
					break;

				case SRM.ILOpCode.Ldelema:
					operand = this.GetType(reader.ReadUInt32());
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
					operand = this.GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Stelem:
					operand = this.GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Unbox_any:
					operand = this.GetType(reader.ReadUInt32());
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
					operand = this.GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Ckfinite:
					break;

				case SRM.ILOpCode.Mkrefany:
					operand = this.GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Ldtoken:
					operand = this.GetRuntimeHandleFromToken(reader.ReadUInt32());
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
						uint leaveOffset = (uint)(reader.Offset + 4 + reader.ReadInt32());
						if (leaveOffset >= maxOffset)
						{
							//  Error...
						}
						operand = leaveOffset;
					}
					break;

				case SRM.ILOpCode.Leave_s:
					{
						uint leaveOffset = (uint)(reader.Offset + 1 + reader.ReadSByte());
						if (leaveOffset >= maxOffset)
						{
							//  Error...
						}
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
					operand = this.GetMethod(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Ldarg:
				case SRM.ILOpCode.Ldarga:
				case SRM.ILOpCode.Starg:
					operand = this.GetParameter(reader.ReadUInt16());
					break;

				case SRM.ILOpCode.Ldloc:
				case SRM.ILOpCode.Ldloca:
				case SRM.ILOpCode.Stloc:
					operand = this.GetLocal(reader.ReadUInt16());
					break;

				case SRM.ILOpCode.Localloc:
					break;

				case SRM.ILOpCode.Endfilter:
					break;

				case SRM.ILOpCode.Unaligned:
					operand = reader.ReadByte();
					break;

				case SRM.ILOpCode.Volatile:
				case SRM.ILOpCode.Tail:
					break;

				case SRM.ILOpCode.Initobj:
					operand = this.GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Constrained:
					operand = this.GetType(reader.ReadUInt32());
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
					operand = this.GetType(reader.ReadUInt32());
					break;

				case SRM.ILOpCode.Refanytype:
				case SRM.ILOpCode.Readonly:
					break;


				default:
					throw opcode.ToUnknownValueException();
			}

			var result = new ILInstruction(offset, opcode, operand);
			return result;
		}

		private IParameterDefinition GetParameter(uint rawParamNum)
		{
			if (!this.MethodDefinition.IsStatic)
			{
				if (rawParamNum == 0) return null; //this
				rawParamNum--;
			}
			IParameterDefinition[] mpa = this.MethodDefinition.RequiredModuleParameters;
			if (mpa != null && rawParamNum < mpa.Length) return mpa[rawParamNum];
			//  Error...
			return Dummy.ParameterDefinition;
		}

		private ILocalDefinition GetLocal(uint rawLocNum)
		{
			var locVarDef = this.MethodBody.LocalVariables;
			if (locVarDef != null && rawLocNum < locVarDef.Length)
				return locVarDef[rawLocNum];
			//  Error...
			return Dummy.LocalVariable;
		}

		private IMethodReference GetMethod(uint methodToken)
		{
			IMethodReference mmr = this.PEFileToObjectModel.GetMethodReferenceForToken(this.MethodDefinition, methodToken);
			return mmr;
		}

		private IFieldReference GetField(uint fieldToken)
		{
			IFieldReference mfr = this.PEFileToObjectModel.GetFieldReferenceForToken(this.MethodDefinition, fieldToken);
			return mfr;
		}

		private ITypeReference GetType(uint typeToken)
		{
			ITypeReference mtr = this.PEFileToObjectModel.GetTypeReferenceForToken(this.MethodDefinition, typeToken);
			if (mtr != null)
				return mtr;
			//  Error...
			return Dummy.TypeReference;
		}

		private IFunctionPointerTypeReference GetFunctionPointerType(uint standAloneSigToken)
		{
			FunctionPointerType fpt = this.GetStandAloneMethodSignature(standAloneSigToken);
			if (fpt != null)
				return fpt;
			//  Error...
			return Dummy.FunctionPointer;
		}

		private object GetRuntimeHandleFromToken(uint token)
		{
			return this.PEFileToObjectModel.GetReferenceForToken(this.MethodDefinition, token);
		}
	}
}
