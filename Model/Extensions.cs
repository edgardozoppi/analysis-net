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

		public static string GetContainingTypes(this ITypeDefinition type)
		{
			var result = new List<string>();

			while (type.ContainingType != null)
			{
				type = type.ContainingType;
				result.Insert(0, type.Name);
			}

			return string.Join(".", result);
		}

		public static string GetFullNameWithAssembly(this IBasicType type)
		{
			var containingAssembly = string.Empty;
			var fullName = type.GetFullName();

			if (type.ContainingAssembly != null)
			{
				containingAssembly = string.Format("[{0}]", type.ContainingAssembly.Name);
			}

			return string.Format("{0}{1}", containingAssembly, fullName);
		}

		public static string GetFullName(this IBasicType type)
		{
			var containingAssembly = string.Empty;
			var containingNamespace = string.Empty;
			var containingTypes = string.Empty;

			if (type.ContainingAssembly != null)
			{
				containingAssembly = string.Format("[{0}]", type.ContainingAssembly.Name);
			}

			if (!string.IsNullOrEmpty(type.ContainingNamespace))
			{
				containingNamespace = string.Format("{0}.", type.ContainingNamespace);
			}

            if(!string.IsNullOrEmpty(type.ContainingTypes))
            {
                containingTypes = string.Format("{0}.", type.ContainingTypes);
            }

			return string.Format("{0}{1}{2}{3}", containingAssembly, containingNamespace, containingTypes, type.GenericName);
		}

        public static bool MatchType(this IType definitionType, IType referenceType, IDictionary<IType, IType> typeParameterBinding)
        {
            var result = false;

            if (definitionType is TypeVariable)
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
