using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend
{
	public abstract class Operand
	{
	}

	public class Constant : Operand
	{
		public object Value { get; set; }

		public Constant(object value)
		{
			this.Value = value;
		}

		public override string ToString()
		{
			return this.Value.ToString();
		}
	}

	public abstract class Variable : Operand
	{
		public string Name { get; set; }

		public override string ToString()
		{
			return this.Name;
		}
	}

	public class LocalVariable : Variable
	{
		public LocalVariable(string name)
		{
			this.Name = name;
		}
	}

	public class TemporalVariable : Variable
	{
		public uint Index { get; set; }

		public TemporalVariable(uint index)
		{
			this.Index = index;
			this.Name = string.Format("t{0}", this.Index);
		}
	}

	public class StaticFieldAccess : Variable
	{
		public string FieldName { get; private set; }
		public ITypeReference ContainingType { get; set; }

		public StaticFieldAccess(ITypeReference containingType, string fieldName)
		{
			this.ContainingType = containingType;
			this.FieldName = fieldName;
			this.Name = string.Format("{0}::{1}", this.ContainingType, this.FieldName);
		}
	}

	public class InstanceFieldAccess : Variable
	{
		public string FieldName { get; private set; }
		public Variable Instance { get; private set; }

		public InstanceFieldAccess(Variable instance, string fieldName)
		{
			this.Instance = instance;
			this.FieldName = fieldName;
			this.Name = string.Format("{0}.{1}", this.Instance, this.FieldName);
		}
	}
}
