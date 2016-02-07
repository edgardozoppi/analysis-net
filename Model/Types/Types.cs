using Model.ThreeAddressCode.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Types
{
	public interface IMetadataReference
	{
	}

	public enum TypeKind
	{
		Unknown,
		ValueType,
		ReferenceType
	}

	public interface IType : IMetadataReference
	{
		TypeKind TypeKind { get; }
	}

	public interface IValueType : IType
	{

	}

	public interface IReferenceType : IType
	{

	}

	public static class PlatformTypes
	{
		public static readonly UnknownType Unknown = UnknownType.Value;
		public static readonly BasicType Boolean = new BasicType("Boolean", TypeKind.ValueType);
		public static readonly BasicType Char = new BasicType("Char", TypeKind.ValueType);
		public static readonly BasicType String = new BasicType("String", TypeKind.ReferenceType);
		public static readonly BasicType Byte = new BasicType("Byte", TypeKind.ValueType);
		public static readonly BasicType SByte = new BasicType("SByte", TypeKind.ValueType);
		public static readonly BasicType Int16 = new BasicType("Int16", TypeKind.ValueType);
		public static readonly BasicType Int32 = new BasicType("Int32", TypeKind.ValueType);
		public static readonly BasicType Int64 = new BasicType("Int64", TypeKind.ValueType);
		public static readonly BasicType UInt16 = new BasicType("UInt16", TypeKind.ValueType);
		public static readonly BasicType UInt32 = new BasicType("UInt32", TypeKind.ValueType);
		public static readonly BasicType UInt64 = new BasicType("UInt64", TypeKind.ValueType);
		public static readonly BasicType Decimal = new BasicType("Decimal", TypeKind.ValueType);
		public static readonly BasicType Single = new BasicType("Single", TypeKind.ValueType);
		public static readonly BasicType Double = new BasicType("Double", TypeKind.ValueType);
		public static readonly BasicType Object = new BasicType("Object", TypeKind.ReferenceType);
		public static readonly BasicType IntPtr = new BasicType("IntPtr", TypeKind.ValueType);
		public static readonly BasicType UIntPtr = new BasicType("UIntPtr", TypeKind.ValueType);
		public static readonly BasicType RuntimeMethodHandle = new BasicType("RuntimeMethodHandle", TypeKind.ValueType);
		public static readonly BasicType RuntimeTypeHandle = new BasicType("RuntimeTypeHandle", TypeKind.ValueType);
		public static readonly BasicType RuntimeFieldHandle = new BasicType("RuntimeFieldHandle", TypeKind.ValueType);
		public static readonly BasicType ArrayLengthType = UInt32;
		public static readonly BasicType SizeofType = UInt32;
		public static readonly BasicType Int8 = SByte;
		public static readonly BasicType UInt8 = Byte;
		public static readonly BasicType Float32 = Single;
		public static readonly BasicType Float64 = Double;
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

		public TypeKind TypeKind
		{
			get { return TypeKind.Unknown; }
		}

		public override string ToString()
		{
			return "Unknown";
		}
	}

	public class BasicType : IType
	{
		public TypeKind TypeKind { get; set; }
		public IAssemblyReference Assembly { get; set; }
		public string Namespace { get; set; }
		public string Name { get; set; }
		public IList<IType> GenericArguments { get; private set; }

		public BasicType(string name, TypeKind kind = TypeKind.Unknown)
		{
			this.Name = name;
			this.TypeKind = kind;
			this.GenericArguments = new List<IType>();
		}

		public string FullName
		{
			get
			{
				var containingAssembly = string.Empty;
				var containingNamespace = string.Empty;
				var arguments = string.Empty;

				if (this.Assembly != null)
				{
					containingAssembly = string.Format("[{0}]", this.Assembly.Name);
				}

				if (!string.IsNullOrEmpty(this.Namespace))
				{
					containingNamespace = string.Format("{0}.", this.Namespace);
				}

				if (this.GenericArguments.Count > 0)
				{
					arguments = string.Join(", ", this.GenericArguments);
					arguments = string.Format("<{0}>", arguments);
				}

				return string.Format("{0}{1}{2}{3}", containingAssembly, containingNamespace, this.Name, arguments);
			}
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
		public TypeKind TypeKind { get; set; }
		public string Name { get; set; }

		public TypeVariable(string name, TypeKind kind = TypeKind.Unknown)
		{
			this.Name = name;
			this.TypeKind = kind;
		}
		
		public override string ToString()
		{
			return this.Name;
		}
	}

	public class FunctionPointerType : IReferenceType
	{
		public IType ReturnType { get; set; }
		public IList<IMethodParameterReference> Parameters { get; private set; }
		public bool IsStatic { get; set; }

		public FunctionPointerType(IType returnType)
		{
			this.ReturnType = returnType;
			this.Parameters = new List<IMethodParameterReference>();
		}

		public FunctionPointerType(IMethodReference method)
			: this(method.ReturnType)
		{
			this.IsStatic = method.IsStatic;
			this.Parameters.AddRange(method.Parameters);
		}

		public TypeKind TypeKind
		{
			get { return TypeKind.ReferenceType; }
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			var parameters = string.Join(", ", this.Parameters);

			if (this.IsStatic)
			{
				result.Append("static ");
			}
			
			result.Append(this.ReturnType);
			result.AppendFormat("({0})", parameters);
			return result.ToString();
		}
	}

	public class PointerType : IReferenceType
	{
		public IType TargetType { get; set; }

		public PointerType(IType targetType)
		{
			this.TargetType = targetType;
		}

		public TypeKind TypeKind
		{
			get { return TypeKind.ReferenceType; }
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

		public TypeKind TypeKind
		{
			get { return TypeKind.ReferenceType; }
		}

		public override string ToString()
		{
			return string.Format("{0}[]", this.ElementsType);
		}
	}
}
