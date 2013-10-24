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
			this.Name = string.Format("temp_{0}", index);
			this.Index = index;
		}
	}

	public class FieldAccess : Variable
	{
		public LocalVariable Instance { get; set; }

		public FieldAccess(LocalVariable instance, string name)
		{
			this.Instance = instance;
			this.Name = name;
		}

		public override string ToString()
		{
			return string.Format("{0}.{1}", this.Instance, this.Name);
		}
	}
}
