// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

using Backend.Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	public class MethodAnalysisInfo : AnalysisInfo
	{
		public IMethodReference Method { get; private set; }

		public MethodAnalysisInfo(IMethodReference method)
		{
			this.Method = method;
		}
	}
}
