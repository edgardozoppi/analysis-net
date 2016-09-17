// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
	public static class Extensions
	{
		public static void AddRange<T>(this ICollection<T> self, IEnumerable<T> elements)
		{
			foreach (var element in elements)
			{
				self.Add(element);
			}
		}

		public static void RemoveAll<T>(this ICollection<T> self, IEnumerable<T> elements)
		{
			foreach (var element in elements)
			{
				self.Remove(element);
			}
		}

		public static void SetRange<K, V>(this IDictionary<K, V> self, IEnumerable<KeyValuePair<K, V>> elements)
		{
			foreach (var element in elements)
			{
				self[element.Key] = element.Value;
			}
		}

		public static IEnumerable<T> ToEnumerable<T>(this T item)
		{
			yield return item;
		}

		public static UnknownValueException<T> ToUnknownValueException<T>(this T self)
			where T : struct
		{
			return new UnknownValueException<T>(self);
		}

		internal static bool BothNullOrMatchReference(this ITypeDefinition def, IBasicType @ref)
		{
			return (def == null && @ref == null) ||
				   (def != null && @ref != null && def.MatchReference(@ref));
		}

		public static int TotalGenericParameterCount(this IBasicType type)
		{
			var result = 0;

			while (type != null)
			{
				result += type.GenericParameterCount;
				type = type.ContainingType;
			}

			return result;
		}

		public static string GetMetadataName(this IBasicType type)
		{
			var name = type.Name;

			if (type.GenericParameterCount > 0)
			{
				name = string.Format("{0}´{1}", name, type.GenericParameterCount);
			}

			return name;
		}

		public static string GetFullNameWithAssembly(this IBasicType type)
		{
			var fullName = type.GetFullName();
			var containingAssembly = string.Empty;

			if (type.ContainingAssembly != null)
			{
				containingAssembly = string.Format("[{0}]", type.ContainingAssembly.Name);
			}

			return string.Format("{0}{1}", containingAssembly, fullName);
		}

		public static string GetFullName(this IBasicType type)
		{
			var containingNamespace = string.Empty;
			var containingTypes = string.Empty;
			var containingType = type.ContainingType;

			if (!string.IsNullOrEmpty(type.ContainingNamespace))
			{
				containingNamespace = string.Format("{0}.", type.ContainingNamespace);
			}

            while (containingType != null)
            {
                containingTypes = string.Format("{0}{1}.", containingTypes, containingType);
				containingType = type.ContainingType;
            }

			return string.Format("{0}{1}{2}", containingNamespace, containingTypes, type.GenericName);
		}

        public static bool MatchType(this IType definitionType, IType referenceType, IDictionary<IType, IType> typeParameterBinding)
        {
            var result = false;

            if (definitionType is IGenericParameterReference)
            {
                IType typeArgument;

                if (typeParameterBinding != null &&
                    typeParameterBinding.TryGetValue(definitionType, out typeArgument))
                {
                    definitionType = typeArgument;
                }
            }

            result = definitionType.Equals(referenceType);
            return result;
        }
	}
}
