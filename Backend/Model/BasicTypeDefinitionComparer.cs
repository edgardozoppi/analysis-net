// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Model
{
	public class TypeDefinitionComparer : IEqualityComparer<ITypeReference>
	{
		public int GetHashCode(ITypeReference type)
		{
			return type.GetHashCode();
		}

		public bool Equals(ITypeReference x, ITypeReference y)
		{
			bool result;
			var xdef = x as ITypeDefinition;
			var ydef = y as ITypeDefinition;

			if (xdef != null && ydef == null)
			{
				result = y.ResolvedType.Equals(xdef);
			}
			else if (xdef == null && ydef != null)
			{
				result = x.ResolvedType.Equals(ydef);
			}
			else
			{
				result = x.Equals(y);
			}

			return result;
		}
	}
}
