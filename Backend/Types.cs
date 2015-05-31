using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Microsoft.Cci.Immutable;

namespace Backend
{
	public class Types
	{
		private IMetadataHost host;

		private Types(IMetadataHost host)
		{
			this.host = host;
		}

		public static Types Instance { get; private set; }

		public static void Initialize(IMetadataHost host)
		{
			Types.Instance = new Types(host);
		}

		public IPlatformType PlatformType
		{
			get { return this.host.PlatformType; }
		}

		public ITypeReference ArrayLengthType
		{
			get { return this.host.PlatformType.SystemUIntPtr; }
		}

		public ITypeReference SizeofType
		{
			get { return this.host.PlatformType.SystemUInt32; }
		}

		public ITypeReference NativePointerType
		{
			get { return this.host.PlatformType.SystemIntPtr; }
		}

		public ITypeReference ArrayType(ITypeReference elementType, uint rank)
		{
			var type = Matrix.GetMatrix(elementType, rank, this.host.InternFactory);
			return type;
		}

		public ITypeReference ArrayElementType(ITypeReference arrayType)
		{
			var type = arrayType as IArrayTypeReference;
			return type.ElementType;
		}

		public ITypeReference PointerTargetType(ITypeReference pointerType)
		{
			ITypeReference result = null;

			if (pointerType is IPointerTypeReference)
			{
				var type = pointerType as IPointerTypeReference;
				result = type.TargetType;
			}
			else if (pointerType is IManagedPointerTypeReference)
			{
				var type = pointerType as IManagedPointerTypeReference;
				result = type.TargetType;
			}

			return result;
		}

		public ITypeReference PointerType(ITypeReference targetType)
		{
			var type = ManagedPointerType.GetManagedPointerType(targetType, this.host.InternFactory);
			return type;
		}

		public ITypeReference FunctionPointerType(ISignature signature)
		{
			var type = new FunctionPointerType(signature, this.host.InternFactory);
			return type;
		}

		public ITypeReference TokenType(IReference token)
		{
			var type = this.host.PlatformType.SystemVoid;

			if (token is IMethodReference)
			{
				type = this.host.PlatformType.SystemRuntimeMethodHandle;
			}
			else if (token is ITypeReference)
			{
				type = this.host.PlatformType.SystemRuntimeTypeHandle;
			}
			else if (token is IFieldReference)
			{
				type = this.host.PlatformType.SystemRuntimeFieldHandle;
			}

			return type;
		}

		public ITypeReference BinaryLogicalOperationType(ITypeReference left, ITypeReference right)
		{
			ITypeReference type = null;

			if (left.TypeCode == PrimitiveTypeCode.Boolean && right.TypeCode == PrimitiveTypeCode.Boolean)
			{
				type = this.host.PlatformType.SystemBoolean;
			}
			else
			{
				type = this.BinarySignedNumericOperationType(left, right);
			}

			return type;
		}

		public ITypeReference BinaryNumericOperationType(ITypeReference left, ITypeReference right, bool unsigned)
		{
			var type = this.BinarySignedNumericOperationType(left, right);

			if (unsigned)
			{
				type = TypeHelper.UnsignedEquivalent(type);
			}

			return type;
		}

		public ITypeReference BinaryUnsignedNumericOperationType(ITypeReference left, ITypeReference right)
		{
			var type = this.BinaryNumericOperationType(left, right, true);
			return type;
		}

