using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Utils
{
	public class ProgramAnalysisInfo
	{
		private IDictionary<MethodDefinition, MethodAnalysisInfo> methodsInfo;

		public ProgramAnalysisInfo()
		{
			this.methodsInfo = new Dictionary<MethodDefinition, MethodAnalysisInfo>();
		}

		public bool Contains(MethodDefinition method)
		{
			return methodsInfo.ContainsKey(method);
		}

		public void Add(MethodDefinition method, MethodAnalysisInfo info)
		{
			methodsInfo.Add(method, info);
		}

		public void Remove(MethodDefinition method)
		{
			methodsInfo.Remove(method);
		}

		public MethodAnalysisInfo this[MethodDefinition method]
		{
			get { return methodsInfo[method]; }
		}

		public bool TryGet(MethodDefinition method, out MethodAnalysisInfo info)
		{
			return methodsInfo.TryGetValue(method, out info);
		}
	}
}
