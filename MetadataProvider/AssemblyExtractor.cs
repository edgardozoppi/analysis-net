using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using Model.ThreeAddressCode.Values;
using Model.Types;
using SR = System.Reflection;
using SRPE = System.Reflection.PortableExecutable;
using SRM = System.Reflection.Metadata;
using Model.Bytecode;

namespace MetadataProvider
{
	internal class AssemblyExtractor
	{
		#region FakeArrayType

		private struct FakeArrayType : IBasicType
		{
			public ArrayType Type { get; private set; }

			public FakeArrayType(ArrayType type)
			{
				this.Type = type;
			}

			public IAssemblyReference ContainingAssembly
			{
				get { return null; }
			}

			public string ContainingNamespace
			{
				get { return string.Empty; }
			}

			public string Name
			{
				get { return "FakeArray"; }
			}

			public string GenericName
			{
				get { return this.Name; }
			}

			public IList<IType> GenericArguments
			{
				get { return null; }
			}

			public IBasicType GenericType
			{
				get { return null; }
			}

			public TypeDefinition ResolvedType
			{
				get { return null; }
			}

			public TypeKind TypeKind
			{
				get { return TypeKind.ReferenceType; }
			}

			public ISet<CustomAttribute> Attributes
			{
				get { return null; }
			}

			public int GenericParameterCount
			{
				get { return 0; }
			}

			public IBasicType ContainingType
			{
				get { return null; }
			}
		}

		#endregion

		private IDictionary<SRM.TypeDefinitionHandle, TypeDefinition> definedTypes;
		private IDictionary<SRM.MethodDefinitionHandle, MethodDefinition> definedMethods;
		private IDictionary<SRM.FieldDefinitionHandle, FieldDefinition> definedFields;

		private SRPE.PEReader reader;
		private SRM.MetadataReader metadata;
		private GenericContext defGenericContext;
		private GenericContext refGenericContext;
		private SignatureTypeProvider signatureTypeProvider;
		private Assembly assembly;
		private Namespace currentNamespace;
		private TypeDefinition currentType;
		private MethodDefinition currentMethod;

		public AssemblyExtractor(Host host, SRPE.PEReader reader)
		{
			this.Host = host;
			this.reader = reader;
			this.metadata = SRM.PEReaderExtensions.GetMetadataReader(reader);
			this.definedTypes = new Dictionary<SRM.TypeDefinitionHandle, TypeDefinition>();
			this.definedMethods = new Dictionary<SRM.MethodDefinitionHandle, MethodDefinition>();
			this.definedFields = new Dictionary<SRM.FieldDefinitionHandle, FieldDefinition>();
			this.defGenericContext = new GenericContext();
			this.refGenericContext = new GenericContext();
			this.signatureTypeProvider = new SignatureTypeProvider(this);
		}

		public Host Host { get; private set; }

		public TypeDefinition GetDefinedType(SRM.TypeDefinitionHandle handle)
		{
			TypeDefinition result;
			var ok = definedTypes.TryGetValue(handle, out result);

			if (!ok)
			{
				var typedef = metadata.GetTypeDefinition(handle);
				var name = metadata.GetString(typedef.Name);
				name = GetGenericName(name);

				result = new TypeDefinition(name);
				definedTypes.Add(handle, result);
			}

			return result;
		}

		public MethodDefinition GetDefinedMethod(SRM.MethodDefinitionHandle handle)
		{
			MethodDefinition result;
			var ok = definedMethods.TryGetValue(handle, out result);

			if (!ok)
			{
				var methoddef = metadata.GetMethodDefinition(handle);
				var name = metadata.GetString(methoddef.Name);
				name = GetGenericName(name);

				result = new MethodDefinition(name, null);
				definedMethods.Add(handle, result);
			}

			return result;
		}

		public FieldDefinition GetDefinedField(SRM.FieldDefinitionHandle handle)
		{
			FieldDefinition result;
			var ok = definedFields.TryGetValue(handle, out result);

			if (!ok)
			{
				var fielddef = metadata.GetFieldDefinition(handle);
				var name = metadata.GetString(fielddef.Name);

				result = new FieldDefinition(name, null);
				definedFields.Add(handle, result);
			}

			return result;
		}

		public Assembly Extract()
		{
			var assemblydef = metadata.GetAssemblyDefinition();
			var name = metadata.GetString(assemblydef.Name);
			assembly = new Assembly(name);

			foreach (var handle in metadata.AssemblyReferences)
			{
				var referencedef = metadata.GetAssemblyReference(handle);
				name = metadata.GetString(referencedef.Name);
				var reference = new AssemblyReference(name);

				assembly.References.Add(reference);
			}

			var namespacedef = metadata.GetNamespaceDefinitionRoot();
			ExtractNamespace(namespacedef);

			return assembly;
		}

		private void ExtractNamespace(SRM.NamespaceDefinitionHandle handle)
		{
			var namespacedef = metadata.GetNamespaceDefinition(handle);
			ExtractNamespace(namespacedef);
		}

