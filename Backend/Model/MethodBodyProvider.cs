using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Model
{
	public class MethodBodyProvider
	{
		private static MethodBodyProvider instance;

		private IDictionary<IMethodDefinition, MethodBody> methodBodies;

		public static MethodBodyProvider Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new MethodBodyProvider();
				}

				return instance;
			}
		}

		public MethodBodyProvider()
		{
			methodBodies = new Dictionary<IMethodDefinition, MethodBody>();
		}

		public MethodBody GetBody(IMethodDefinition method)
		{
			return methodBodies[method];
		}

		public bool ContainsBody(IMethodDefinition method)
		{
			return methodBodies.ContainsKey(method);
		}

		public void AddBody(IMethodDefinition method, MethodBody body)
		{
			methodBodies.Add(method, body);
		}
	}
}
