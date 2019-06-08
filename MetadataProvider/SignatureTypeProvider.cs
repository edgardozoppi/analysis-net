using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using SRM = System.Reflection.Metadata;

namespace MetadataProvider
{
	internal class GenericContext
	{
		public IList<IGenericParameterReference> TypeParameters { get; private set; }
		public IList<IGenericParameterReference> MethodParameters { get; private set; }

		public GenericContext()
		{
			this.TypeParameters = new List<IGenericParameterReference>();
			this.MethodParameters = new List<IGenericParameterReference>();
		}

		public void Clear()
		{
			this.TypeParameters.Clear();
			this.MethodParameters.Clear();
		}

		public override string ToString()
		{
			return string.Format("Type: {0}, Method: {1}", this.TypeParameters.Count, this.MethodParameters.Count);
		}
	}

	internal class SignatureTypeProvider : SRM.ISignatureTypeProvider<IType, GenericContext>
	{
		private AssemblyExtractor extractor;

		public SignatureTypeProvider(AssemblyExtractor extractor)
		{
			this.extractor = extractor;
		}

		public virtual IType GetPrimitiveType(SRM.PrimitiveTypeCode typeCode)
		{
			var result = TypeHelper.ToType(typeCode);
			return result;
		}

		public virtual IType GetTypeFromDefinition(SRM.MetadataReader reader, SRM.TypeDefinitionHandle handle, byte rawTypeKind = 0)
		{
			var result = extractor.GetDefinedType(handle);
			return result;
		}

		public virtual IType GetTypeFromReference(SRM.MetadataReader reader, SRM.TypeReferenceHandle handle, byte rawTypeKind = 0)
		{
			var typeref = reader.GetTypeReference(handle);
			var namespaze = reader.GetString(typeref.Namespace);
			var name = reader.GetString(typeref.Name);
			var genericParameterCount = ExtractGenericParameterCount(ref name);

			var type = new BasicType(name)
			{
				ContainingNamespace = namespaze,
				GenericParameterCount = genericParameterCount
			};

			type.Resolve(extractor.Host);

			switch (typeref.ResolutionScope.Kind)
			{
				case SRM.HandleKind.AssemblyReference:
					{
						var assemblyHandle = (SRM.AssemblyReferenceHandle)typeref.ResolutionScope;
						var assembly = reader.GetAssemblyReference(assemblyHandle);
						name = reader.GetString(assembly.Name);
						type.ContainingAssembly = new AssemblyReference(name);
						break;
					}

				//case SRM.HandleKind.ModuleReference:
				//	{
				//		var moduleHandle = (SRM.ModuleReferenceHandle)typeref.ResolutionScope;
				//		var module = reader.GetModuleReference(moduleHandle);
				//		name = reader.GetString(module.Name);
				//		type.ContainingAssembly = new AssemblyReference(name);
				//		break;
				//	}

				case SRM.HandleKind.TypeReference:
					{
						var containingTypeHandle = (SRM.TypeReferenceHandle)typeref.ResolutionScope;
						var containingType = GetTypeFromReference(reader, containingTypeHandle);
						type.ContainingType = (IBasicType)containingType;
						type.ContainingAssembly = type.ContainingType.ContainingAssembly;
						break;
					}

				case SRM.HandleKind.TypeDefinition:
					{
						var containingTypeHandle = (SRM.TypeDefinitionHandle)typeref.ResolutionScope;
						var containingType = GetTypeFromDefinition(reader, containingTypeHandle);
						type.ContainingType = (IBasicType)containingType;
						type.ContainingAssembly = type.ContainingType.ContainingAssembly;
						break;
					}

				default:
					throw typeref.ResolutionScope.Kind.ToUnknownValueException();
			}

			return type;
		}

		private static int ExtractGenericParameterCount(ref string name)
		{
			var result = 0;
			var start = name.LastIndexOf('`');

			if (start > -1)
			{
				var count = name.Substring(start + 1);
				result = Convert.ToInt32(count);
				name = name.Remove(start);
			}

			return result;
		}

		public virtual IType GetTypeFromSpecification(SRM.MetadataReader reader, GenericContext genericContext, SRM.TypeSpecificationHandle handle, byte rawTypeKind = 0)
		{
			var typespec = reader.GetTypeSpecification(handle);
			var result = typespec.DecodeSignature(this, genericContext);
			return result;
		}

		public virtual IType GetSZArrayType(IType elementsType)
		{
			var result = new ArrayType(elementsType, 1);
			return result;
		}

		public virtual IType GetPointerType(IType targetType)
		{
			var result = new PointerType(targetType);
			return result;
		}

		public virtual IType GetByReferenceType(IType targetType)
		{
			var result = new PointerType(targetType);
			return result;
		}

		public virtual IType GetGenericMethodParameter(GenericContext genericContext, int index)
		{
			return genericContext.MethodParameters[index];
		}

		public virtual IType GetGenericTypeParameter(GenericContext genericContext, int index)
		{
			return genericContext.TypeParameters[index];
		}

		public virtual IType GetPinnedType(IType targetType)
		{
			throw new NotImplementedException();
		}

		public virtual IType GetGenericInstantiation(IType genericType, ImmutableArray<IType> genericArguments)
		{
			var result = genericType as IBasicType;
			result = result.Instantiate(genericArguments);
			return result;
		}

		public virtual IType GetArrayType(IType elementsType, SRM.ArrayShape shape)
		{
			var result = new ArrayType(elementsType, (uint)shape.Rank);
			return result;
		}

		public virtual IType GetTypeFromHandle(SRM.MetadataReader reader, GenericContext genericContext, SRM.EntityHandle handle)
		{
			IType result = null;

			if (!handle.IsNil)
			{
				switch (handle.Kind)
				{
					case SRM.HandleKind.TypeDefinition:
						result = GetTypeFromDefinition(reader, (SRM.TypeDefinitionHandle)handle);
						break;

					case SRM.HandleKind.TypeReference:
						result = GetTypeFromReference(reader, (SRM.TypeReferenceHandle)handle);
						break;

					case SRM.HandleKind.TypeSpecification:
						result = GetTypeFromSpecification(reader, genericContext, (SRM.TypeSpecificationHandle)handle);
						break;

					default:
						throw handle.Kind.ToUnknownValueException();
				}
			}

			return result;
		}

		public virtual IType GetModifiedType(IType modifierType, IType unmodifiedType, bool isRequired)
		{
			throw new NotImplementedException();
		}

		public virtual IType GetFunctionPointerType(SRM.MethodSignature<IType> signature)
		{
			// TODO: Not sure if FunctionPointerType should have GenericParameterCount property.
			var result = new FunctionPointerType(signature.ReturnType)
			{
				IsStatic = !signature.Header.IsInstance,
			};

			for (var i = 0; i < signature.ParameterTypes.Length; ++i)
			{
				var parameterType = signature.ParameterTypes[i];
				var parameter = new MethodParameterReference((ushort)i, parameterType);

				result.Parameters.Add(parameter);
			}

			return result;
		}
	}
}