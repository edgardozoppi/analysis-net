using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRM = System.Reflection.Metadata;

namespace MetadataProvider
{
	internal static class TypeHelper
	{
		public static IType ToType(SRM.PrimitiveTypeCode typeCode)
		{
			switch (typeCode)
			{
				case SRM.PrimitiveTypeCode.Boolean: return PlatformTypes.Boolean;
				case SRM.PrimitiveTypeCode.Byte: return PlatformTypes.Byte;
				case SRM.PrimitiveTypeCode.Char: return PlatformTypes.Char;
				case SRM.PrimitiveTypeCode.Double: return PlatformTypes.Double;
				case SRM.PrimitiveTypeCode.Int16: return PlatformTypes.Int16;
				case SRM.PrimitiveTypeCode.Int32: return PlatformTypes.Int32;
				case SRM.PrimitiveTypeCode.Int64: return PlatformTypes.Int64;
				case SRM.PrimitiveTypeCode.IntPtr: return PlatformTypes.IntPtr;
				case SRM.PrimitiveTypeCode.Object: return PlatformTypes.Object;
				case SRM.PrimitiveTypeCode.SByte: return PlatformTypes.SByte;
				case SRM.PrimitiveTypeCode.Single: return PlatformTypes.Single;
				case SRM.PrimitiveTypeCode.String: return PlatformTypes.String;
				case SRM.PrimitiveTypeCode.UInt16: return PlatformTypes.UInt16;
				case SRM.PrimitiveTypeCode.UInt32: return PlatformTypes.UInt32;
				case SRM.PrimitiveTypeCode.UInt64: return PlatformTypes.UInt64;
				case SRM.PrimitiveTypeCode.UIntPtr: return PlatformTypes.UIntPtr;
				case SRM.PrimitiveTypeCode.Void: return PlatformTypes.Void;

				//case SRM.PrimitiveTypeCode.TypedReference:	return "typedref";

				default: throw typeCode.ToUnknownValueException();
			}
		}

		public static IType ToType(SRM.ConstantTypeCode typeCode)
		{
			switch (typeCode)
			{
				case SRM.ConstantTypeCode.Boolean: return PlatformTypes.Boolean;
				case SRM.ConstantTypeCode.Byte: return PlatformTypes.Byte;
				case SRM.ConstantTypeCode.Char: return PlatformTypes.Char;
				case SRM.ConstantTypeCode.Double: return PlatformTypes.Double;
				case SRM.ConstantTypeCode.Int16: return PlatformTypes.Int16;
				case SRM.ConstantTypeCode.Int32: return PlatformTypes.Int32;
				case SRM.ConstantTypeCode.Int64: return PlatformTypes.Int64;
				case SRM.ConstantTypeCode.SByte: return PlatformTypes.SByte;
				case SRM.ConstantTypeCode.Single: return PlatformTypes.Single;
				case SRM.ConstantTypeCode.String: return PlatformTypes.String;
				case SRM.ConstantTypeCode.UInt16: return PlatformTypes.UInt16;
				case SRM.ConstantTypeCode.UInt32: return PlatformTypes.UInt32;
				case SRM.ConstantTypeCode.UInt64: return PlatformTypes.UInt64;
				case SRM.ConstantTypeCode.NullReference: return PlatformTypes.Object;

				default: throw typeCode.ToUnknownValueException();
			}
		}
	}
}
