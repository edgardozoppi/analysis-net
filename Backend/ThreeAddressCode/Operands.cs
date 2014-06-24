using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.ThreeAddressCode
{
	public abstract class Operand : IExpression
	{
		#region IExpression

		ISet<Variable> IExpression.Variables
		{
			get { return new HashSet<Variable>(); }
		}

		IExpression IExpression.Clone()
		{
			return this;
		}

		IExpression IExpression.Replace(IExpression oldValue, IExpression newValue)
		{
			if (this.Equals(oldValue)) return newValue;
			else return this;
		}

		#endregion
	}

	public class StaticMethod : Operand
	{
		public IMethodReference Method { get; set; }

		public StaticMethod(IMethodReference method)
		{
			this.Method = method;
		}

		public override bool Equals(object obj)
		{
			var other = obj as StaticMethod;
			return other != null &&
				this.Method.Equals(other.Method);
		}

		public override int GetHashCode()
		{
			return this.Method.GetHashCode();
		}

		public override string ToString()
		{
			var type = TypeHelper.GetTypeName(this.Method.ContainingType);
			var method = MemberHelper.GetMethodSignature(this.Method, NameFormattingOptions.OmitContainingType | NameFormattingOptions.PreserveSpecialNames);

			return string.Format("{0}::{1}", type, method);
		}
	}

	public class VirtualMethod : Operand
	{
		public Variable Instance { get; set; }
		public IMethodReference Method { get; set; }

		public VirtualMethod(Variable instance, IMethodReference method)
		{
			this.Instance = instance;
			this.Method = method;
		}

		public override bool Equals(object obj)
		{
			var other = obj as VirtualMethod;
			return other != null &&
				this.Instance.Equals(other.Instance) &&
				this.Method.Equals(other.Method);
		}

		public override int GetHashCode()
		{
			return this.Instance.GetHashCode() ^ this.Method.GetHashCode();
		}

		public override string ToString()
		{
			var type = TypeHelper.GetTypeName(this.Method.ContainingType);
			var method = MemberHelper.GetMethodSignature(this.Method, NameFormattingOptions.OmitContainingType | NameFormattingOptions.PreserveSpecialNames);

			return string.Format("{0}::{1}({2})", type, method, this.Instance);
		}
	}

	public class UnknownValue : Operand
	{
		private static UnknownValue value;

		private UnknownValue() { }

		public static UnknownValue Value
		{
			get
			{
				if (value == null)
				{
					value = new UnknownValue();
				}

				return value;
			}
		}

		public override string ToString()
		{
			return "UNK";
		}
	}

	public class Constant : Operand
	{
		public object Value { get; set; }

		public Constant(object value)
		{
			this.Value = value;
		}

		public override bool Equals(object obj)
		{
			var other = obj as Constant;
			return other != null &&
				this.Value.Equals(other.Value);
		}

		public override int GetHashCode()
		{
			return this.Value.GetHashCode();
		}

		public override string ToString()
		{
			return this.Value.ToString();
		}
	}

	public abstract class Variable : Operand, IExpression
	{
		public abstract string Name { get; set; }

		#region IExpression

		ISet<Variable> IExpression.Variables
		{
			get { return new HashSet<Variable>() { this }; }
		}

		#endregion

		public abstract Variable ChangeRoot(Variable root);

		public override bool Equals(object obj)
		{
			var other = obj as Variable;
			return other != null &&
				this.Name.Equals(other.Name);
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		public override string ToString()
		{
			return this.Name;
		}
	}

	public class LocalVariable : Variable
	{
		public override string Name { get; set; }

		public LocalVariable(string name)
		{
			this.Name = name;
		}
	}

	public class TemporalVariable : Variable
	{
		public uint Index { get; set; }

		public override string Name
		{
			get { return string.Format("t{0}", this.Index); }
			set { throw new InvalidOperationException(); }
		}

		public TemporalVariable(uint index)
		{
			this.Index = index;
		}
	}

	public class DerivedVariable : Variable
	{
		public Variable Original { get; set; }

		public uint Index { get; set; }

		public override string Name
		{
			get { return string.Format("{0}{1}", this.Original, this.Index); }
			set { throw new InvalidOperationException(); }
		}

		public DerivedVariable(Variable original, uint index)
		{
			this.Original = original;
			this.Index = index;
		}
	}

	public class StaticFieldAccess : Variable
	{
		public string FieldName { get; set; }
		public ITypeReference ContainingType { get; set; }

		public override string Name
		{
			get { return string.Format("{0}::{1}", this.ContainingType, this.FieldName); }
			set { throw new InvalidOperationException(); }
		}

		public StaticFieldAccess(ITypeReference containingType, string fieldName)
		{
			this.ContainingType = containingType;
			this.FieldName = fieldName;
		}
	}

	public class InstanceFieldAccess : Variable
	{
		public string FieldName { get; set; }
		public Variable Instance { get; set; }

		public override string Name
		{
			get { return string.Format("{0}.{1}", this.Instance, this.FieldName); }
			set { throw new InvalidOperationException(); }
		}

		public InstanceFieldAccess(Variable instance, string fieldName)
		{
			this.Instance = instance;
			this.FieldName = fieldName;
		}
	}

	public class ArrayElementAccess : Variable
	{
		public Variable Array { get; set; }
		public Operand Index { get; set; }

		public override string Name
		{
			get { return string.Format("{0}[{1}]", this.Array, this.Index); }
			set { throw new InvalidOperationException(); }
		}

		public ArrayElementAccess(Variable array, Operand index)
		{
			this.Array = array;
			this.Index = index;
		}
	}

	public class IndirectAccess : Variable
	{
		public Variable Address { get; set; }

		public override string Name
		{
			get { return string.Format("*{0}", this.Address); }
			set { throw new InvalidOperationException(); }
		}

		public IndirectAccess(Variable address)
		{
			this.Address = address;
		}
	}
}
