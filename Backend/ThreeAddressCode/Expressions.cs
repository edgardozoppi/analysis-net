using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.ThreeAddressCode
{
	public interface IExpression
	{
		ISet<Variable> Variables { get; }

		IExpression Clone();
		IExpression Replace(IExpression oldValue, IExpression newValue);
	}

	public class BinaryExpression : IExpression
	{
		public BinaryOperation Operation { get; set; }
		public IExpression Left { get; set; }
		public IExpression Right { get; set; }

		public BinaryExpression(IExpression left, BinaryOperation operation, IExpression right)
		{
			this.Operation = operation;
			this.Left = left;
			this.Right = right;
		}

		public ISet<Variable> Variables
		{
			get
			{
				var result = new HashSet<Variable>(this.Left.Variables);
				result.UnionWith(this.Right.Variables);
				return result;
			}
		}

		public IExpression Clone()
		{
			var result = new BinaryExpression(this.Left.Clone(), this.Operation, this.Right.Clone());
			return result;
		}

		public IExpression Replace(IExpression oldValue, IExpression newValue)
		{
			if (this.Equals(oldValue))
				return newValue;

			var left = this.Left.Replace(oldValue, newValue);
			var right = this.Right.Replace(oldValue, newValue);
			var result = new BinaryExpression(left, this.Operation, right);

			return result;
		}

		public override bool Equals(object obj)
		{
			var other = obj as BinaryExpression;

			return other != null &&
				this.Left.Equals(other.Left) &&
				this.Operation.Equals(other.Operation) &&
				this.Right.Equals(other.Right);
		}

		public override int GetHashCode()
		{
			return this.Left.GetHashCode() ^ this.Operation.GetHashCode() ^ this.Right.GetHashCode();
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
		public IExpression Expression { get; set; }

		public UnaryExpression(UnaryOperation operation, IExpression operand)
		{
			this.Operation = operation;
			this.Expression = operand;
		}

		public ISet<Variable> Variables
		{
			get { return this.Expression.Variables; }
		}

		public IExpression Clone()
		{
			var result = new UnaryExpression(this.Operation, this.Expression.Clone());
			return result;
		}

		public IExpression Replace(IExpression oldValue, IExpression newValue)
		{
			if (this.Equals(oldValue))
				return newValue;

			var operand = this.Expression.Replace(oldValue, newValue);
			var result = new UnaryExpression(this.Operation, operand);

			return result;
		}

		public override bool Equals(object obj)
		{
			var other = obj as UnaryExpression;

			return other != null &&
				this.Operation.Equals(other.Operation) &&
				this.Expression.Equals(other.Expression);
		}

		public override int GetHashCode()
		{
			return this.Operation.GetHashCode() ^ this.Expression.GetHashCode();
		}

		public override string ToString()
		{
			var operation = string.Empty;

			switch (this.Operation)
			{
				case UnaryOperation.AddressOf: operation = "&"; break;
				case UnaryOperation.Neg: operation = "-"; break;
				case UnaryOperation.Not: operation = "!"; break;
			}

			return string.Format("{0}{1}", operation, this.Expression);
		}
	}

	public class PhiExpression : IExpression
	{
		public IList<Variable> Arguments { get; private set; }

		public PhiExpression()
		{
			this.Arguments = new List<Variable>();
		}

		public ISet<Variable> Variables
		{
			get
			{
				var result = new HashSet<Variable>();

				foreach (var argument in this.Arguments)
				{
					result.UnionWith(argument.Variables);
				}

				return result;
			}
		}

		public IExpression Clone()
		{
			var result = new PhiExpression();

			foreach (var argument in this.Arguments)
			{
				result.Arguments.Add(argument);
			}

			return result;
		}

		public IExpression Replace(IExpression oldValue, IExpression newValue)
		{
			if (this.Equals(oldValue))
				return newValue;

			var result = this;

			if (oldValue is Variable && newValue is Variable)
			{
				var oldVariable = oldValue as Variable;
				var newVariable = newValue as Variable;
				result = new PhiExpression();

				foreach (var argument in this.Arguments)
				{
					var variable = argument.Replace(oldVariable, newVariable);
					result.Arguments.Add(variable);
				}
			}

			return result;
		}

		public override bool Equals(object obj)
		{
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
