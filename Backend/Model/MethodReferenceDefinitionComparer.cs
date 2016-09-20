// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Model
{
	public class MethodReferenceDefinitionComparer : IEqualityComparer<IMethodReference>
	{
		public int GetHashCode(IMethodReference method)
		{
			return method.GetHashCode();
		}

		public bool Equals(IMethodReference x, IMethodReference y)
		{
			bool result;
			var xdef = x as IMethodDefinition;
			var ydef = y as IMethodDefinition;

			if (xdef != null && ydef == null)
			{
				result = y.ResolvedMethod.Equals(xdef);
			}
			else if (xdef == null && ydef != null)
			{
				result = x.ResolvedMethod.Equals(ydef);
			}
			else
			{
				result = x.Equals(y);
			}

			return result;
		}
	}
}
