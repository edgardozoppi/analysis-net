// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

using Model.ThreeAddressCode.Instructions;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	public static class Helper
	{
		public static IMethodReference FindMethodImplementation(IBasicType receiverType, IMethodReference method)
		{
			var result = method;

			while (receiverType != null && !method.ContainingType.Equals(receiverType))
			{
				var receiverTypeDef = receiverType.ResolvedType as ClassDefinition;
				if (receiverTypeDef == null) break;

				var matchingMethod = receiverTypeDef.Methods.SingleOrDefault(m => m.MatchSignature(method));

				if (matchingMethod != null)
				{
					result = matchingMethod;
					break;
				}
				else
				{
					receiverType = receiverTypeDef.Base;
				}

			}

			return result;
		}
	}
}
