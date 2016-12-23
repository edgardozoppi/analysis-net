// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	public class ProgramAnalysisInfo
	{
		private IDictionary<IMethodReference, MethodAnalysisInfo> methodsInfo;

		public ProgramAnalysisInfo()
		{
			this.methodsInfo = new Dictionary<IMethodReference, MethodAnalysisInfo>(new MethodReferenceDefinitionComparer());
		}

		public IEnumerable<IMethodReference> Methods
		{
			get { return methodsInfo.Keys; }
		}

		public bool Contains(IMethodReference method)
		{
			return methodsInfo.ContainsKey(method);
		}

		public void Add(IMethodReference method, MethodAnalysisInfo info)
		{
			methodsInfo.Add(method, info);
		}

		public MethodAnalysisInfo GetOrAdd(IMethodReference method)
		{
			MethodAnalysisInfo info;
			var ok = methodsInfo.TryGetValue(method, out info);

			if (!ok)
			{
				info = new MethodAnalysisInfo(method);
				methodsInfo.Add(method, info);
			}

			return info;
		}

		public void Remove(IMethodReference method)
		{
			methodsInfo.Remove(method);
		}

		public MethodAnalysisInfo this[IMethodReference method]
		{
			get { return methodsInfo[method]; }
		}

		public bool TryGet(IMethodReference method, out MethodAnalysisInfo info)
		{
			return methodsInfo.TryGetValue(method, out info);
		}
	}
}
