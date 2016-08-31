using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Types
{
	public class MethodReferenceDefinitionComparer : IEqualityComparer<IMethodReference>
	{
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
