// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
	public interface ILoader : IDisposable
	{
		Assembly LoadAssembly(string fileName);
	}
}
