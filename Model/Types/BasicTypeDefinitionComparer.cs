using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Types
{
	public class BasicTypeDefinitionComparer : IEqualityComparer<IBasicType>
	{
		public int GetHashCode(IBasicType type)
		{
			return type.Name.GetHashCode();
		}

		public bool Equals(IBasicType x, IBasicType y)
		{
			bool result;
			var xdef = x as ITypeDefinition;
			var ydef = y as ITypeDefinition;

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
