// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Types
{
	public class MethodReferenceDefinitionComparer : IEqualityComparer<IMethodReference>
	{
		private static MethodReferenceDefinitionComparer _default;

		public static MethodReferenceDefinitionComparer Default
		{
			get
			{
				if (_default == null)
				{
					_default = new MethodReferenceDefinitionComparer();
				}

				return _default;
			}
		}

		private MethodReferenceDefinitionComparer()
		{
			// Don't create a new instance of this class,
			// use Default property instead.
		}

		public int GetHashCode(IMethodReference method)
		{
			return method.Name.GetHashCode();
		}

		public bool Equals(IMethodReference x, IMethodReference y)
		{
			bool result;
			var xdef = x as MethodDefinition;
			var ydef = y as MethodDefinition;

			if (xdef != null && ydef == null)
			{
				result = xdef.MatchReference(y);
			}
			else if (xdef == null && ydef != null)
			{
				result = ydef.MatchReference(x);
			}
			else
			{
				result = x.Equals(y);
			}

			return result;
		}
	}
}