		private void ExtractNamespace(SRM.NamespaceDefinition namespacedef)
		{
			var name = metadata.GetString(namespacedef.Name);
			var namespaze = new Namespace(name);

			if (currentNamespace == null)
			{
				assembly.RootNamespace = namespaze;
			}
			else
			{
				currentNamespace.Namespaces.Add(namespaze);
			}

			namespaze.ContainingAssembly = assembly;
			namespaze.ContainingNamespace = currentNamespace;
			currentNamespace = namespaze;

			foreach (var handle in namespacedef.NamespaceDefinitions)
			{
				ExtractNamespace(handle);
			}

			foreach (var handle in namespacedef.TypeDefinitions)
			{
				ExtractType(handle);
			}

			currentNamespace = currentNamespace.ContainingNamespace;
		}

		private void ExtractType(SRM.TypeDefinitionHandle typedefHandle)
		{
			var typedef = metadata.GetTypeDefinition(typedefHandle);
			var type = GetDefinedType(typedefHandle);

			if (currentType == null)
			{
				currentNamespace.Types.Add(type);
			}
			else
			{
				currentType.Types.Add(type);
			}

			type.Kind = GetTypeDefinitionKind(typedef.Attributes);
			type.TypeKind = type.Kind.ToTypeKind();
			type.ContainingType = currentType;
			type.ContainingAssembly = assembly;
			type.ContainingNamespace = currentNamespace;
			currentType = type;

			foreach (var handle in typedef.GetGenericParameters())
			{
				ExtractGenericParameter(GenericParameterKind.Type, type, handle);
			}

			ExtractBaseType(typedef.BaseType);

			foreach (var handle in typedef.GetInterfaceImplementations())
			{
				ExtractInterfaceImplementation(handle);
			}

			foreach (var handle in typedef.GetFields())
			{
				ExtractField(handle);
			}

			foreach (var handle in typedef.GetMethods())
			{
				ExtractMethod(handle);
			}

			defGenericContext.TypeParameters.Clear();

			foreach (var handle in typedef.GetNestedTypes())
			{
				ExtractType(handle);
			}

			currentType = currentType.ContainingType;
		}

		private static TypeDefinitionKind GetTypeDefinitionKind(SR.TypeAttributes attributes)
		{
			var result = TypeDefinitionKind.Unknown;

			if (attributes.HasFlag(SR.TypeAttributes.Class))
			{
				result = TypeDefinitionKind.Class;
			}
			else if (attributes.HasFlag(SR.TypeAttributes.Interface))
			{
				result = TypeDefinitionKind.Interface;
			}

			return result;
		}

		private void ExtractBaseType(SRM.EntityHandle handle)
		{
			currentType.Base = (IBasicType)signatureTypeProvider.GetTypeFromHandle(metadata, defGenericContext, handle);
		}

		private void ExtractInterfaceImplementation(SRM.InterfaceImplementationHandle handle)
		{
			var interfaceref = metadata.GetInterfaceImplementation(handle);
			var typeref = (IBasicType)signatureTypeProvider.GetTypeFromHandle(metadata, defGenericContext, interfaceref.Interface);
			currentType.Interfaces.Add(typeref);
		}

		private void ExtractGenericParameter(GenericParameterKind parameterKind, IGenericDefinition genericContainer, SRM.GenericParameterHandle handle)
		{
			var genericParameterdef = metadata.GetGenericParameter(handle);
			var typeKind = TypeKind.Unknown;

			if (genericParameterdef.Attributes.HasFlag(SR.GenericParameterAttributes.ReferenceTypeConstraint))
			{
				typeKind = TypeKind.ReferenceType;
			}

			if (genericParameterdef.Attributes.HasFlag(SR.GenericParameterAttributes.NotNullableValueTypeConstraint))
			{
				typeKind = TypeKind.ValueType;
			}

			var name = metadata.GetString(genericParameterdef.Name);
			var genericParameter = new GenericParameter(parameterKind, (ushort)genericParameterdef.Index, name, typeKind)
			{
				GenericContainer = genericContainer
			};

			if (parameterKind == GenericParameterKind.Type)
			{
				defGenericContext.TypeParameters.Add(genericParameter);
			}
			else if (parameterKind == GenericParameterKind.Method)
			{
				defGenericContext.MethodParameters.Add(genericParameter);
			}
			else
			{
				throw parameterKind.ToUnknownValueException();
			}

			genericContainer.GenericParameters.Add(genericParameter);
		}

		private void ExtractField(SRM.FieldDefinitionHandle handle)
		{
			var fielddef = metadata.GetFieldDefinition(handle);
			var field = GetDefinedField(handle);

			field.ContainingType = currentType;
			field.Type = fielddef.DecodeSignature(signatureTypeProvider, defGenericContext);
			field.IsStatic = fielddef.Attributes.HasFlag(SR.FieldAttributes.Static);

			currentType.Fields.Add(field);
		}

