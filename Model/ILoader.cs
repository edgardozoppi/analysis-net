// Copyright (c) Edgardo Zoppi. All Rights Reserved.
// See License.txt in the repository root directory for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
	public interface ILoader : IDisposable
	{
		Host Host { get; }
		Assembly LoadAssembly(string fileName);
	}
}
