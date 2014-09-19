using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.ThreeAddressCode
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
	
	public class ReferenceType : IReferenceType
	{
		public IReferenceTypeDefinition Definition { get; set; }
		public IList<IType> TypeParameters { get; private set; }

		public ReferenceType(IReferenceTypeDefinition definition)
		{
			this.Definition = definition;
			this.TypeParameters = new List<IType>();
		}
	}

	public class ArrayType : IReferenceType
	{
		public IType ElementsType { get; set; }

		public ArrayType(IType elementsType)
		{
			this.ElementsType = elementsType;
		}
	}
}
