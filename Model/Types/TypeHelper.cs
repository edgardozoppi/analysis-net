using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Types
{
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

		/// <summary>
		/// If both type references can be resolved, this returns the merged type of two types as per the verification algorithm in CLR.
		/// Otherwise it returns either type1, or type2 or System.Object, depending on how much is known about either type.
		/// </summary>
		public static IType MergedType(IType type1, IType type2)
		{
			throw new NotImplementedException();

			//if (TypesAreEquivalent(type1, type2)) return type1;
			//if (StackTypesAreEquivalent(type1, type2)) return StackType(type1);
			//var typedef1 = type1.ResolvedType;
			//var typedef2 = type2.ResolvedType;

			//if (typedef1 != null && typedef2 != null)
			//{
			//	return MergedType(typedef1, typedef2);
			//}

			//if (typedef1 != null && Type1ImplementsType2(typedef1, type2)) return type2;
			//else if (typedef2 != null && Type1ImplementsType2(typedef2, type1)) return type1;

			//return PlatformTypes.Object;
		}

		/// <summary>
		/// Returns the merged type of two types as per the verification algorithm in CLR.
		/// If the types cannot be merged, then it returns System.Object.
		/// </summary>
		public static IType MergedType(ITypeDefinition type1, ITypeDefinition type2)
		{
			throw new NotImplementedException();

			//if (TypesAreEquivalent(type1, type2)) return type1;
			//if (StackTypesAreEquivalent(type1, type2)) return StackType(type1);
			//if (TypesAreAssignmentCompatible(type1, type2))	return type2;
			//if (TypesAreAssignmentCompatible(type2, type1))	return type1;

			//var mdcbc = MostDerivedCommonBaseClass(type1, type2);
			//if (mdcbc != null) return mdcbc;

			//return PlatformTypes.Object;
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
