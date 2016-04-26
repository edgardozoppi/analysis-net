using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Types
{
	public enum TypeModifier
	{
		Pointer,
		Array
	}

	public static class TypeHelper
	{
		public static IEnumerable<ITypeDefinition> GetAllTypes(this Namespace self)
		{
			var typeContainer = self as ITypeDefinitionContainer;
			var types = typeContainer.GetAllTypes();
			var nestedNamespacesTypes = self.Namespaces.SelectMany(n => n.GetAllTypes());
			var result = types.Union(nestedNamespacesTypes);
			return result;
		}

		public static IEnumerable<ITypeDefinition> GetAllTypes(this ITypeDefinitionContainer self)
		{
			var types = self.Types;
			var nestedTypes = self.Types.OfType<ITypeDefinitionContainer>()
										.SelectMany(t => t.GetAllTypes());
			var result = types.Union(nestedTypes);
			return result;
		}

		public static IType TokenType(IMetadataReference token)
		{
			IType type = PlatformTypes.Unknown;

			if (token is IMethodReference)
			{
				type = PlatformTypes.RuntimeMethodHandle;
			}
			else if (token is IType)
			{
				type = PlatformTypes.RuntimeTypeHandle;
			}
			else if (token is IFieldReference)
			{
				type = PlatformTypes.RuntimeFieldHandle;
			}

			return type;
		}

		public static bool IsContainer(IType type)
		{
			throw new NotImplementedException();

			//var result = false;

			//if (type is SpecializedType)
			//{
			//	var specializedType = type as SpecializedType;
			//	type = specializedType.GenericType;
			//}

			//var typedef = TypeHelper.Resolve(type, host);

			//if (typedef != null)
			//{
			//	result = TypeHelper.Type1ImplementsType2(typedef, host.PlatformType.SystemCollectionsICollection);
			//	result = result || TypeHelper.Type1ImplementsType2(typedef, host.PlatformType.SystemCollectionsGenericICollection);
			//}

			//return result;
		}

		public static IType MergedType(IType type1, IType type2)
		{
			IType result = PlatformTypes.Object;

			if (type1 is PointerType && type2 is PointerType)
			{
				var pointerType1 = type1 as PointerType;
				var pointerType2 = type2 as PointerType;
				type1 = pointerType1.TargetType;
				type2 = pointerType2.TargetType;

				var targetType = MergedType(type1, type2);
				result = new PointerType(targetType);
			}
			else if (type1 is ArrayType && type2 is ArrayType)
			{
				var arrayType1 = type1 as ArrayType;
				var arrayType2 = type2 as ArrayType;
				type1 = arrayType1.ElementsType;
				type2 = arrayType2.ElementsType;

				var elementsType = MergedType(type1, type2);
				result = new ArrayType(elementsType);
			}
			else if (type1 is BasicType && type2 is BasicType)
			{
				var basicType1 = type1 as BasicType;
				var basicType2 = type2 as BasicType;

				result = MergedType(basicType1, basicType2);
			}

			return result;
		}

		/// <summary>
		/// If both type references can be resolved, this returns the merged type of two types as per the verification algorithm in CLR.
		/// Otherwise it returns either type1, or type2 or System.Object, depending on how much is known about either type.
		/// </summary>
		public static IType MergedType(BasicType type1, BasicType type2)
		{
			if (TypesAreEquivalent(type1, type2)) return type1;
			if (StackTypesAreEquivalent(type1, type2)) return StackType(type1);

			var typedef1 = type1.ResolvedType;
			var typedef2 = type2.ResolvedType;

			if (typedef1 != null && typedef2 != null)
			{
				return MergedType(typedef1, typedef2);
			}

			if (typedef1 != null && Type1ImplementsType2(typedef1, type2)) return type2;
			else if (typedef2 != null && Type1ImplementsType2(typedef2, type1)) return type1;

			return PlatformTypes.Object;
		}

		/// <summary>
		/// Returns the merged type of two types as per the verification algorithm in CLR.
		/// If the types cannot be merged, then it returns System.Object.
		/// </summary>
		public static IType MergedType(ITypeDefinition type1, ITypeDefinition type2)
		{
			if (TypesAreEquivalent(type1, type2)) return type1;
			if (StackTypesAreEquivalent(type1, type2)) return StackType(type1);
			if (TypesAreAssignmentCompatible(type1, type2)) return type2;
			if (TypesAreAssignmentCompatible(type2, type1)) return type1;

			var mdcbc = MostDerivedCommonBaseClass(type1, type2);
			if (mdcbc != null) return mdcbc;

			return PlatformTypes.Object;
		}

		public static bool TypesAreEquivalent(IType type1, IType type2)
		{
			if (type1 == null || type2 == null) return false;
			if (type1 == type2) return true;
			if (type1.Equals(type2)) return true;
			return false;
		}

		public static bool TypesAreEquivalent(ITypeDefinition type1, ITypeDefinition type2)
		{
			if (type1 == null || type2 == null) return false;
			if (type1 == type2) return true;
			if (type1.Equals(type2)) return true;
			return false;
		}

		/// <summary>
		/// Returns the stack state type used by the CLR verification algorithm when merging control flow
		/// paths. For example, both signed and unsigned 16-bit integers are treated as the same as signed 32-bit
		/// integers for the purposes of verifying that stack state merges are safe.
		/// </summary>
		public static IType StackType(IType type)
		{
			switch (type.TypeCode)
			{
				case PrimitiveTypeCode.Boolean:
				case PrimitiveTypeCode.Char:
				case PrimitiveTypeCode.Int16:
				case PrimitiveTypeCode.Int32:
				case PrimitiveTypeCode.Int8:
				case PrimitiveTypeCode.UInt16:
				case PrimitiveTypeCode.UInt32:
				case PrimitiveTypeCode.UInt8:
					return type.PlatformType.SystemInt32;

				case PrimitiveTypeCode.Int64:
				case PrimitiveTypeCode.UInt64:
					return type.PlatformType.SystemInt64;

				case PrimitiveTypeCode.Float32:
				case PrimitiveTypeCode.Float64:
					return type.PlatformType.SystemFloat64;

				case PrimitiveTypeCode.IntPtr:
				case PrimitiveTypeCode.UIntPtr:
					return type.PlatformType.SystemIntPtr;

				case PrimitiveTypeCode.NotPrimitive:
					if (type.IsEnum)
					{
						return StackType(type.ResolvedType.UnderlyingType);
					}
					break;
			}

			return type;
		}

		/// <summary>
		/// Returns true if the stack state types of the given two types are to be considered equivalent for the purpose of signature matching and so on.
		/// </summary>
		public static bool StackTypesAreEquivalent(IType type1, IType type2)
		{
			var stackType1 = StackType(type1);
			var stackType2 = StackType(type2);

			var result = TypesAreEquivalent(stackType1, stackType2);
			return result;
		}

		public static List<TypeModifier> TypeModifiers(IType type)
		{
			var result = new List<TypeModifier>();

			while (type != null)
			{
				if (type is PointerType)
				{
					var pointerType = type as PointerType;
					type = pointerType.TargetType;
					result.Add(TypeModifier.Pointer);
				}
				else if (type is ArrayType)
				{
					var arrayType = type as ArrayType;
					type = arrayType.ElementsType;
					result.Add(TypeModifier.Array);
				}
				else
				{
					type = null;
				}
			}

			return result;
		}

		/// <summary>
		/// Returns true if the given type definition, or one of its base types, implements the given interface or an interface
		/// that derives from the given interface.
		/// </summary>
		public static bool Type1ImplementsType2(ITypeDefinition type1, IType type2)
		{
			IEnumerable<BasicType> interfaces = null;
			BasicType baseType = null;

			if (type1 is ClassDefinition)
			{
				var classdef = type1 as ClassDefinition;
				interfaces = classdef.Interfaces;
				baseType = classdef.Base;
			}
			else if (type1 is StructDefinition)
			{
				var structdef = type1 as StructDefinition;
				interfaces = structdef.Interfaces;
			}

			if (interfaces != null)
			{
				foreach (var implementedInterface in interfaces)
				{
					if (TypesAreEquivalent(implementedInterface, type2))
						return true;

					if (implementedInterface.ResolvedType != null &&
						Type1ImplementsType2(implementedInterface.ResolvedType, type2))
						return true;
				}
			}

			if (baseType != null && baseType.ResolvedType != null &&
				Type1ImplementsType2(baseType.ResolvedType, type2))
				return true;

			return false;
		}

		public static IType BinaryNumericOperationType(IType type1, IType type2, bool unsigned)
		{
			throw new NotImplementedException();
		}

		public static IType BinaryLogicalOperationType(IType type1, IType type2)
		{
			throw new NotImplementedException();
		}
	}
}
