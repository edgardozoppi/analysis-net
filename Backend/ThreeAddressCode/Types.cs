// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.ThreeAddressCode.Types
{
	public interface IType
	{
	}

	public interface IValueType : IType
	{

	}

	public interface IReferenceType : IType
	{

	}

	public enum PrimitiveTypeKind
	{
		Bool,
		Byte,
		SByte,
		Char,
		Decimal,
		Double,
		Float,
		Short,
		Int,
		Long,
		UShort,
		UInt,
		ULong
	}

	public class PrimitiveType : IValueType
	{
		public PrimitiveTypeKind Kind { get; set; }

		public PrimitiveType(PrimitiveTypeKind kind)
		{
			this.Kind = kind;
		}

		public string Name
		{
			get
			{
				var result = string.Empty;

				switch (this.Kind)
				{
					case PrimitiveTypeKind.Bool: result = "bool"; break;
					case PrimitiveTypeKind.Byte: result = "byte"; break;
					case PrimitiveTypeKind.SByte: result = "sbyte"; break;
					case PrimitiveTypeKind.Char: result = "char"; break;
					case PrimitiveTypeKind.Decimal: result = "decimal"; break;
					case PrimitiveTypeKind.Double: result = "double"; break;
					case PrimitiveTypeKind.Float: result = "float"; break;
					case PrimitiveTypeKind.Short: result = "short"; break;
					case PrimitiveTypeKind.Int: result = "int"; break;
					case PrimitiveTypeKind.Long: result = "long"; break;
					case PrimitiveTypeKind.UShort: result = "ushort"; break;
					case PrimitiveTypeKind.UInt: result = "uint"; break;
					case PrimitiveTypeKind.ULong: result = "ulong"; break;
				}

				return result;
			}
		}

		public override string ToString()
		{
			return this.Name;
		}
	}
	
	public static class PrimitiveTypes
	{
		public static UnknownType UnknownType
		{
			get { return UnknownType.Value; }
		}

		public static UnknownType Int32
		{
			get { return null; }
		}

		public static UnknownType IntPtr
		{
			get { return null; }
		}
	}

	public class UnknownType : IType
	{
		private static UnknownType value;

		private UnknownType() { }

		public static UnknownType Value
		{
			get
			{
				if (value == null) value = new UnknownType();
				return value;
			}
		}

		public override string ToString()
		{
			return "UNK";
		}
	}

	public class BasicType : IType
	{
		public string Name { get; set; }
		public IList<IType> GenericArguments { get; private set; }

		public BasicType(string name)
		{
			this.Name = name;
			this.GenericArguments = new List<IType>();
		}

		public override string ToString()
		{
			var arguments = string.Empty;

			if (this.GenericArguments.Count > 0)
			{
				arguments = string.Join(", ", this.GenericArguments);
				arguments = string.Format("<{0}>", arguments);
			}

			return string.Format("{0}{1}", this.Name, arguments);
		}
	}

	public class TypeVariable : IType
	{
		public string Name { get; set; }

		public TypeVariable(string name)
		{
			this.Name = name;
		}

		public override string ToString()
		{
			return this.Name;
		}
	}

	public class PointerType : IReferenceType
	{
		public IType TargetType { get; set; }

		public PointerType(IType targetType)
		{
			this.TargetType = targetType;
		}

		public override string ToString()
		{
			return string.Format("{0}*", this.TargetType);
		}
	}

	public class ArrayType : IReferenceType
	{
		public IType ElementsType { get; set; }

		public ArrayType(IType elementsType)
		{
			this.ElementsType = elementsType;
		}

		public override string ToString()
		{
			return string.Format("{0}[]", this.ElementsType);
		}
	}
}