		private void ExtractMethod(SRM.MethodDefinitionHandle methoddefHandle)
		{
			var methoddef = metadata.GetMethodDefinition(methoddefHandle);
			var method = GetDefinedMethod(methoddefHandle);

			method.ContainingType = currentType;
			method.IsStatic = methoddef.Attributes.HasFlag(SR.MethodAttributes.Static);
			method.IsAbstract = methoddef.Attributes.HasFlag(SR.MethodAttributes.Abstract);
			method.IsVirtual = methoddef.Attributes.HasFlag(SR.MethodAttributes.Virtual);
			method.IsExternal = methoddef.Attributes.HasFlag(SR.MethodAttributes.PinvokeImpl);
			method.IsConstructor = method.Name.EndsWith(".ctor");

			currentType.Methods.Add(method);
			currentMethod = method;

			foreach (var handle in methoddef.GetGenericParameters())
			{
				ExtractGenericParameter(GenericParameterKind.Method, method, handle);
			}

			var signature = methoddef.DecodeSignature(signatureTypeProvider, defGenericContext);
			method.ReturnType = signature.ReturnType;

			foreach (var handle in methoddef.GetParameters())
			{
				ExtractParameter(signature, handle);
			}
			
			ExtractMethodBody(methoddef.RelativeVirtualAddress);

			defGenericContext.MethodParameters.Clear();
			currentMethod = null;
		}

		private void ExtractParameter(SRM.MethodSignature<IType> signature, SRM.ParameterHandle handle)
		{
			var parameterdef = metadata.GetParameter(handle);
			var type = signature.ParameterTypes[parameterdef.SequenceNumber - 1];
			var parameterName = metadata.GetString(parameterdef.Name);

			var parameter = new MethodParameter((ushort)parameterdef.SequenceNumber, parameterName, type)
			{
				Kind = GetParameterKind(parameterdef.Attributes, type),
				DefaultValue = ExtractParameterDefaultValue(parameterdef, type)
			};

			currentMethod.Parameters.Add(parameter);
		}

		private static MethodParameterKind GetParameterKind(SR.ParameterAttributes attributes, IType type)
		{
			var result = MethodParameterKind.In;
			var isOut = attributes.HasFlag(SR.ParameterAttributes.Out);

			if (isOut)
			{
				result = MethodParameterKind.Out;
			}
			else if (type.IsPointer())
			{
				result = MethodParameterKind.Ref;
			}

			return result;
		}

		private Constant ExtractParameterDefaultValue(SRM.Parameter parameterdef, IType type)
		{
			Constant result = null;
			var hasDefaultValue = parameterdef.Attributes.HasFlag(SR.ParameterAttributes.HasDefault);

			if (hasDefaultValue)
			{
				var defaultValueHandle = parameterdef.GetDefaultValue();
				var constant = metadata.GetConstant(defaultValueHandle);
				var reader = metadata.GetBlobReader(constant.Value);
				var value = reader.ReadConstant(constant.TypeCode);

				result = new Constant(value)
				{
					Type = type
				};
			}

			return result;
		}

		private void ExtractMethodBody(int relativeVirtualAddress)
		{
			if (relativeVirtualAddress == 0) return;
			var bodyBlock = SRM.PEReaderExtensions.GetMethodBody(reader, relativeVirtualAddress);
			var body = new MethodBody(MethodBodyKind.Bytecode)
			{
				MaxStack = (ushort)bodyBlock.MaxStack
			};

			currentMethod.Body = body;

			ExtractParameters(body.Parameters);
			ExtractLocalVariables(bodyBlock, body.LocalVariables);
			ExtractExceptionInformation(bodyBlock, body.ExceptionInformation);
			ExtractInstructions(bodyBlock, body.Instructions);
		}

		private void ExtractParameters(IList<IVariable> parameters)
		{
			if (!currentMethod.IsStatic)
			{
				IType type = currentMethod.ContainingType;

				if (type.TypeKind == TypeKind.ValueType)
				{
					type = signatureTypeProvider.GetByReferenceType(type);
				}

				var v = new LocalVariable("this", true) { Type = type };
				parameters.Add(v);
			}

			foreach (var parameter in currentMethod.Parameters)
			{
				var v = new LocalVariable(parameter.Name, true)
				{
					Type = parameter.Type
				};

				parameters.Add(v);
			}
		}

		private void ExtractLocalVariables(SRM.MethodBodyBlock bodyBlock, IList<IVariable> variables)
		{
			if (bodyBlock.LocalSignature.IsNil) return;
			var localSignature = metadata.GetStandaloneSignature(bodyBlock.LocalSignature);
			var types = localSignature.DecodeLocalSignature(signatureTypeProvider, defGenericContext);

			for (var i = 0; i < types.Length; ++i)
			{
				var name = GetLocalSourceName(i);
				var type = types[i];
				var v = new LocalVariable(name)
				{
					Type = type
				};

				variables.Add(v);
			}
		}

		private string GetLocalSourceName(int localVariableIndex)
		{
			// TODO: Figure out how to get original variable name from PDB!
			return string.Format("local_{0}", localVariableIndex);
		}

