using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Cci;

namespace Backend.ThreeAddressCode
{
	public interface IVariableContainer
	{
		ISet<Variable> Variables { get; }
		void Replace(Variable oldvar, Variable newvar);
	}

	public interface IValue : IVariableContainer, IExpressible
	{
	}

	public interface IAssignableValue : IValue
	{
	}

	public interface IReferenceable : IVariableContainer, IExpression
	{
	}

	public interface IMethodSignature : IReferenceable
	{
		IMethodReference Method { get; set; }
	}

	public class StaticMethod : IMethodSignature
	{
		public IMethodReference Method { get; set; }

		public StaticMethod(IMethodReference method)
		{
			this.Method = method;
		}

		ISet<Variable> IVariableContainer.Variables
		{
			get { return new HashSet<Variable>(); }
		}

		void IVariableContainer.Replace(Variable oldvar, Variable newvar)
		{
		}

		IExpression IExpression.Replace(IExpression oldexpr, IExpression newexpr)
		{
			return this;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
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

	public class VirtualMethod : IMethodSignature
	{
		public Variable Instance { get; set; }
		public IMethodReference Method { get; set; }

		public VirtualMethod(Variable instance, IMethodReference method)
		{
			this.Instance = instance;
			this.Method = method;
		}

		public ISet<Variable> Variables
		{
			get { return new HashSet<Variable>() { this.Instance }; }
		}

		public void Replace(Variable oldvar, Variable newvar)
		{
			if (this.Instance.Equals(oldvar)) this.Instance = newvar;
		}

		IExpression IExpression.Replace(IExpression oldexpr, IExpression newexpr)
		{
			var result = this;

			if (oldexpr is Variable && newexpr is Variable)
			{
				var instance = (this.Instance as IExpression).Replace(oldexpr, newexpr) as Variable;
				result = new VirtualMethod(instance, this.Method);
			}

			return result;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
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
			return this.Instance.GetHashCode() ^
				this.Method.GetHashCode();
		}

		public override string ToString()
		{
			var type = TypeHelper.GetTypeName(this.Method.ContainingType);
			var method = MemberHelper.GetMethodSignature(this.Method, NameFormattingOptions.OmitContainingType | NameFormattingOptions.PreserveSpecialNames);

			return string.Format("{0}::{1}({2})", type, method, this.Instance);
		}
	}

	public interface IInmediateValue : IValue, IExpression
	{
	}

	public class UnknownValue : IInmediateValue
	{
		private static UnknownValue value;

		private UnknownValue() { }

		public static UnknownValue Value
		{
			get
			{
				if (value == null) value = new UnknownValue();
				return value;
			}
		}

		ISet<Variable> IVariableContainer.Variables
		{
			get { return new HashSet<Variable>(); }
		}

		void IVariableContainer.Replace(Variable oldvar, Variable newvar)
		{
		}

		IExpression IExpression.Replace(IExpression oldexpr, IExpression newexpr)
		{
			return this;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

		public override string ToString()
		{
			return "UNK";
		}
	}

	public class Constant : IInmediateValue
	{
		public object Value { get; set; }

		public Constant(object value)
		{
			this.Value = value;
		}

		ISet<Variable> IVariableContainer.Variables
		{
			get { return new HashSet<Variable>(); }
		}

		void IVariableContainer.Replace(Variable oldvar, Variable newvar)
		{
		}

		IExpression IExpression.Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr)) return newexpr;
			return this;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
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
			return Convert.ToString(this.Value);
		}
	}

	public abstract class Variable : IInmediateValue, IReferenceable
	{
		public abstract string Name { get; }

		ISet<Variable> IVariableContainer.Variables
		{
			get { return new HashSet<Variable>() { this }; }
		}

		void IVariableContainer.Replace(Variable oldvar, Variable newvar)
		{
		}

