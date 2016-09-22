// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Microsoft.Cci.Immutable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.ThreeAddressCode.Values;
using Backend.ThreeAddressCode.Instructions;

namespace Backend.ThreeAddressCode.Expressions
{
	public interface IExpressible
	{
		IExpression ToExpression();
	}

	public interface IExpression : IValue
	{
		IExpression Replace(IExpression oldexpr, IExpression newexpr);
	}

	public class BinaryExpression : IExpression
	{
		public BinaryOperation Operation { get; set; }
		public IExpression Left { get; set; }
		public IExpression Right { get; set; }
		public ITypeReference Type { get; set; }
		public bool OverflowCheck { get; set; }
		public bool UnsignedOperands { get; set; }

		public BinaryExpression(IExpression left, BinaryOperation operation, IExpression right)
		{
			this.Operation = operation;
			this.Left = left;
			this.Right = right;
		}

		public ISet<IVariable> Variables
		{
			get
			{
				var result = new HashSet<IVariable>();
				result.UnionWith(this.Left.Variables);
				result.UnionWith(this.Right.Variables);
				return result;
			}
		}

		public void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.Left.Equals(oldvar)) this.Left = newvar;
			else this.Left.Replace(oldvar, newvar);

			if (this.Right.Equals(oldvar)) this.Right = newvar;
			else this.Right.Replace(oldvar, newvar);
		}

		public IExpression Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr)) return newexpr;

			var left = this.Left.Replace(oldexpr, newexpr);
			var right = this.Right.Replace(oldexpr, newexpr);
			var result = new BinaryExpression(left, this.Operation, right);

			return result;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as BinaryExpression;

			return other != null &&
				this.Left.Equals(other.Left) &&
				this.Operation.Equals(other.Operation) &&
				this.Right.Equals(other.Right);
		}

		public override int GetHashCode()
		{
			return this.Left.GetHashCode() ^
				this.Operation.GetHashCode() ^
				this.Right.GetHashCode();
		}

		public override string ToString()
		{
			var operation = string.Empty;

			switch (this.Operation)
			{
				case BinaryOperation.Add: operation = "+"; break;
				case BinaryOperation.Sub: operation = "-"; break;
				case BinaryOperation.Mul: operation = "*"; break;
				case BinaryOperation.Div: operation = "/"; break;
				case BinaryOperation.Rem: operation = "%"; break;
				case BinaryOperation.And: operation = "&"; break;
				case BinaryOperation.Or: operation = "|"; break;
				case BinaryOperation.Xor: operation = "^"; break;
				case BinaryOperation.Shl: operation = "<<"; break;
				case BinaryOperation.Shr: operation = ">>"; break;
				case BinaryOperation.Eq: operation = "=="; break;
				case BinaryOperation.Neq: operation = "!="; break;
				case BinaryOperation.Gt: operation = ">"; break;
				case BinaryOperation.Ge: operation = ">="; break;
				case BinaryOperation.Lt: operation = "<"; break;
				case BinaryOperation.Le: operation = "<="; break;
			}

			return string.Format("{0} {1} {2}", this.Left, operation, this.Right);
		}
	}

	public class UnaryExpression : IExpression
	{
		public UnaryOperation Operation { get; set; }
		public IExpression Operand { get; set; }
		public ITypeReference Type { get; set; }

		public UnaryExpression(UnaryOperation operation, IExpression operand)
		{
			this.Operation = operation;
			this.Operand = operand;
		}

		public ISet<IVariable> Variables
		{
			get { return this.Operand.Variables; }
		}

		public void Replace(IVariable oldvar, IVariable newvar)
		{
			this.Operand = this.Operand.Replace(oldvar, newvar);
		}

		public IExpression Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr)) return newexpr;

			var operand = this.Operand.Replace(oldexpr, newexpr);
			var result = new UnaryExpression(this.Operation, operand);

			return result;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as UnaryExpression;

			return other != null &&
				this.Operation.Equals(other.Operation) &&
				this.Operand.Equals(other.Operand);
		}

		public override int GetHashCode()
		{
			return this.Operation.GetHashCode() ^
				this.Operand.GetHashCode();
		}

		public override string ToString()
		{
			var operation = string.Empty;

			switch (this.Operation)
			{
				case UnaryOperation.Neg: operation = "-"; break;
				case UnaryOperation.Not: operation = "!"; break;
			}

			return string.Format("{0}{1}", operation, this.Operand);
		}
	}

	public class CatchExpression : IExpression
	{
		public ITypeReference ExceptionType { get; set; }

		public CatchExpression(ITypeReference exceptionType)
		{
			this.ExceptionType = exceptionType;
		}

		public ITypeReference Type
		{
			get { return this.ExceptionType; }
		}

		public ISet<IVariable> Variables
		{
			get { return new HashSet<IVariable>(); }
		}

		public void Replace(IVariable oldvar, IVariable newvar)
		{
		}

		public IExpression Replace(IExpression oldexpr, IExpression newexpr)
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
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as CatchExpression;

			return other != null &&
				this.ExceptionType.Equals(other.ExceptionType);
		}

		public override int GetHashCode()
		{
			return this.ExceptionType.GetHashCode();
		}

		public override string ToString()
		{
			var exceptionType = TypeHelper.GetTypeName(this.ExceptionType);
			return string.Format("catch {0}", exceptionType);
		}
	}

	public class ConvertExpression : IExpression
	{
		public ConvertOperation Operation { get; set; }
		public IExpression Operand { get; set; }
		public ITypeReference ConversionType { get; set; }
		public bool OverflowCheck { get; set; }
		public bool UnsignedOperands { get; set; }

		public ConvertExpression(IExpression operand, ConvertOperation operation, ITypeReference conversionType)
		{
			this.Operand = operand;
			this.Operation = operation;
			this.ConversionType = conversionType;			
		}

		public ITypeReference Type
		{
			get { return this.ConversionType; }
		}

		public ISet<IVariable> Variables
		{
			get { return this.Operand.Variables; }
		}

		public void Replace(IVariable oldvar, IVariable newvar)
		{
			this.Operand = this.Operand.Replace(oldvar, newvar);
		}

		public IExpression Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr)) return newexpr;

			var operand = this.Operand.Replace(oldexpr, newexpr);
			var result = new ConvertExpression(operand, this.Operation, this.ConversionType);

			return result;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as ConvertExpression;

			return other != null &&
				this.Operand.Equals(other.Operand) &&
				this.Type.Equals(other.Type);
		}

		public override int GetHashCode()
		{
			return this.Operand.GetHashCode() ^
				this.Type.GetHashCode();
		}

		public override string ToString()
		{
			var convertionType = TypeHelper.GetTypeName(this.ConversionType);
			return string.Format("{0} as {1}", this.Operand, convertionType);
		}
	}

	public class SizeofExpression : IExpression
	{
		public ITypeReference MeasuredType { get; set; }

		public SizeofExpression(ITypeReference measuredType)
		{
			this.MeasuredType = measuredType;
		}

		public ITypeReference Type
		{
			get { return null; }
		}

		public ISet<IVariable> Variables
		{
			get { return new HashSet<IVariable>(); }
		}

		public void Replace(IVariable oldvar, IVariable newvar)
		{
		}

		public IExpression Replace(IExpression oldexpr, IExpression newexpr)
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
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as SizeofExpression;

			return other != null &&
				this.MeasuredType.Equals(other.MeasuredType);
		}

		public override int GetHashCode()
		{
			return this.MeasuredType.GetHashCode();
		}

		public override string ToString()
		{
			var measuredType = TypeHelper.GetTypeName(this.MeasuredType);
			return string.Format("sizeof {0}", measuredType);
		}
	}

	public class TokenExpression : IExpression
	{
		public IReference Token { get; set; }

		public TokenExpression(IReference token)
		{
			this.Token = token;
		}

		public ITypeReference Type
		{
			get { return null; }
		}

		public ISet<IVariable> Variables
		{
			get { return new HashSet<IVariable>(); }
		}

		public void Replace(IVariable oldvar, IVariable newvar)
		{
		}

		public IExpression Replace(IExpression oldexpr, IExpression newexpr)
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
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as TokenExpression;

			return other != null &&
				this.Token.Equals(other.Token);
		}

		public override int GetHashCode()
		{
			return this.Token.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("token {0}", this.Token);
		}
	}

	public class MethodCallExpression : IExpression
	{
		public IMethodReference Method { get; set; }
		public IList<IVariable> Arguments { get; private set; }

		public MethodCallExpression(IMethodReference method)
		{
			this.Arguments = new List<IVariable>();
			this.Method = method;
		}

		public MethodCallExpression(IMethodReference method, IEnumerable<IVariable> arguments)
		{
			this.Arguments = new List<IVariable>(arguments);
			this.Method = method;
		}

		public ITypeReference Type
		{
			get { return this.Method.Type; }
		}

		public ISet<IVariable> Variables
		{
			get { return new HashSet<IVariable>(this.Arguments); }
		}

		public void Replace(IVariable oldvar, IVariable newvar)
		{
			for (var i = 0; i < this.Arguments.Count; ++i)
			{
				var argument = this.Arguments[i];
				if (argument.Equals(oldvar)) this.Arguments[i] = newvar;
			}
		}

		public IExpression Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr)) return newexpr;
			var result = this;

			if (oldexpr is IVariable && newexpr is IVariable)
			{
				var oldvar = oldexpr as IVariable;
				var newvar = newexpr as IVariable;
				result = new MethodCallExpression(this.Method);

				foreach (var argument in this.Arguments)
				{
					var variable = argument;
					if (argument.Equals(oldvar)) variable = newvar;
					result.Arguments.Add(variable);
				}
			}

			return result;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as MethodCallExpression;

			return other != null &&
				this.Method.Equals(other.Method) &&
				this.Arguments.SequenceEqual(other.Arguments);
		}

		public override int GetHashCode()
		{
			return this.Method.GetHashCode() ^
				this.Arguments.GetHashCode();
		}

		public override string ToString()
		{
			var type = TypeHelper.GetTypeName(this.Method.ContainingType);
			var method = MemberHelper.GetMethodSignature(this.Method, NameFormattingOptions.OmitContainingType | NameFormattingOptions.PreserveSpecialNames);
			var arguments = string.Join(", ", this.Arguments);

			return string.Format("{0}::{1}({2})", type, method, arguments);
		}
	}

	public class IndirectMethodCallExpression : IExpression
	{
		public IFunctionPointerTypeReference Function { get; set; }
		public IVariable Pointer { get; set; }
		public IList<IVariable> Arguments { get; private set; }

		public IndirectMethodCallExpression(IVariable pointer, IFunctionPointerTypeReference function)
		{
			this.Arguments = new List<IVariable>();
			this.Pointer = pointer;
			this.Function = function;
		}

		public IndirectMethodCallExpression(IVariable pointer, IFunctionPointerTypeReference function, IEnumerable<IVariable> arguments)
		{
			this.Arguments = new List<IVariable>(arguments);
			this.Pointer = pointer;
			this.Function = function;
		}

		public ITypeReference Type
		{
			get { return this.Function.Type; }
		}

		public ISet<IVariable> Variables
		{
			get { return new HashSet<IVariable>(this.Arguments) { this.Pointer }; }
		}

		public void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.Pointer.Equals(oldvar)) this.Pointer = newvar;

			for (var i = 0; i < this.Arguments.Count; ++i)
			{
				var argument = this.Arguments[i];
				if (argument.Equals(oldvar)) this.Arguments[i] = newvar;
			}
		}

		public IExpression Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr)) return newexpr;
			var result = this;

			if (oldexpr is IVariable && newexpr is IVariable)
			{
				var oldvar = oldexpr as IVariable;
				var newvar = newexpr as IVariable;
				var pointer = this.Pointer;

				if (pointer.Equals(oldvar)) pointer = newvar;
				result = new IndirectMethodCallExpression(pointer, this.Function);

				foreach (var argument in this.Arguments)
				{
					var variable = argument;
					if (argument.Equals(oldvar)) variable = newvar;
					result.Arguments.Add(variable);
				}
			}

			return result;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as IndirectMethodCallExpression;

			return other != null &&
				this.Pointer.Equals(other.Pointer) &&
				this.Function.Equals(other.Function) &&
				this.Arguments.SequenceEqual(other.Arguments);
		}

		public override int GetHashCode()
		{
			return this.Pointer.GetHashCode() ^
				this.Function.GetHashCode() ^
				this.Arguments.GetHashCode();
		}

		public override string ToString()
		{
			var arguments = string.Join(", ", this.Arguments);
			return string.Format("(*{0})({1})", this.Pointer, arguments);
		}
	}

	public class CreateObjectExpression : IExpression
	{
		public ITypeReference AllocationType { get; set; }

		public CreateObjectExpression(ITypeReference allocationType)
		{
			this.AllocationType = allocationType;
		}

		public ITypeReference Type
		{
			get { return this.AllocationType; }
		}

		public ISet<IVariable> Variables
		{
			get { return new HashSet<IVariable>(); }
		}

		public void Replace(IVariable oldvar, IVariable newvar)
		{
		}

		public IExpression Replace(IExpression oldexpr, IExpression newexpr)
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
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as CreateObjectExpression;

			return other != null &&
				this.AllocationType.Equals(other.AllocationType);
		}

		public override int GetHashCode()
		{
			return this.AllocationType.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("new {0};", this.AllocationType);
		}
	}

	public class CreateArrayExpression : IExpression
	{
		public ITypeReference ElementType { get; set; }
		public uint Rank { get; set; }
		//public IList<IVariable> LowerBounds { get; private set; }
		public IList<IVariable> Sizes { get; private set; }

		public CreateArrayExpression(ITypeReference elementType, uint rank)
		{
			this.ElementType = elementType;
			this.Rank = rank;
			//this.LowerBounds = new List<IVariable>();
			this.Sizes = new List<IVariable>();
		}

		public CreateArrayExpression(ITypeReference elementType, uint rank, IEnumerable<IVariable> lowerBounds, IEnumerable<IVariable> sizes)
		{
			this.ElementType = elementType;
			this.Rank = rank;
			//this.LowerBounds = new List<IVariable>(lowerBounds);
			this.Sizes = new List<IVariable>(sizes);
		}

		public ITypeReference Type
		{
			get { return Matrix.GetMatrix(this.ElementType, this.Rank, null); }
		}

		public ISet<IVariable> Variables
		{
			get
			{
				var result = new HashSet<IVariable>();
				//result.UnionWith(this.LowerBounds);
				result.UnionWith(this.Sizes);
				return result;
			}
		}

		public void Replace(IVariable oldvar, IVariable newvar)
		{
			//for (var i = 0; i < this.LowerBounds.Count; ++i)
			//{
			//	var bound = this.LowerBounds[i];
			//	if (bound.Equals(oldvar)) this.LowerBounds[i] = newvar;
			//}

			for (var i = 0; i < this.Sizes.Count; ++i)
			{
				var size = this.Sizes[i];
				if (size.Equals(oldvar)) this.Sizes[i] = newvar;
			}
		}

		public IExpression Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr)) return newexpr;
			var result = this;

			if (oldexpr is IVariable && newexpr is IVariable)
			{
				var oldvar = oldexpr as IVariable;
				var newvar = newexpr as IVariable;
				result = new CreateArrayExpression(this.ElementType, this.Rank);

				//foreach (var bound in this.LowerBounds)
				//{
				//	var variable = bound;
				//	if (bound.Equals(oldvar)) variable = newvar;
				//	result.LowerBounds.Add(variable);
				//}

				foreach (var size in this.Sizes)
				{
					var variable = size;
					if (size.Equals(oldvar)) variable = newvar;
					result.Sizes.Add(variable);
				}
			}

			return result;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as CreateArrayExpression;

			return other != null &&
				this.ElementType.Equals(other.ElementType) &&
				this.Rank.Equals(other.Rank) &&
				//this.LowerBounds.SequenceEqual(other.LowerBounds) &&
				this.Sizes.SequenceEqual(other.Sizes);
		}

		public override int GetHashCode()
		{
			return this.ElementType.GetHashCode() ^
				this.Rank.GetHashCode() ^
				//this.LowerBounds.GetHashCode() ^
				this.Sizes.GetHashCode();
		}

		public override string ToString()
		{
			var elementType = TypeHelper.GetTypeName(this.ElementType);
			var sizes = string.Join(", ", this.Sizes);
			return string.Format("new {0}[{1}]", elementType, sizes);
		}
	}

	public class PhiExpression : IExpression
	{
		public IList<IVariable> Arguments { get; private set; }

		public PhiExpression()
		{
			this.Arguments = new List<IVariable>();
		}

		public PhiExpression(IEnumerable<IVariable> arguments)
		{
			this.Arguments = new List<IVariable>(arguments);
		}

		public ITypeReference Type
		{
			get
			{
				ITypeReference type = null;

				if (this.Arguments.Count > 0)
				{
					type = this.Arguments.First().Type;
				}

				return type;
			}
		}

		public ISet<IVariable> Variables
		{
			get { return new HashSet<IVariable>(this.Arguments); }
		}

		public void Replace(IVariable oldvar, IVariable newvar)
		{
			for (var i = 0; i < this.Arguments.Count; ++i)
			{
				var argument = this.Arguments[i];
				if (argument.Equals(oldvar)) this.Arguments[i] = newvar;
			}
		}

		public IExpression Replace(IExpression oldexpr, IExpression newexpr)
		{
			if (this.Equals(oldexpr))
				return newexpr;

			var result = this;

			if (oldexpr is IVariable && newexpr is IVariable)
			{
				var oldvar = oldexpr as IVariable;
				var newvar = newexpr as IVariable;
				result = new PhiExpression();

				foreach (var argument in this.Arguments)
				{
					var variable = argument;
					if (argument.Equals(oldvar)) variable = newvar;
					result.Arguments.Add(variable);
				}
			}

			return result;
		}

		IExpression IExpressible.ToExpression()
		{
			return this;
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as PhiExpression;

			return other != null &&
				this.Arguments.SequenceEqual(other.Arguments);
		}

		public override int GetHashCode()
		{
			return this.Arguments.GetHashCode();
		}

		public override string ToString()
		{
			var arguments = string.Join(", ", this.Arguments);
			return string.Format("Φ({0})", arguments);
		}
	}
}