		private void ExtractExceptionInformation(SRM.MethodBodyBlock bodyBlock, IList<ProtectedBlock> handlers)
		{
			foreach (var region in bodyBlock.ExceptionRegions)
			{
				var endOffset = region.TryOffset + region.TryLength;
				var tryHandler = new ProtectedBlock((uint)region.TryOffset, (uint)endOffset);
				endOffset = region.HandlerOffset + region.HandlerLength;

				switch (region.Kind)
				{
					case SRM.ExceptionRegionKind.Filter:
						var filterExceptionType = signatureTypeProvider.GetPrimitiveType(SRM.PrimitiveTypeCode.Object);
						var filterHandler = new FilterExceptionHandler((uint)region.FilterOffset, (uint)region.HandlerOffset, (uint)endOffset, filterExceptionType);
						tryHandler.Handler = filterHandler;
						break;

					case SRM.ExceptionRegionKind.Catch:
						var catchExceptionType = signatureTypeProvider.GetTypeFromHandle(metadata, defGenericContext, region.CatchType);
						var catchHandler = new CatchExceptionHandler((uint)region.HandlerOffset, (uint)endOffset, catchExceptionType);
						tryHandler.Handler = catchHandler;
						break;

					case SRM.ExceptionRegionKind.Fault:
						var faultHandler = new FaultExceptionHandler((uint)region.HandlerOffset, (uint)endOffset);
						tryHandler.Handler = faultHandler;
						break;

					case SRM.ExceptionRegionKind.Finally:
						var finallyHandler = new FinallyExceptionHandler((uint)region.HandlerOffset, (uint)endOffset);
						tryHandler.Handler = finallyHandler;
						break;

					default:
						throw new Exception("Unknown exception region kind");
				}

				handlers.Add(tryHandler);
			}
		}

		private void ExtractInstructions(SRM.MethodBodyBlock bodyBlock, IList<IInstruction> instructions)
		{
			var ilReader = new ILReader(bodyBlock);
			var operations = ilReader.ReadInstructions();

			//Console.WriteLine();
			//Console.WriteLine(currentMethod.Name);

			foreach (var op in operations)
			{
				//Console.WriteLine(op);

				var instruction = ExtractInstruction(op);
				instructions.Add(instruction);
			}
		}

		#region Extract Instructions