		IExpression IExpression.Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr)) return newexpr;
			return this;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

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
		private string name;

		public LocalVariable(string name)
		{
			this.name = name;
		}

		public override string Name
		{
			get { return this.name; }
		}
	}

	public class TemporalVariable : Variable
	{
		public uint Index { get; set; }

		public TemporalVariable(uint index)
		{
			this.Index = index;
		}

		public override string Name
		{
			get { return string.Format("t{0}", this.Index); }
		}
	}

	public class DerivedVariable : Variable
	{
		public Variable Original { get; set; }
		public uint Index { get; set; }

		public DerivedVariable(Variable original, uint index)
		{
			this.Original = original;
			this.Index = index;
		}

		public override string Name
		{
			get { return string.Format("{0}{1}", this.Original, this.Index); }
		}
	}

	public abstract class FieldAccess : IAssignableValue, IReferenceable, IExpression
	{
		public string FieldName { get; set; }
		public abstract string Name { get; }

		public virtual ISet<Variable> Variables
		{
			get { return new HashSet<Variable>(); }
		}

		public virtual void Replace(Variable oldvar, Variable newvar)
		{
		}

		IExpression IExpression.Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr)) return newexpr;
			return this;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

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

	public class StaticFieldAccess : FieldAccess
	{
		public ITypeReference ContainingType { get; set; }

		public StaticFieldAccess(ITypeReference containingType, string fieldName)
		{
			this.ContainingType = containingType;
			this.FieldName = fieldName;
		}

		public override string Name
		{
			get { return string.Format("{0}::{1}", this.ContainingType, this.FieldName); }
		}
	}

	public class InstanceFieldAccess : FieldAccess, IExpression
	{
		public Variable Instance { get; set; }

		public InstanceFieldAccess(Variable instance, string fieldName)
		{
			this.Instance = instance;
			this.FieldName = fieldName;
		}

		public override ISet<Variable> Variables
		{
			get { return new HashSet<Variable>() { this.Instance }; }
		}

		public override void Replace(Variable oldvar, Variable newvar)
		{
			if (this.Instance.Equals(oldvar)) this.Instance = newvar;
		}

		IExpression IExpression.Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr)) return newexpr;
			var result = this;

			if (oldexpr is Variable && newexpr is Variable)
			{
				var instance = (this.Instance as IExpression).Replace(oldexpr, newexpr) as Variable;
				result = new InstanceFieldAccess(instance, this.FieldName);
			}

			return result;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

		public override string Name
		{
			get { return string.Format("{0}.{1}", this.Instance, this.FieldName); }
		}
	}

	public class ArrayElementAccess : IAssignableValue, IReferenceable, IExpression
	{
		public Variable Array { get; set; }
		public Variable Index { get; set; }

		public ArrayElementAccess(Variable array, Variable index)
		{
			this.Array = array;
			this.Index = index;
		}

		public ISet<Variable> Variables
		{
			get { return new HashSet<Variable>() { this.Array, this.Index }; }
		}

		public void Replace(Variable oldvar, Variable newvar)
		{
			if (this.Array.Equals(oldvar)) this.Array = newvar;
			if (this.Index.Equals(oldvar)) this.Index = newvar;
		}

		IExpression IExpression.Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr)) return newexpr;
			var result = this;

			if (oldexpr is Variable && newexpr is Variable)
			{
				var array = (this.Array as IExpression).Replace(oldexpr, newexpr) as Variable;
				var index = (this.Index as IExpression).Replace(oldexpr, newexpr) as Variable;
				result = new ArrayElementAccess(array, index);
			}

			return result;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

		public override bool Equals(object obj)
		{
			var other = obj as ArrayElementAccess;
			return other != null &&
				this.Array.Equals(other.Array) &&
				this.Index.Equals(other.Index);
		}

		public override int GetHashCode()
		{
			return this.Array.GetHashCode() ^
				this.Index.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("{0}[{1}]", this.Array, this.Index);
		}
	}

	public class Dereference : IAssignableValue, IReferenceable, IExpression
	{
		public Variable Reference { get; set; }

		public Dereference(Variable reference)
		{
			this.Reference = reference;
		}

		public ISet<Variable> Variables
		{
			get { return new HashSet<Variable>() { this.Reference }; }
		}

		public void Replace(Variable oldvar, Variable newvar)
		{
			if (this.Reference.Equals(oldvar)) this.Reference = newvar;
		}

		IExpression IExpression.Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr)) return newexpr;
			var result = this;

			if (oldexpr is Variable && newexpr is Variable)
			{
				var reference = (this.Reference as IExpression).Replace(oldexpr, newexpr) as Variable;
				result = new Dereference(reference);
			}

			return result;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

		public override bool Equals(object obj)
		{
			var other = obj as Dereference;
			return other != null &&
				this.Reference.Equals(other.Reference);
		}

		public override int GetHashCode()
		{
			return this.Reference.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("*{0}", this.Reference);
		}
	}

	public class Reference : IValue, IExpression
	{
		public IReferenceable Value { get; set; }

		public Reference(IReferenceable value)
		{
			this.Value = value;
		}

		public ISet<Variable> Variables
		{
			get { return new HashSet<Variable>(this.Value.Variables); }
		}

		public void Replace(Variable oldvar, Variable newvar)
		{
			if (this.Value.Equals(oldvar)) this.Value = newvar;
			else this.Value.Replace(oldvar, newvar);
		}

		IExpression IExpression.Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr)) return newexpr;

			var value = (this.Value as IExpression).Replace(oldexpr, newexpr) as IReferenceable;
			var result = new Reference(value);

			return result;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

		public override bool Equals(object obj)
		{
			var other = obj as Reference;
			return other != null &&
				this.Value.Equals(other.Value);
		}

		public override int GetHashCode()
		{
			return this.Value.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("*{0}", this.Value);
		}
	}
}
