// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.ThreeAddressCode;
using Model.ThreeAddressCode.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Types
{
	public interface ITypeDefinitionContainer
	{
		IList<ITypeDefinition> Types { get; }
	}

	public interface ITypeMemberReference
	{
		IBasicType ContainingType { get; }
	}

	public interface ITypeMemberDefinition : ITypeMemberReference
	{
		new ITypeDefinition ContainingType { get; set; }

		bool MatchReference(ITypeMemberReference member);
	}

	public interface ITypeDefinition : ITypeMemberDefinition, IBasicType
	{
		new Assembly ContainingAssembly { get; set; }
		new Namespace ContainingNamespace { get; set; }
		new ITypeDefinition ContainingType { get; set; }
		IEnumerable<ITypeMemberDefinition> Members { get; }

		bool MatchReference(IBasicType type);
	}

	public interface IGenericReference : ITypeMemberReference
	{
		int GenericParameterCount { get; }
	}

	public interface IGenericDefinition : IGenericReference, ITypeMemberDefinition
	{
		IList<GenericParameter> GenericParameters { get; }
	}

	public interface IValueTypeDefinition : ITypeDefinition
	{
	}

	public interface IReferenceTypeDefinition : ITypeDefinition
	{
	}

	public class CustomAttribute
	{
		public IType Type { get; set; }
		public IMethodReference Constructor { get; set; }
		public IList<Constant> Arguments { get; private set; }

		public CustomAttribute()
		{
			this.Arguments = new List<Constant>();
		}

		public override string ToString()
		{
			var arguments = string.Join(", ", this.Arguments);

			return string.Format("[{0}({1})]", this.Type, arguments);
		}
	}

	public class StructDefinition : IValueTypeDefinition, IGenericDefinition, ITypeDefinitionContainer
	{
		public Assembly ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public ITypeDefinition ContainingType { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public string Name { get; set; }
		public IList<IBasicType> Interfaces { get; private set; }
		public IList<GenericParameter> GenericParameters { get; private set; }
		public IList<FieldDefinition> Fields { get; private set; }
		public IList<MethodDefinition> Methods { get; private set; }
		public IList<ITypeDefinition> Types { get; private set; }

		public StructDefinition(string name)
		{
			this.Name = name;
			this.Attributes = new HashSet<CustomAttribute>();
			this.Interfaces = new List<IBasicType>();
			this.GenericParameters = new List<GenericParameter>();
			this.Fields = new List<FieldDefinition>();
			this.Methods = new List<MethodDefinition>();
			this.Types = new List<ITypeDefinition>();
		}

		public string GenericName
		{
			get
			{
				var parameters = string.Empty;

				if (this.GenericParameters.Count > 0)
				{
					parameters = string.Join(", ", this.GenericParameters);
					parameters = string.Format("<{0}>", parameters);
				}

				return string.Format("{0}{1}", this.Name, parameters);
			}
		}

		public IEnumerable<ITypeMemberDefinition> Members
		{
			get
			{
				var result = this.Types.AsEnumerable<ITypeMemberDefinition>();
				result = result.Union(this.Fields);
				result = result.Union(this.Methods);
				return result;
			}
		}

		#region ITypeMemberReference members

		IBasicType ITypeMemberReference.ContainingType
		{
			get { return this.ContainingType; }
		}

		#endregion

		#region IGenericReference members

		int IGenericReference.GenericParameterCount
		{
			get { return this.GenericParameters.Count; }
		}

		#endregion

		#region IBasicType members

		IAssemblyReference IBasicType.ContainingAssembly
		{
			get { return this.ContainingAssembly; }
		}

		string IBasicType.ContainingNamespace
		{
			get { return this.ContainingNamespace.FullName; }
		}

		IList<IType> IBasicType.GenericArguments
		{
			get { return new List<IType>(); }
		}

		ITypeDefinition IBasicType.ResolvedType
		{
			get { return this; }
		}

		public TypeKind TypeKind
		{
			get { return TypeKind.ValueType; }
		}

		IBasicType IBasicType.GenericType
		{
			get { return null; }
		}

		#endregion

		public bool MatchReference(ITypeMemberReference member)
		{
			var type = member as IBasicType;
			var result = type != null && this.MatchReference(type);

			return result;
		}

		public bool MatchReference(IBasicType type)
		{
			var result = false;

			if (type is ITypeDefinition)
			{
				result = this.Equals(type);
			}
			else
			{
				if (type.GenericType != null)
				{
					type = type.GenericType;
				}

				// TODO: Maybe we should also compare the TypeKind?
				result = this.Name == type.Name &&
						 this.GenericParameters.Count == type.GenericArguments.Count &&
						 this.ContainingNamespace.FullName == type.ContainingNamespace &&
						 this.ContainingAssembly.MatchReference(type.ContainingAssembly) &&
						 this.ContainingType.BothNullOrMatchReference(type.ContainingType);
			}

			return result;
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			result.AppendFormat("struct {0}", this.GenericName);

			if (this.Interfaces.Count > 0)
			{
				var interfaces = string.Join(", ", this.Interfaces);
				result.AppendFormat(" : {0}", interfaces);
			}

			result.AppendLine();
			result.AppendLine("{");

			foreach (var field in this.Fields)
			{
				result.AppendFormat("  {0};\n", field);
			}

			foreach (var method in this.Methods)
			{
				var methodString = method.ToString();
				methodString = methodString.Replace("\n", "\n  ");
				result.AppendFormat("{0}\n", methodString);
			}

			result.AppendLine("}");
			return result.ToString();
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}
	}

	public interface IFieldReference : ITypeMemberReference, IMetadataReference
	{
		IType Type { get; }
		string Name { get; }
		bool IsStatic { get; }
	}

	public class FieldReference : IFieldReference
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public IBasicType ContainingType { get; set; }
		public IType Type { get; set; }
		public string Name { get; set; }
		public bool IsStatic { get; set; }

		public FieldReference(string name, IType type)
		{
			this.Name = name;
			this.Type = type;
			this.Attributes = new HashSet<CustomAttribute>();
		}

		public override string ToString()
		{
			var modifier = this.IsStatic ? "static " : string.Empty;
			return string.Format("{0}{1} {2}", modifier, this.Type, this.Name);
		}
	}

	public class FieldDefinition : ITypeMemberDefinition, IFieldReference
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public ITypeDefinition ContainingType { get; set; }
		public IType Type { get; set; }
		public string Name { get; set; }
		public bool IsStatic { get; set; }

		public FieldDefinition(string name, IType type)
		{
			this.Name = name;
			this.Type = type;
			this.Attributes = new HashSet<CustomAttribute>();
		}

		#region ITypeMemberReference members

		IBasicType ITypeMemberReference.ContainingType
		{
			get { return this.ContainingType; }
		}

		#endregion

		public bool MatchReference(ITypeMemberReference member)
		{
			var result = false;

			if (member is ITypeMemberDefinition)
			{
				result = this.Equals(member);
			}
			else
			{
				var field = member as IFieldReference;

				result = field != null &&
						 this.Name == field.Name &&
						 this.IsStatic == field.IsStatic &&
						 this.ContainingType.MatchReference(field.ContainingType) &&
						 this.Type.Equals(field.Type);
			}

			return result;
		}

		public override string ToString()
		{
			var modifier = this.IsStatic ? "static " : string.Empty;
			return string.Format("{0}{1} {2}", modifier, this.Type, this.Name);
		}
	}

	public enum MethodParameterKind
	{
		In,
		Out,
		Ref
	}

	public interface IMethodParameterReference
	{
		ushort Index { get; }
		IType Type { get; }
		MethodParameterKind Kind { get; }
	}

	public class MethodParameterReference : IMethodParameterReference
	{
		public ushort Index { get; set; }
		public IType Type { get; set; }
		public MethodParameterKind Kind { get; set; }

		public MethodParameterReference(ushort index, IType type)
		{
			this.Index = index;
			this.Type = type;
			this.Kind = MethodParameterKind.In;
		}

		public override string ToString()
		{
			string kind;

			switch (this.Kind)
			{
				case MethodParameterKind.In: kind = string.Empty; break;
				case MethodParameterKind.Out: kind = "out "; break;
				case MethodParameterKind.Ref: kind = "ref "; break;

				default: throw this.Kind.ToUnknownValueException();
			}

			return string.Format("{0}{1}", kind, this.Type);
		}

		public override int GetHashCode()
		{
			return this.Type.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as IMethodParameterReference;

			var result = other != null &&
						 this.Kind == other.Kind &&
						 this.Type.Equals(other.Type);

			return result;
		}
	}

	public class MethodParameter : IMethodParameterReference
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public ushort Index { get; set; }
		public string Name { get; set; }
		public IType Type { get; set; }
		public MethodParameterKind Kind {get; set; }
		public Constant DefaultValue { get; set; }

		public MethodParameter(ushort index, string name, IType type)
		{
			this.Index = index;
			this.Name = name;
			this.Type = type;
			this.Kind = MethodParameterKind.In;
			this.Attributes = new HashSet<CustomAttribute>();
		}

		public bool HasDefaultValue
		{
			get { return this.DefaultValue != null; }
		}

		public bool MatchReference(IMethodParameterReference parameter)
		{
			var result = false;

			if (parameter is MethodParameter)
			{
				result = this.Equals(parameter);
			}
			else
			{
				result = this.Kind == parameter.Kind &&
						 this.Type.Equals(parameter.Type);
			}

			return result;
		}

		public override string ToString()
		{
			string kind;

			switch (this.Kind)
			{
				case MethodParameterKind.In: kind = string.Empty; break;
				case MethodParameterKind.Out: kind = "out "; break;
				case MethodParameterKind.Ref: kind = "ref "; break;

				default: throw this.Kind.ToUnknownValueException();
			}

			return string.Format("{0}{1} {2}", kind, this.Type, this.Name);
		}
	}

	public interface IMethodReference : ITypeMemberReference, IMetadataReference, IGenericReference
	{
		IType ReturnType { get; }
		string Name { get; }
		string GenericName { get; }
		IList<IMethodParameterReference> Parameters { get; }
		IList<IType> GenericArguments { get; }
		IMethodReference GenericMethod { get; }
		MethodDefinition ResolvedMethod { get; }
		bool IsStatic { get; }
	}

	public class MethodReference : IMethodReference
	{
		private Func<MethodDefinition> ResolveMethod;
		private MethodDefinition resolvedMethod;

		public ISet<CustomAttribute> Attributes { get; private set; }
		public IBasicType ContainingType { get; set; }
		public IType ReturnType { get; set; }
		public string Name { get; set; }
		public int GenericParameterCount { get; set; }
		public IList<IType> GenericArguments { get; private set; }
		public IList<IMethodParameterReference> Parameters { get; private set; }
		public IMethodReference GenericMethod { get; set; }
		public bool IsStatic { get; set; }

		public MethodReference(string name, IType returnType)
		{
			this.Name = name;
			this.ReturnType = returnType;
			this.Parameters = new List<IMethodParameterReference>();
			this.Attributes = new HashSet<CustomAttribute>();
			this.GenericArguments = new List<IType>();

			this.ResolveMethod = () =>
			{
				var msg = "Use Resolve method to bind this reference with some host.";

				throw new InvalidOperationException(msg);
			};
		}

		public string GenericName
		{
			get
			{
				var arguments = string.Empty;

				if (this.GenericArguments.Count > 0)
				{
					arguments = string.Join(", ", this.GenericArguments);
					arguments = string.Format("<{0}>", arguments);
				}
				else if (this.GenericParameterCount > 0)
				{
					var startIndex = this.ContainingType.GenericParameterCount + 1;
					arguments = string.Join(", T", Enumerable.Range(startIndex, this.GenericParameterCount));
					arguments = string.Format("<T{0}>", arguments);
				}
				//else if (this.GenericParameterCount > 0)
				//{
				//	var startIndex = this.ContainingType.TotalGenericParameterCount();
				//	arguments = string.Join(", T", Enumerable.Range(startIndex, this.GenericParameterCount));
				//	arguments = string.Format("<T{0}>", arguments);
				//}

				return string.Format("{0}{1}", this.Name, arguments);
			}
		}

		public MethodDefinition ResolvedMethod
		{
			get
			{
				if (resolvedMethod == null)
				{
					resolvedMethod = ResolveMethod();
				}

				return resolvedMethod;
			}
		}

		//public MethodDefinition Resolve(Host host)
		//{
		//	this.ResolvedMethod = host.ResolveReference(this) as MethodDefinition;
		//	return this.ResolvedMethod;
		//}

		public void Resolve(Host host)
		{
			ResolveMethod = () => host.ResolveReference(this) as MethodDefinition;
		}

		public override string ToString()
		{
			return this.ToSignatureString();
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as IMethodReference;

			var result = other != null &&
						 this.Name == other.Name &&
						 this.IsStatic == other.IsStatic &&
						 this.GenericParameterCount == other.GenericParameterCount &&
						 this.ReturnType.Equals(other.ReturnType) &&
						 this.ContainingType.Equals(other.ContainingType) &&
						 this.GenericArguments.SequenceEqual(other.GenericArguments) &&
						 this.Parameters.SequenceEqual(other.Parameters);

			return result;
		}
	}

	public class MethodDefinition : ITypeMemberDefinition, IMethodReference, IGenericDefinition
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public ITypeDefinition ContainingType { get; set; }
		public IType ReturnType { get; set; }
		public string Name { get; set; }
		public IList<GenericParameter> GenericParameters { get; private set; }
		public IList<MethodParameter> Parameters { get; private set; }
		public bool IsStatic { get; set; }
		public bool IsAbstract { get; set; }
		public bool IsVirtual { get; set; }
		public bool IsConstructor { get; set; }
		public MethodBody Body { get; set; }

		public MethodDefinition(string name, IType returnType)
		{
			this.Name = name;
			this.ReturnType = returnType;
			this.Attributes = new HashSet<CustomAttribute>();
			this.GenericParameters = new List<GenericParameter>();
			this.Parameters = new List<MethodParameter>();
		}

		public bool HasBody
		{
			get { return this.Body != null; }
		}

		public string GenericName
		{
			get
			{
				var parameters = string.Empty;

				if (this.GenericParameters.Count > 0)
				{
					parameters = string.Join(", ", this.GenericParameters);
					parameters = string.Format("<{0}>", parameters);
				}

				return string.Format("{0}{1}", this.Name, parameters);
			}
		}

		#region ITypeMemberReference members

		IBasicType ITypeMemberReference.ContainingType
		{
			get { return this.ContainingType; }
		}

		#endregion

		#region IGenericReference members

		int IGenericReference.GenericParameterCount
		{
			get { return this.GenericParameters.Count; }
		}

		#endregion

		#region IMethodReference members

		IList<IMethodParameterReference> IMethodReference.Parameters
		{
			get { return new List<IMethodParameterReference>(this.Parameters); }
		}

		IList<IType> IMethodReference.GenericArguments
		{
			get { return new List<IType>(); }
		}

		MethodDefinition IMethodReference.ResolvedMethod
		{
			get { return this; }
		}

		public IMethodReference GenericMethod
		{
			get { return null; }
		}

		#endregion

		public bool MatchReference(ITypeMemberReference member)
		{
			var result = false;

			if (member is MethodDefinition)
			{
				result = this.Equals(member);
			}
			else if (member is IMethodReference)
			{
				var method = member as IMethodReference;

				if (method.GenericMethod != null)
				{
					method = method.GenericMethod;
				}

				result = this.ContainingType.MatchReference(method.ContainingType) &&
						 this.MatchSignature(method);
			}

			return result;
		}

		public bool MatchSignature(IMethodReference method)
		{
			var result = this.Name == method.Name &&
						 this.IsStatic == method.IsStatic &&
						 this.GenericParameters.Count == method.GenericParameterCount &&
						 this.ReturnType.Equals(method.ReturnType) &&
						 this.MatchParameters(method);
			return result;
		}

		public bool MatchParameters(IMethodReference method)
		{
			var result = false;

			if (this.Parameters.Count == method.Parameters.Count)
			{
				result = true;

				for (var i = 0; i < this.Parameters.Count && result; ++i)
				{
					var parameterdef = this.Parameters[i];
					var parameterref = method.Parameters[i];

					result = parameterdef.MatchReference(parameterref);
				}
			}

			return result;
		}

		public string ToSignatureString()
		{
			var result = new StringBuilder();

			if (this.IsStatic)
			{
				result.Append("static ");
			}

			if (this.IsAbstract)
			{
				result.Append("abstract ");
			}

			if (this.IsVirtual)
			{
				result.Append("virtual ");
			}

			result.AppendFormat("{0} {1}::{2}", this.ReturnType, this.ContainingType.GenericName, this.GenericName);

			var parameters = string.Join(", ", this.Parameters);
			result.AppendFormat("({0})", parameters);

			return result.ToString();
		}

		public override string ToString()
		{
			var result = new StringBuilder();

			var signature = this.ToSignatureString();
			result.Append(signature);

			if (this.HasBody)
			{
				result.AppendLine();
				result.AppendLine("{");
				result.Append(this.Body);
				result.AppendLine("}");
			}

			return result.ToString();
		}
	}

	public class EnumDefinition : IValueTypeDefinition
	{
		public Assembly ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public ITypeDefinition ContainingType { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public string Name { get; set; }
		public IBasicType UnderlayingType { get; set; }
		public IList<ConstantDefinition> Constants { get; private set; }

		public EnumDefinition(string name)
		{
			this.Name = name;
			this.Attributes = new HashSet<CustomAttribute>();
			this.Constants = new List<ConstantDefinition>();
		}

		public string GenericName
		{
			get { return this.Name; }
		}

		public IEnumerable<ITypeMemberDefinition> Members
		{
			get { return this.Constants; }
		}

		#region ITypeMemberReference members

		IBasicType ITypeMemberReference.ContainingType
		{
			get { return this.ContainingType; }
		}

		#endregion

		#region IGenericReference members

		int IGenericReference.GenericParameterCount
		{
			get { return 0; }
		}

		#endregion

		#region IBasicType members

		IAssemblyReference IBasicType.ContainingAssembly
		{
			get { return this.ContainingAssembly; }
		}

		string IBasicType.ContainingNamespace
		{
			get { return this.ContainingNamespace.FullName; }
		}

		IList<IType> IBasicType.GenericArguments
		{
			get { return new List<IType>(); }
		}

		ITypeDefinition IBasicType.ResolvedType
		{
			get { return this; }
		}

		public TypeKind TypeKind
		{
			get { return TypeKind.ValueType; }
		}

		IBasicType IBasicType.GenericType
		{
			get { return null; }
		}

		#endregion

		public bool MatchReference(ITypeMemberReference member)
		{
			var type = member as IBasicType;
			var result = type != null && this.MatchReference(type);

			return result;
		}

		public bool MatchReference(IBasicType type)
		{
			var result = false;

			if (type is ITypeDefinition)
			{
				result = this.Equals(type);
			}
			else
			{
				// TODO: Maybe we should also compare the TypeKind?
				result = this.Name == type.Name &&
						 this.ContainingNamespace.FullName == type.ContainingNamespace &&
						 this.ContainingAssembly.MatchReference(type.ContainingAssembly) &&
						 this.ContainingType.BothNullOrMatchReference(type.ContainingType);
			}

			return result;
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			result.AppendFormat("enum {0} : {1}\n", this.Name, this.UnderlayingType);
			result.AppendLine("{");

			foreach (var constant in this.Constants)
			{
				result.AppendFormat("  {0};\n", constant);
			}

			result.AppendLine("}");
			return result.ToString();
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}
	}

	public class ConstantDefinition : ITypeMemberDefinition
	{
		public ITypeDefinition ContainingType { get; set; }
		public string Name { get; set; }
		public Constant Value { get; set; }

		public ConstantDefinition(string name, Constant value)
		{
			this.Name = name;
			this.Value = value;
		}

		#region ITypeMemberReference members

		IBasicType ITypeMemberReference.ContainingType
		{
			get { return this.ContainingType; }
		}

		#endregion

		public bool MatchReference(ITypeMemberReference member)
		{
			var result = false;

			if (member is ITypeMemberDefinition)
			{
				result = this.Equals(member);
			}
			else
			{
				var constant = member as ConstantDefinition;

				result = constant != null &&
						 this.Name == constant.Name &&
						 this.ContainingType.MatchReference(constant.ContainingType) &&
						 this.Value.Equals(constant.Value);
			}

			return result;
		}

		public override string ToString()
		{
			return string.Format("{0} = {1}", this.Name, this.Value);
		}
	}

	public class InterfaceDefinition : IReferenceTypeDefinition, IGenericDefinition
	{
		public Assembly ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public ITypeDefinition ContainingType { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public string Name { get; set; }
		public IList<IBasicType> Interfaces { get; private set; }
		public IList<GenericParameter> GenericParameters { get; private set; }
		public IList<MethodDefinition> Methods { get; private set; }

		public InterfaceDefinition(string name)
		{
			this.Name = name;
			this.Attributes = new HashSet<CustomAttribute>();
			this.Interfaces = new List<IBasicType>();
			this.GenericParameters = new List<GenericParameter>();
			this.Methods = new List<MethodDefinition>();
		}

		public string GenericName
		{
			get
			{
				var parameters = string.Empty;

				if (this.GenericParameters.Count > 0)
				{
					parameters = string.Join(", ", this.GenericParameters);
					parameters = string.Format("<{0}>", parameters);
				}

				return string.Format("{0}{1}", this.Name, parameters);
			}
		}

		public IEnumerable<ITypeMemberDefinition> Members
		{
			get { return this.Methods; }
		}

		#region ITypeMemberReference members

		IBasicType ITypeMemberReference.ContainingType
		{
			get { return this.ContainingType; }
		}

		#endregion

		#region IGenericReference members

		int IGenericReference.GenericParameterCount
		{
			get { return this.GenericParameters.Count; }
		}

		#endregion

		#region IBasicType members

		IAssemblyReference IBasicType.ContainingAssembly
		{
			get { return this.ContainingAssembly; }
		}

		string IBasicType.ContainingNamespace
		{
			get { return this.ContainingNamespace.FullName; }
		}

		IList<IType> IBasicType.GenericArguments
		{
			get { return new List<IType>(); }
		}

		ITypeDefinition IBasicType.ResolvedType
		{
			get { return this; }
		}

		public TypeKind TypeKind
		{
			get { return TypeKind.ReferenceType; }
		}

		IBasicType IBasicType.GenericType
		{
			get { return null; }
		}

		#endregion

		public bool MatchReference(ITypeMemberReference member)
		{
			var type = member as IBasicType;
			var result = type != null && this.MatchReference(type);

			return result;
		}

		public bool MatchReference(IBasicType type)
		{
			var result = false;

			if (type is ITypeDefinition)
			{
				result = this.Equals(type);
			}
			else
			{
				if (type.GenericType != null)
				{
					type = type.GenericType;
				}

				// TODO: Maybe we should also compare the TypeKind?
				result = this.Name == type.Name &&
						 this.GenericParameters.Count == type.GenericParameterCount &&
						 this.ContainingNamespace.FullName == type.ContainingNamespace &&
						 this.ContainingAssembly.MatchReference(type.ContainingAssembly) &&
						 this.ContainingType.BothNullOrMatchReference(type.ContainingType);
			}

			return result;
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			result.AppendFormat("interface {0}", this.GenericName);

			if (this.Interfaces.Count > 0)
			{
				var interfaces = string.Join(", ", this.Interfaces);
				result.AppendFormat(" : {0}", interfaces);
			}

			result.AppendLine();
			result.AppendLine("{");

			foreach (var method in this.Methods)
			{
				result.AppendFormat("  {0};\n", method);
			}

			result.AppendLine("}");
			return result.ToString();
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}
	}

	public class ClassDefinition : IReferenceTypeDefinition, IGenericDefinition, ITypeDefinitionContainer
	{
		public Assembly ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public ITypeDefinition ContainingType { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public string Name { get; set; }
		public IBasicType Base { get; set; }
		public IList<IBasicType> Interfaces { get; private set; }
		public IList<GenericParameter> GenericParameters { get; private set; }
		public IList<FieldDefinition> Fields { get; private set; }
		public IList<MethodDefinition> Methods { get; private set; }
		public IList<ITypeDefinition> Types { get; private set; }

		// TODO: Fix! Remove this property when creating a specific DelegateDefinition model class.
		public bool IsDelegate { get; set; }

		public ClassDefinition(string name)
		{
			this.Name = name;
			this.Attributes = new HashSet<CustomAttribute>();
			this.Interfaces = new List<IBasicType>();
			this.GenericParameters = new List<GenericParameter>();
			this.Fields = new List<FieldDefinition>();
			this.Methods = new List<MethodDefinition>();
			this.Types = new List<ITypeDefinition>();
		}

		public string GenericName
		{
			get
			{
				var parameters = string.Empty;

				if (this.GenericParameters.Count > 0)
				{
					parameters = string.Join(", ", this.GenericParameters);
					parameters = string.Format("<{0}>", parameters);
				}

				return string.Format("{0}{1}", this.Name, parameters);
			}
		}

		public IEnumerable<ITypeMemberDefinition> Members
		{
			get
			{
				var result = this.Types.AsEnumerable<ITypeMemberDefinition>();
				result = result.Union(this.Fields);
				result = result.Union(this.Methods);
				return result;
			}
		}

		#region ITypeMemberReference members

		IBasicType ITypeMemberReference.ContainingType
		{
			get { return this.ContainingType; }
		}

		#endregion

		#region IGenericReference members

		int IGenericReference.GenericParameterCount
		{
			get { return this.GenericParameters.Count; }
		}

		#endregion

		#region IBasicType members

		IAssemblyReference IBasicType.ContainingAssembly
		{
			get { return this.ContainingAssembly; }
		}

		string IBasicType.ContainingNamespace
		{
			get { return this.ContainingNamespace.FullName; }
		}

		IList<IType> IBasicType.GenericArguments
		{
			get { return new List<IType>(); }
		}

		ITypeDefinition IBasicType.ResolvedType
		{
			get { return this; }
		}

		public TypeKind TypeKind
		{
			get { return TypeKind.ReferenceType; }
		}

		IBasicType IBasicType.GenericType
		{
			get { return null; }
		}

		#endregion

		public bool MatchReference(ITypeMemberReference member)
		{
			var type = member as IBasicType;
			var result = type != null && this.MatchReference(type);

			return result;
		}

		public bool MatchReference(IBasicType type)
		{
			var result = false;

			if (type is ITypeDefinition)
			{
				result = this.Equals(type);
			}
			else
			{
				if (type.GenericType != null)
				{
					type = type.GenericType;
				}

				// TODO: Maybe we should also compare the TypeKind?
				result = this.Name == type.Name &&
						 this.GenericParameters.Count == type.GenericParameterCount &&
						 this.ContainingNamespace.FullName == type.ContainingNamespace &&
						 this.ContainingAssembly.MatchReference(type.ContainingAssembly) &&
						 this.ContainingType.BothNullOrMatchReference(type.ContainingType);
			}

			return result;
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			result.AppendFormat("class {0}", this.GenericName);

			result.AppendFormat(" : {0}", this.Base);

			if (this.Interfaces.Count > 0)
			{
				var interfaces = string.Join(", ", this.Interfaces);
				result.AppendFormat(", {0}", interfaces);
			}

			result.AppendLine();
			result.AppendLine("{");

			foreach (var type in this.Types)
			{
				var typeString = type.ToString();
				typeString = typeString.Replace("\n", "\n  ");
				result.AppendFormat("{0}\n", typeString);
			}

			foreach (var field in this.Fields)
			{
				result.AppendFormat("  {0};\n", field);
			}

			foreach (var method in this.Methods)
			{
				var methodString = method.ToString();
				methodString = methodString.Replace("\n", "\n  ");
				result.AppendFormat("{0}\n", methodString);
			}

			result.AppendLine("}");
			return result.ToString();
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}
	}

	public enum MethodBodyKind
	{
		Bytecode,
		ThreeAddressCode,
		StaticSingleAssignment
	}

	public class MethodBody : IInstructionContainer
	{
		public IList<IVariable> Parameters { get; private set; }
		public ISet<IVariable> LocalVariables { get; private set; }
		public IList<IInstruction> Instructions { get; private set; }
		public IList<ProtectedBlock> ExceptionInformation { get; private set; }
		public ushort MaxStack { get; set; }
		public MethodBodyKind Kind { get; set; }

		public MethodBody(MethodBodyKind kind)
		{
			this.Kind = kind;
			this.Parameters = new List<IVariable>();
			this.LocalVariables = new HashSet<IVariable>();
			this.Instructions = new List<IInstruction>();
			this.ExceptionInformation = new List<ProtectedBlock>();
		}

		public void UpdateVariables()
		{
			this.LocalVariables.Clear();
			//this.LocalVariables.AddRange(this.Parameters);

			// TODO: SSA is not inserting phi instructions into method's body instructions collection.

			foreach (var instruction in this.Instructions)
			{
				this.LocalVariables.AddRange(instruction.Variables);
			}

			this.LocalVariables.ExceptWith(this.Parameters);
		}

		public override string ToString()
		{
			var result = new StringBuilder();

			if (this.Parameters.Count > 0)
			{
				foreach (var parameter in this.Parameters)
				{
					result.AppendFormat("  parameter {0} {1};", parameter.Type, parameter.Name);
					result.AppendLine();
				}

				result.AppendLine();
			}

			if (this.LocalVariables.Count > 0)
			{
				foreach (var local in this.LocalVariables)
				{
					result.AppendFormat("  {0} {1};", local.Type, local.Name);
					result.AppendLine();
				}

				result.AppendLine();
			}

			foreach (var instruction in this.Instructions)
			{
				result.Append("  ");
				result.Append(instruction);
				result.AppendLine();
			}

			foreach (var handler in this.ExceptionInformation)
			{
				result.AppendLine();
				result.Append(handler);
			}

			return result.ToString();
		}
	}
}