		private IInstruction ExtractInstruction(ILInstruction operation)
		{
			IInstruction instruction = null;

			switch (operation.Opcode)
			{
				case SRM.ILOpCode.Add:
				case SRM.ILOpCode.Add_ovf:
				case SRM.ILOpCode.Add_ovf_un:
				case SRM.ILOpCode.And:
				case SRM.ILOpCode.Ceq:
				case SRM.ILOpCode.Cgt:
				case SRM.ILOpCode.Cgt_un:
				case SRM.ILOpCode.Clt:
				case SRM.ILOpCode.Clt_un:
				case SRM.ILOpCode.Div:
				case SRM.ILOpCode.Div_un:
				case SRM.ILOpCode.Mul:
				case SRM.ILOpCode.Mul_ovf:
				case SRM.ILOpCode.Mul_ovf_un:
				case SRM.ILOpCode.Or:
				case SRM.ILOpCode.Rem:
				case SRM.ILOpCode.Rem_un:
				case SRM.ILOpCode.Shl:
				case SRM.ILOpCode.Shr:
				case SRM.ILOpCode.Shr_un:
				case SRM.ILOpCode.Sub:
				case SRM.ILOpCode.Sub_ovf:
				case SRM.ILOpCode.Sub_ovf_un:
				case SRM.ILOpCode.Xor:
					instruction = ProcessBasic(operation);
					break;

				//case SRM.ILOpCode.Arglist:
				//    //expression = new RuntimeArgumentHandleExpression();
				//    break;

				case SRM.ILOpCode.Newarr:
					instruction = ProcessCreateArray(operation);
					break;

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
				case SRM.ILOpCode.Ldelem_ref:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Ldelema:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Beq:
				case SRM.ILOpCode.Beq_s:
				case SRM.ILOpCode.Bne_un:
				case SRM.ILOpCode.Bne_un_s:
				case SRM.ILOpCode.Bge:
				case SRM.ILOpCode.Bge_s:
				case SRM.ILOpCode.Bge_un:
				case SRM.ILOpCode.Bge_un_s:
				case SRM.ILOpCode.Bgt:
				case SRM.ILOpCode.Bgt_s:
				case SRM.ILOpCode.Bgt_un:
				case SRM.ILOpCode.Bgt_un_s:
				case SRM.ILOpCode.Ble:
				case SRM.ILOpCode.Ble_s:
				case SRM.ILOpCode.Ble_un:
				case SRM.ILOpCode.Ble_un_s:
				case SRM.ILOpCode.Blt:
				case SRM.ILOpCode.Blt_s:
				case SRM.ILOpCode.Blt_un:
				case SRM.ILOpCode.Blt_un_s:
					instruction = ProcessBinaryConditionalBranch(operation);
					break;

				case SRM.ILOpCode.Br:
				case SRM.ILOpCode.Br_s:
					instruction = ProcessUnconditionalBranch(operation);
					break;

				case SRM.ILOpCode.Leave:
				case SRM.ILOpCode.Leave_s:
					instruction = ProcessLeave(operation);
					break;

				case SRM.ILOpCode.Break:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Nop:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Brfalse:
				case SRM.ILOpCode.Brfalse_s:
				case SRM.ILOpCode.Brtrue:
				case SRM.ILOpCode.Brtrue_s:
					instruction = ProcessUnaryConditionalBranch(operation);
					break;

				case SRM.ILOpCode.Call:
				case SRM.ILOpCode.Callvirt:
				case SRM.ILOpCode.Jmp:
					instruction = ProcessMethodCall(operation);
					break;

				case SRM.ILOpCode.Calli:
					instruction = ProcessMethodCallIndirect(operation);
					break;

				case SRM.ILOpCode.Castclass:
				case SRM.ILOpCode.Isinst:
				case SRM.ILOpCode.Box:
				case SRM.ILOpCode.Unbox:
				case SRM.ILOpCode.Unbox_any:
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
				case SRM.ILOpCode.Conv_r_un:
					instruction = ProcessConversion(operation);
					break;

				//case SRM.ILOpCode.Ckfinite:
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

				//case SRM.ILOpCode.Constrained_:
				//	// This prefix is redundant and is not represented in the code model.
				//	break;

				case SRM.ILOpCode.Cpblk:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Cpobj:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Dup:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Endfilter:
				case SRM.ILOpCode.Endfinally:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Initblk:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Initobj:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Ldarg:
				case SRM.ILOpCode.Ldarg_0:
				case SRM.ILOpCode.Ldarg_1:
				case SRM.ILOpCode.Ldarg_2:
				case SRM.ILOpCode.Ldarg_3:
				case SRM.ILOpCode.Ldarg_s:
				case SRM.ILOpCode.Ldarga:
				case SRM.ILOpCode.Ldarga_s:
					instruction = ProcessLoadArgument(operation);
					break;

				case SRM.ILOpCode.Ldloc:
				case SRM.ILOpCode.Ldloc_0:
				case SRM.ILOpCode.Ldloc_1:
				case SRM.ILOpCode.Ldloc_2:
				case SRM.ILOpCode.Ldloc_3:
				case SRM.ILOpCode.Ldloc_s:
				case SRM.ILOpCode.Ldloca:
				case SRM.ILOpCode.Ldloca_s:
					instruction = ProcessLoadLocal(operation);
					break;

				case SRM.ILOpCode.Ldfld:
				case SRM.ILOpCode.Ldsfld:
				case SRM.ILOpCode.Ldflda:
				case SRM.ILOpCode.Ldsflda:
					instruction = ProcessLoadField(operation);
					break;

				case SRM.ILOpCode.Ldftn:
				case SRM.ILOpCode.Ldvirtftn:
					instruction = ProcessLoadMethodAddress(operation);
					break;

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
				case SRM.ILOpCode.Ldstr:
					instruction = ProcessLoadConstant(operation);
					break;

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
				case SRM.ILOpCode.Ldobj:
					instruction = ProcessLoadIndirect(operation);
					break;

				case SRM.ILOpCode.Ldlen:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Ldtoken:
					instruction = ProcessLoadToken(operation);
					break;

				case SRM.ILOpCode.Localloc:
					instruction = ProcessBasic(operation);
					break;

				//case SRM.ILOpCode.Mkrefany:
				//    expression = result = ParseMakeTypedReference(currentOperation);
				//    break;

				case SRM.ILOpCode.Neg:
				case SRM.ILOpCode.Not:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Newobj:
					instruction = ProcessCreateObject(operation);
					break;

				//case SRM.ILOpCode.No_:
				//	// If code out there actually uses this, I need to know sooner rather than later.
				//	// TODO: need object model support
				//	throw new NotImplementedException("Invalid opcode: No.");

				case SRM.ILOpCode.Pop:
					instruction = ProcessBasic(operation);
					break;

				//case SRM.ILOpCode.Readonly_:
				//    result = sawReadonly = true;
				//    break;

				//case SRM.ILOpCode.Refanytype:
				//    expression = result = ParseGetTypeOfTypedReference();
				//    break;

				//case SRM.ILOpCode.Refanyval:
				//    expression = result = ParseGetValueOfTypedReference(currentOperation);
				//    break;

				case SRM.ILOpCode.Ret:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Sizeof:
					instruction = ProcessSizeof(operation);
					break;

				case SRM.ILOpCode.Starg:
				case SRM.ILOpCode.Starg_s:
					instruction = ProcessStoreArgument(operation);
					break;

				case SRM.ILOpCode.Stelem:
				case SRM.ILOpCode.Stelem_i:
				case SRM.ILOpCode.Stelem_i1:
				case SRM.ILOpCode.Stelem_i2:
				case SRM.ILOpCode.Stelem_i4:
				case SRM.ILOpCode.Stelem_i8:
				case SRM.ILOpCode.Stelem_r4:
				case SRM.ILOpCode.Stelem_r8:
				case SRM.ILOpCode.Stelem_ref:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Stfld:
				case SRM.ILOpCode.Stsfld:
					instruction = ProcessStoreField(operation);
					break;

				case SRM.ILOpCode.Stind_i:
				case SRM.ILOpCode.Stind_i1:
				case SRM.ILOpCode.Stind_i2:
				case SRM.ILOpCode.Stind_i4:
				case SRM.ILOpCode.Stind_i8:
				case SRM.ILOpCode.Stind_r4:
				case SRM.ILOpCode.Stind_r8:
				case SRM.ILOpCode.Stind_ref:
				case SRM.ILOpCode.Stobj:
					instruction = ProcessBasic(operation);
					break;

				case SRM.ILOpCode.Stloc:
				case SRM.ILOpCode.Stloc_0:
				case SRM.ILOpCode.Stloc_1:
				case SRM.ILOpCode.Stloc_2:
				case SRM.ILOpCode.Stloc_3:
				case SRM.ILOpCode.Stloc_s:
					instruction = ProcessStoreLocal(operation);
					break;

				case SRM.ILOpCode.Switch:
					instruction = ProcessSwitch(operation);
					break;

				//case SRM.ILOpCode.Tail_:
				//    result = sawTailCall = true;
				//    break;

				case SRM.ILOpCode.Throw:
				case SRM.ILOpCode.Rethrow:
					instruction = ProcessBasic(operation);
					break;

				//case SRM.ILOpCode.Unaligned_:
				//    Contract.Assume(currentOperation.Value is byte);
				//    var alignment = (byte)currentOperation.Value;
				//    Contract.Assume(alignment == 1 || alignment == 2 || alignment == 4);
				//    result = alignment = alignment;
				//    break;

				//case SRM.ILOpCode.Volatile_:
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

		private T GetOperand<T>(ILInstruction op)
		{
			T result;

			switch (op.OperandType)
			{
				case OperandType.None:
					{
						result = default(T);
						break;
					}

				case OperandType.Immediate:
					{
						result = (T)op.Operand;
						break;
					}

				case OperandType.UserString:
					{
						var handle = (SRM.UserStringHandle)op.Operand;
						result = (T)(object)metadata.GetUserString(handle);
						break;
					}

				case OperandType.FieldDefinition:
					{
						var handle = (SRM.FieldDefinitionHandle)op.Operand;
						result = (T)(object)GetDefinedField(handle);
						break;
					}

				case OperandType.TypeReference:
					{
						var handle = (SRM.TypeReferenceHandle)op.Operand;
						result = (T)signatureTypeProvider.GetTypeFromReference(metadata, handle);
						break;
					}

				case OperandType.TypeSpecification:
					{
						var handle = (SRM.TypeSpecificationHandle)op.Operand;
						result = (T)signatureTypeProvider.GetTypeFromSpecification(metadata, defGenericContext, handle);
						break;
					}

				case OperandType.MethodDefinition:
					{
						var handle = (SRM.MethodDefinitionHandle)op.Operand;
						result = (T)(object)GetDefinedMethod(handle);
						break;
					}

				case OperandType.MemberReference:
					{
						var handle = (SRM.MemberReferenceHandle)op.Operand;
						result = (T)GetMemberReference(handle);
						break;
					}

				case OperandType.MethodSpecification:
					{
						var handle = (SRM.MethodSpecificationHandle)op.Operand;
						result = (T)GetMethodReference(handle);
						break;
					}

				default:
					throw op.OperandType.ToUnknownValueException();
			}

			return result;
		}

		private ITypeMemberReference GetMemberReference(SRM.MemberReferenceHandle handle)
		{
			ITypeMemberReference result;
			var member = metadata.GetMemberReference(handle);
			var kind = member.GetKind();

			switch (kind)
			{
				case SRM.MemberReferenceKind.Field:
					result = GetFieldReference(member);
					break;

				case SRM.MemberReferenceKind.Method:
					result = GetMethodReference(member);
					break;

				default:
					throw kind.ToUnknownValueException();
			}

			return result;
		}

		private IFieldReference GetFieldReference(SRM.MemberReference member)
		{
			var name = metadata.GetString(member.Name);
			var containingType = (IBasicType)signatureTypeProvider.GetTypeFromHandle(metadata, defGenericContext, member.Parent);

			CreateGenericParameterReferences(GenericParameterKind.Type, containingType.GenericParameterCount);
			var type = member.DecodeFieldSignature(signatureTypeProvider, refGenericContext);
			var signatureReader = metadata.GetBlobReader(member.Signature);
			var signatureHeader = signatureReader.ReadSignatureHeader();
			var field = new FieldReference(name, type)
			{
				ContainingType = containingType,
				IsStatic = !signatureHeader.IsInstance
			};

			BindGenericParameterReferences(GenericParameterKind.Type, containingType);
			return field;
		}

		private IMethodReference GetMethodReference(SRM.MemberReference member)
		{
			var name = metadata.GetString(member.Name);
			var type = signatureTypeProvider.GetTypeFromHandle(metadata, defGenericContext, member.Parent);

			if (type is ArrayType)
			{
				type = new FakeArrayType(type as ArrayType);
			}

			var containingType = (IBasicType)type;

			CreateGenericParameterReferences(GenericParameterKind.Type, containingType.GenericParameterCount);
			var signature = member.DecodeMethodSignature(signatureTypeProvider, refGenericContext);

			var method = new MethodReference(name, signature.ReturnType)
			{
				ContainingType = containingType,
				GenericParameterCount = signature.GenericParameterCount,
				IsStatic = !signature.Header.IsInstance
			};

			method.Resolve(this.Host);

			for (var i = 0; i < signature.ParameterTypes.Length; ++i)
			{
				var parameterType = signature.ParameterTypes[i];
				var parameter = new MethodParameterReference((ushort)i, parameterType);

				method.Parameters.Add(parameter);
			}

			BindGenericParameterReferences(GenericParameterKind.Type, containingType);
			return method;
		}

		private IMethodReference GetMethodReference(SRM.MethodSpecificationHandle handle)
		{
			var methodspec = metadata.GetMethodSpecification(handle);
			var genericArguments = methodspec.DecodeSignature(signatureTypeProvider, defGenericContext);

			CreateGenericParameterReferences(GenericParameterKind.Method, genericArguments.Length);

			var method = GetMethodReference(methodspec.Method);
			method = method.Instantiate(genericArguments);

			BindGenericParameterReferences(GenericParameterKind.Method, method);
			return method;
		}

		private IMethodReference GetMethodReference(SRM.EntityHandle handle)
		{
			IMethodReference result;

			switch (handle.Kind)
			{
				case SRM.HandleKind.MethodDefinition:
					var defHandle = (SRM.MethodDefinitionHandle)handle;
					result = GetDefinedMethod(defHandle);
					break;

				case SRM.HandleKind.MethodSpecification:
					var specHandle = (SRM.MethodSpecificationHandle)handle;
					result = GetMethodReference(specHandle);
					break;

				case SRM.HandleKind.MemberReference:
					var memberHandle = (SRM.MemberReferenceHandle)handle;
					var member = metadata.GetMemberReference(memberHandle);
					result = GetMethodReference(member);
					break;

				default:
					throw handle.Kind.ToUnknownValueException();
			}

			return result;
		}

		private void CreateGenericParameterReferences(GenericParameterKind kind, int genericParameterCount)
		{
			IList<IGenericParameterReference> parameters;

			switch (kind)
			{
				case GenericParameterKind.Type:
					parameters = refGenericContext.TypeParameters;
					break;

				case GenericParameterKind.Method:
					parameters = refGenericContext.MethodParameters;
					break;

				default:
					throw kind.ToUnknownValueException();
			}

			for (var i = 0; i < genericParameterCount; ++i)
			{
				var genericParameterReference = new GenericParameterReference(kind, (ushort)i);
				parameters.Add(genericParameterReference);
			}
		}

		private void BindGenericParameterReferences(GenericParameterKind kind, IGenericReference genericContainer)
		{
			IList<IGenericParameterReference> parameters;

			switch (kind)
			{
				case GenericParameterKind.Type:
					parameters = refGenericContext.TypeParameters;
					break;

				case GenericParameterKind.Method:
					parameters = refGenericContext.MethodParameters;
					break;

				default:
					throw kind.ToUnknownValueException();
			}

			foreach (var parameter in parameters)
			{
				var param = parameter as GenericParameterReference;
				param.GenericContainer = genericContainer;
			}

			parameters.Clear();
		}

		private IInstruction ProcessSwitch(ILInstruction op)
		{
			var targets = GetOperand<uint[]>(op);

			var instruction = new SwitchInstruction(op.Offset, targets);
			return instruction;
		}

		private IInstruction ProcessCreateArray(ILInstruction op, ArrayType arrayType = null, bool withLowerBounds = false)
		{
			if (arrayType == null)
			{
				var elementsType = GetOperand<IType>(op);
				arrayType = new ArrayType(elementsType);
			}

			var instruction = new CreateArrayInstruction(op.Offset, arrayType);
			instruction.WithLowerBound = withLowerBounds;
			return instruction;
		}

		private IInstruction ProcessLoadArrayElement(ILInstruction op, ArrayType arrayType, LoadArrayElementOperation operation)
		{
			var instruction = new LoadArrayElementInstruction(op.Offset, operation, arrayType);
			return instruction;
		}

		private IInstruction ProcessStoreArrayElement(ILInstruction op, ArrayType arrayType)
		{
			var instruction = new StoreArrayElementInstruction(op.Offset, arrayType);
			return instruction;
		}

		private IInstruction ProcessCreateObject(ILInstruction op)
		{
			var method = GetOperand<IMethodReference>(op);
			IInstruction instruction;

			if (method.ContainingType is FakeArrayType)
			{
				var arrayType = (FakeArrayType)method.ContainingType;
				var withLowerBounds = method.Parameters.Count > arrayType.Type.Rank;

				instruction = ProcessCreateArray(op, arrayType.Type, withLowerBounds);
			}
			else
			{
				instruction = new CreateObjectInstruction(op.Offset, method);
			}

			return instruction;
		}

		private IInstruction ProcessMethodCall(ILInstruction op)
		{
			var method = GetOperand<IMethodReference>(op);
			IInstruction instruction;

			if (method.ContainingType is FakeArrayType)
			{
				var arrayType = (FakeArrayType)method.ContainingType;

				if (method.Name == "Set")
				{
					instruction = ProcessStoreArrayElement(op, arrayType.Type);
				}
				else
				{
					var operation = OperationHelper.ToLoadArrayElementOperation(method.Name);

					instruction = ProcessLoadArrayElement(op, arrayType.Type, operation);
				}
			}
			else
			{
				var operation = OperationHelper.ToMethodCallOperation(op.Opcode);

				instruction = new MethodCallInstruction(op.Offset, operation, method);
			}

			return instruction;
		}

		private IInstruction ProcessMethodCallIndirect(ILInstruction op)
		{
			var functionPointerType = GetOperand<FunctionPointerType>(op);

			var instruction = new IndirectMethodCallInstruction(op.Offset, functionPointerType);
			return instruction;
		}

		private IInstruction ProcessSizeof(ILInstruction op)
		{
			var type = GetOperand<IType>(op);

			var instruction = new SizeofInstruction(op.Offset, type);
			return instruction;
		}

		private IInstruction ProcessUnaryConditionalBranch(ILInstruction op)
		{
			var operation = OperationHelper.ToBranchOperation(op.Opcode);
			var target = GetOperand<uint>(op);

			var instruction = new BranchInstruction(op.Offset, operation, target);
			return instruction;
		}

		private IInstruction ProcessBinaryConditionalBranch(ILInstruction op)
		{
			var operation = OperationHelper.ToBranchOperation(op.Opcode);
			var unsigned = OperationHelper.OperandsAreUnsigned(op.Opcode);
			var target = GetOperand<uint>(op);

			var instruction = new BranchInstruction(op.Offset, operation, target);
			instruction.UnsignedOperands = unsigned;
			return instruction;
		}

		private IInstruction ProcessLeave(ILInstruction op)
		{
			var target = GetOperand<uint>(op);

			var instruction = new BranchInstruction(op.Offset, BranchOperation.Leave, target);
			return instruction;
		}

		private IInstruction ProcessUnconditionalBranch(ILInstruction op)
		{
			var target = GetOperand<uint>(op);

			var instruction = new BranchInstruction(op.Offset, BranchOperation.Branch, target);
			return instruction;
		}

		private IInstruction ProcessLoadConstant(ILInstruction op)
		{
			var operation = OperationHelper.ToLoadOperation(op.Opcode);
			var type = OperationHelper.GetOperationType(op.Opcode);
            var value = GetOperand<object>(op);
			var source = new Constant(value) { Type = type };

			var instruction = new LoadInstruction(op.Offset, operation, source);
			return instruction;
		}

		private IInstruction ProcessLoadArgument(ILInstruction op)
		{
			var operation = OperationHelper.ToLoadOperation(op.Opcode);
			var parameterIndex = GetOperand<int>(op);
			var source = currentMethod.Body.Parameters[parameterIndex];

			var instruction = new LoadInstruction(op.Offset, operation, source);
			return instruction;
		}

		private IInstruction ProcessLoadLocal(ILInstruction op)
		{
			var operation = OperationHelper.ToLoadOperation(op.Opcode);
			var localIndex = GetOperand<int>(op);
			var source = currentMethod.Body.LocalVariables[localIndex];

			var instruction = new LoadInstruction(op.Offset, operation, source);
			return instruction;
		}

		private IInstruction ProcessLoadIndirect(ILInstruction op)
		{
			var instruction = new BasicInstruction(op.Offset, BasicOperation.IndirectLoad);
			return instruction;
		}

		private IInstruction ProcessLoadField(ILInstruction op)
		{
			var operation = OperationHelper.ToLoadFieldOperation(op.Opcode);
			var field = GetOperand<IFieldReference>(op);

			var instruction = new LoadFieldInstruction(op.Offset, operation, field);
			return instruction;
		}

		private IInstruction ProcessLoadMethodAddress(ILInstruction op)
		{
			var operation = OperationHelper.ToLoadMethodAddressOperation(op.Opcode);
			var method = GetOperand<IMethodReference>(op);

			var instruction = new LoadMethodAddressInstruction(op.Offset, operation, method);
			return instruction;
		}

		private IInstruction ProcessLoadToken(ILInstruction op)
		{
			var token = GetOperand<IMetadataReference>(op);

			var instruction = new LoadTokenInstruction(op.Offset, token);
			return instruction;
		}

		private IInstruction ProcessStoreArgument(ILInstruction op)
		{
			var parameterIndex = GetOperand<int>(op);
			var dest = currentMethod.Body.Parameters[parameterIndex];

			var instruction = new StoreInstruction(op.Offset, dest);
			return instruction;
		}

		private IInstruction ProcessStoreLocal(ILInstruction op)
		{
			var localIndex = GetOperand<int>(op);
			var dest = currentMethod.Body.LocalVariables[localIndex];

			var instruction = new StoreInstruction(op.Offset, dest);
			return instruction;
		}

		private IInstruction ProcessStoreField(ILInstruction op)
		{
			var field = GetOperand<IFieldReference>(op);

			var instruction = new StoreFieldInstruction(op.Offset, field);
			return instruction;
		}

		private IInstruction ProcessBasic(ILInstruction op)
		{
			var operation = OperationHelper.ToBasicOperation(op.Opcode);
			var overflow = OperationHelper.PerformsOverflowCheck(op.Opcode);
			var unsigned = OperationHelper.OperandsAreUnsigned(op.Opcode);

			var instruction = new BasicInstruction(op.Offset, operation);
			instruction.OverflowCheck = overflow;
			instruction.UnsignedOperands = unsigned;
			return instruction;
		}

		private IInstruction ProcessConversion(ILInstruction op)
		{
			var operation = OperationHelper.ToConvertOperation(op.Opcode);
			var overflow = OperationHelper.PerformsOverflowCheck(op.Opcode);
			var unsigned = OperationHelper.OperandsAreUnsigned(op.Opcode);
			var type = GetOperand<IType>(op);

			if (operation == ConvertOperation.Box && type.TypeKind == TypeKind.ValueType)
			{
				type = PlatformTypes.Object;
			}
			else if (operation == ConvertOperation.Conv)
			{
				type = OperationHelper.GetOperationType(op.Opcode);
			}

			var instruction = new ConvertInstruction(op.Offset, operation, type);
			instruction.OverflowCheck = overflow;
			instruction.UnsignedOperands = unsigned;
			return instruction;
		}

		#endregion

		private static string GetGenericName(string name)
		{
			var start = name.LastIndexOf('`');

			if (start > -1)
			{
				name = name.Remove(start);
			}

			return name;
		}
	}
}