		public ITypeReference BinarySignedNumericOperationType(ITypeReference left, ITypeReference right)
		{
			switch (left.TypeCode)
			{
				case PrimitiveTypeCode.Boolean:
				case PrimitiveTypeCode.Char:
				case PrimitiveTypeCode.UInt16:
				case PrimitiveTypeCode.UInt32:
				case PrimitiveTypeCode.UInt8:
					switch (right.TypeCode)
					{
						case PrimitiveTypeCode.Boolean:
						case PrimitiveTypeCode.Char:
						case PrimitiveTypeCode.UInt16:
						case PrimitiveTypeCode.UInt32:
						case PrimitiveTypeCode.UInt8:
							return this.host.PlatformType.SystemUInt32;

						case PrimitiveTypeCode.Int8:
						case PrimitiveTypeCode.Int16:
						case PrimitiveTypeCode.Int32:
							// Code generators will tend to make both operands be of the same type.
							// Assume this happened because the right operand is a polymorphic constant.
							return this.host.PlatformType.SystemUInt32;

						//The cases below are not expected to happen in practice
						case PrimitiveTypeCode.UInt64:
						case PrimitiveTypeCode.Int64:
							return this.host.PlatformType.SystemUInt64;

						case PrimitiveTypeCode.UIntPtr:
						case PrimitiveTypeCode.IntPtr:
							return this.host.PlatformType.SystemUIntPtr;

						case PrimitiveTypeCode.Float32:
							return this.host.PlatformType.SystemFloat32;

						case PrimitiveTypeCode.Float64:
							return this.host.PlatformType.SystemFloat64;

						default:
							// Code generators will tend to make both operands be of the same type.
							// Assume this happened because the right operand is an enum.
							return right;
					}

				case PrimitiveTypeCode.Int8:
				case PrimitiveTypeCode.Int16:
				case PrimitiveTypeCode.Int32:
					switch (right.TypeCode)
					{
						case PrimitiveTypeCode.Boolean:
						case PrimitiveTypeCode.Char:
						case PrimitiveTypeCode.UInt16:
						case PrimitiveTypeCode.UInt32:
						case PrimitiveTypeCode.UInt8:
							// Code generators will tend to make both operands be of the same type.
							// Assume this happened because the left operand is a polymorphic constant.
							return this.host.PlatformType.SystemUInt32;

						case PrimitiveTypeCode.Int8:
						case PrimitiveTypeCode.Int16:
						case PrimitiveTypeCode.Int32:
							return this.host.PlatformType.SystemInt32;

						// The cases below are not expected to happen in practice
						case PrimitiveTypeCode.UInt64:
							return this.host.PlatformType.SystemUInt64;

						case PrimitiveTypeCode.Int64:
							return this.host.PlatformType.SystemInt64;

						case PrimitiveTypeCode.UIntPtr:
							return this.host.PlatformType.SystemUIntPtr;

						case PrimitiveTypeCode.IntPtr:
							return this.host.PlatformType.SystemIntPtr;

						case PrimitiveTypeCode.Float32:
							return this.host.PlatformType.SystemFloat32;

						case PrimitiveTypeCode.Float64:
							return this.host.PlatformType.SystemFloat64;

						default:
							// Code generators will tend to make both operands be of the same type.
							// Assume this happened because the right operand is an enum.
							return right;
					}

				case PrimitiveTypeCode.UInt64:
					switch (right.TypeCode)
					{
						case PrimitiveTypeCode.Boolean:
						case PrimitiveTypeCode.Char:
						case PrimitiveTypeCode.UInt16:
						case PrimitiveTypeCode.UInt32:
						case PrimitiveTypeCode.UInt8:
						case PrimitiveTypeCode.UInt64:
							return this.host.PlatformType.SystemUInt64;

						case PrimitiveTypeCode.Int8:
						case PrimitiveTypeCode.Int16:
						case PrimitiveTypeCode.Int32:
						case PrimitiveTypeCode.Int64:
							// Code generators will tend to make both operands be of the same type.
							// Assume this happened because the right operand is a polymorphic constant.
							return this.host.PlatformType.SystemUInt64;

						case PrimitiveTypeCode.UIntPtr:
							return this.host.PlatformType.SystemUIntPtr;

						case PrimitiveTypeCode.IntPtr:
							return this.host.PlatformType.SystemIntPtr;

						case PrimitiveTypeCode.Float32:
							return this.host.PlatformType.SystemFloat32;

						case PrimitiveTypeCode.Float64:
							return this.host.PlatformType.SystemFloat64;

						default:
							// Code generators will tend to make both operands be of the same type.
							// Assume this happened because the right operand is an enum.
							return right;
					}

				case PrimitiveTypeCode.Int64:
					switch (right.TypeCode)
					{
						case PrimitiveTypeCode.Boolean:
						case PrimitiveTypeCode.Char:
						case PrimitiveTypeCode.UInt16:
						case PrimitiveTypeCode.UInt32:
						case PrimitiveTypeCode.UInt8:
						case PrimitiveTypeCode.UInt64:
							// Code generators will tend to make both operands be of the same type.
							// Assume this happened because the left operand is a polymorphic constant.
							return this.host.PlatformType.SystemUInt64;

						case PrimitiveTypeCode.Int8:
						case PrimitiveTypeCode.Int16:
						case PrimitiveTypeCode.Int32:
						case PrimitiveTypeCode.Int64:
							return this.host.PlatformType.SystemInt64;

						case PrimitiveTypeCode.UIntPtr:
						case PrimitiveTypeCode.IntPtr:
							return this.host.PlatformType.SystemIntPtr;

						case PrimitiveTypeCode.Float32:
							return this.host.PlatformType.SystemFloat32;

						case PrimitiveTypeCode.Float64:
							return this.host.PlatformType.SystemFloat64;

						default:
							// Code generators will tend to make both operands be of the same type.
							// Assume this happened because the right operand is an enum.
							return right;
					}

				case PrimitiveTypeCode.UIntPtr:
					switch (right.TypeCode)
					{
						case PrimitiveTypeCode.Boolean:
						case PrimitiveTypeCode.Char:
						case PrimitiveTypeCode.UInt16:
						case PrimitiveTypeCode.UInt32:
						case PrimitiveTypeCode.UInt8:
						case PrimitiveTypeCode.UInt64:
						case PrimitiveTypeCode.UIntPtr:
							return this.host.PlatformType.SystemUIntPtr;

						case PrimitiveTypeCode.Int8:
						case PrimitiveTypeCode.Int16:
						case PrimitiveTypeCode.Int32:
						case PrimitiveTypeCode.Int64:
						case PrimitiveTypeCode.IntPtr:
							return this.host.PlatformType.SystemUIntPtr;

						case PrimitiveTypeCode.Float32:
							return this.host.PlatformType.SystemFloat32;

						case PrimitiveTypeCode.Float64:
							return this.host.PlatformType.SystemFloat64;

						case PrimitiveTypeCode.Pointer:
						case PrimitiveTypeCode.Reference:
							return right;

						default:
							return null;
					}

				case PrimitiveTypeCode.IntPtr:
					switch (right.TypeCode)
					{
						case PrimitiveTypeCode.Boolean:
						case PrimitiveTypeCode.Char:
						case PrimitiveTypeCode.UInt16:
						case PrimitiveTypeCode.UInt32:
						case PrimitiveTypeCode.UInt8:
						case PrimitiveTypeCode.UInt64:
						case PrimitiveTypeCode.UIntPtr:
							return this.host.PlatformType.SystemUIntPtr;

						case PrimitiveTypeCode.Int8:
						case PrimitiveTypeCode.Int16:
						case PrimitiveTypeCode.Int32:
						case PrimitiveTypeCode.Int64:
						case PrimitiveTypeCode.IntPtr:
							return this.host.PlatformType.SystemIntPtr;

						case PrimitiveTypeCode.Float32:
							return this.host.PlatformType.SystemFloat32;

						case PrimitiveTypeCode.Float64:
							return this.host.PlatformType.SystemFloat64;

						case PrimitiveTypeCode.Pointer:
						case PrimitiveTypeCode.Reference:
							return right;

						default:
							return null;
					}

				case PrimitiveTypeCode.Float32:
				case PrimitiveTypeCode.Float64:
					return right;

				case PrimitiveTypeCode.Pointer:
				case PrimitiveTypeCode.Reference:
					switch (right.TypeCode)
					{
						case PrimitiveTypeCode.Pointer:
						case PrimitiveTypeCode.Reference:
							return this.host.PlatformType.SystemUIntPtr;

						case PrimitiveTypeCode.Int8:
						case PrimitiveTypeCode.Int16:
						case PrimitiveTypeCode.Int32:
						case PrimitiveTypeCode.Int64:
						case PrimitiveTypeCode.Char:
						case PrimitiveTypeCode.UInt8:
						case PrimitiveTypeCode.UInt16:
						case PrimitiveTypeCode.UInt32:
						case PrimitiveTypeCode.UInt64:
						case PrimitiveTypeCode.IntPtr:
						case PrimitiveTypeCode.UIntPtr:
							return left;

						case PrimitiveTypeCode.NotPrimitive:
							// Assume rh type is an enum.
							return left;

						default:
							return null;
					}

				default:
					switch (right.TypeCode)
					{
						case PrimitiveTypeCode.Int8:
						case PrimitiveTypeCode.Int16:
						case PrimitiveTypeCode.Int32:
						case PrimitiveTypeCode.Int64:
						case PrimitiveTypeCode.Boolean:
						case PrimitiveTypeCode.Char:
						case PrimitiveTypeCode.UInt8:
						case PrimitiveTypeCode.UInt16:
						case PrimitiveTypeCode.UInt32:
						case PrimitiveTypeCode.UInt64:
							// Assume that the left operand has an enum type.
							return left;
					}

					// Assume they are both enums
					return left;
			}
		}
	}
}
