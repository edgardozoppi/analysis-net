using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Types
{
	public class BasicTypeDefinitionComparer : IEqualityComparer<IBasicType>
	{
		private static BasicTypeDefinitionComparer _default;

		public static BasicTypeDefinitionComparer Default
		{
			get
			{
				if (_default == null)
				{
					_default = new BasicTypeDefinitionComparer();
				}

				return _default;
			}
		}

		private BasicTypeDefinitionComparer()
		{
			// Don't create a new instance of this class,
			// use Default property instead.
		}

		public int GetHashCode(IBasicType type)
		{
			return type.Name.GetHashCode();
		}

		public bool Equals(IBasicType x, IBasicType y)
		{
			bool result;
			var xdef = x as TypeDefinition;
			var ydef = y as TypeDefinition;

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
