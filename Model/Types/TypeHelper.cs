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
	}
}
