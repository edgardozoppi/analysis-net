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
		IEnumerable<ITypeMemberDefinition> Members { get; }

		bool MatchReference(IBasicType type);
	}

    public interface IGenericTypeDefinition : ITypeDefinition
    {
        IList<TypeVariable> GenericParameters { get; }
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

	public class StructDefinition : IValueTypeDefinition, IGenericTypeDefinition, ITypeDefinitionContainer
	{
		public Assembly ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public ITypeDefinition ContainingType { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public string Name { get; set; }
		public IList<IBasicType> Interfaces { get; private set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
		public IList<FieldDefinition> Fields { get; private set; }
		public IList<MethodDefinition> Methods { get; private set; }
		public IList<ITypeDefinition> Types { get; private set; }

		public StructDefinition(string name)
		{
			this.Name = name;
			this.Attributes = new HashSet<CustomAttribute>();
			this.Interfaces = new List<IBasicType>();
			this.GenericParameters = new List<TypeVariable>();
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

        string IBasicType.ContainingTypes
        {
            get { return this.GetContainingTypes(); }
        }

        IBasicType IBasicType.GenericType
        {
            get { return null; }
        }

        int IBasicType.GenericParameterCount
        {
            get { return this.GenericParameters.Count; }
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
			if (type is ITypeDefinition)
			{
				return this.Equals(type);
			}

			if (type.GenericType != null)
            {
                type = type.GenericType;
            }

			// TODO: Maybe we should also compare the TypeKind?
			var result = this.Name == type.Name &&
						 this.GenericParameters.Count == type.GenericArguments.Count &&
						 this.ContainingNamespace.FullName == type.ContainingNamespace &&
						 this.ContainingAssembly.MatchReference(type.ContainingAssembly) &&
						 this.GetContainingTypes() == type.ContainingTypes;
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
			if (member is ITypeMemberDefinition)
			{
				return this.Equals(member);
			}

			var field = member as IFieldReference;

			var result = field != null &&
						 this.Name == field.Name &&
						 this.IsStatic == field.IsStatic &&
						 this.ContainingType.MatchReference(field.ContainingType) &&
						 this.Type.Equals(field.Type);
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
		IType Type { get; }
		MethodParameterKind Kind { get; }
	}

	public class MethodParameterReference : IMethodParameterReference
	{
		public IType Type { get; set; }
		public MethodParameterKind Kind { get; set; }

		public MethodParameterReference(IType type)
		{
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
		public string Name { get; set; }
		public IType Type { get; set; }
		public MethodParameterKind Kind {get; set; }

		public MethodParameter(string name, IType type)
		{
			this.Name = name;
			this.Type = type;
			this.Kind = MethodParameterKind.In;
			this.Attributes = new HashSet<CustomAttribute>();
		}

        public bool MatchReference(IMethodParameterReference parameter)
        {
            var result = MatchReference(parameter, null);
            return result;
        }

        public bool MatchReference(IMethodParameterReference parameter, IDictionary<IType, IType> typeParameterBinding)
		{
			if (parameter is MethodParameter)
			{
				return this.Equals(parameter);
			}

			var result = this.Kind == parameter.Kind &&
                this.Type.MatchType(parameter.Type, typeParameterBinding);
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

	public interface IMethodReference : ITypeMemberReference, IMetadataReference
	{
		IType ReturnType { get; }
		string Name { get; }
		int GenericParameterCount { get; }
		IList<IMethodParameterReference> Parameters { get; }
        IList<IType> GenericArguments { get; }
        IMethodReference GenericMethod { get; }
        bool IsStatic { get; }
    }

	public class MethodReference : IMethodReference
	{
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
		}

		public override string ToString()
		{
			var result = new StringBuilder();

			if (this.IsStatic)
			{
				result.Append("static ");
			}

			result.AppendFormat("{0} {1}::{2}", this.ReturnType, this.ContainingType, this.Name);

			if (this.GenericArguments.Count > 0)
			{
				var gparameters = string.Join(", ", this.GenericArguments);
				result.AppendFormat("<{0}>", gparameters);
			}

			var parameters = string.Join(", ", this.Parameters);
			result.AppendFormat("({0})", parameters);
			return result.ToString();
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
						 this.Parameters.SequenceEqual(other.Parameters);

			return result;
		}
	}

	public class MethodDefinition : ITypeMemberDefinition, IMethodReference
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public ITypeDefinition ContainingType { get; set; }
		public IType ReturnType { get; set; }
		public string Name { get; set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
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
			this.GenericParameters = new List<TypeVariable>();
			this.Parameters = new List<MethodParameter>();
			this.Body = new MethodBody();
		}

		#region ITypeMemberReference members

		IBasicType ITypeMemberReference.ContainingType
		{
			get { return this.ContainingType; }
		}

		#endregion

		#region IMethodReference members

		int IMethodReference.GenericParameterCount
		{
			get { return this.GenericParameters.Count; }
		}

		IList<IMethodParameterReference> IMethodReference.Parameters
		{
			get { return new List<IMethodParameterReference>(this.Parameters); }
		}

        IList<IType> IMethodReference.GenericArguments
        {
            get { return new List<IType>(); }
        }

        public IMethodReference GenericMethod
        {
            get { return null; }
        }

        #endregion

        public bool MatchReference(ITypeMemberReference member)
		{
			var method = member as IMethodReference;
            var result = false;

            if (method != null)
            {
				if (method is MethodDefinition)
				{
					return this.Equals(method);
				}

				if (!MatchBasicSignature(method))
                    return false;

                var typeParameterBinding = new Dictionary<IType, IType>();
                var containingType = method.ContainingType;

                if (this.ContainingType is IGenericTypeDefinition &&
                    containingType.GenericType != null)
                {
                    var genericContainingType = this.ContainingType as IGenericTypeDefinition;

                    for (var i = 0; i < genericContainingType.GenericParameterCount; ++i)
                    {
                        var typeParameter = genericContainingType.GenericParameters[i];
                        var typeArgument = containingType.GenericArguments[i];

                        typeParameterBinding.Add(typeParameter, typeArgument);
                    }

                    containingType = containingType.GenericType;
                }

                if (method.GenericMethod != null)
                {
                    if (this.GenericParameters.Count != method.GenericParameterCount)
                        return false;

                    for (var i = 0; i < this.GenericParameters.Count; ++i)
                    {
                        var typeParameter = this.GenericParameters[i];
                        var typeArgument = method.GenericArguments[i];

                        typeParameterBinding.Add(typeParameter, typeArgument);
                    }

                    method = method.GenericMethod;
                }
                
                result = method != null &&
                             this.ContainingType.MatchReference(containingType) &&
                             this.MatchSignature(method, typeParameterBinding);
            }

			//var result = method != null &&
			//			 this.ContainingType.MatchReference(method.ContainingType) &&
			//			 this.MatchSignature(method);
			return result;
		}

        public bool MatchSignature(IMethodReference method)
        {
            var result = MatchSignature(method, null);
            return result;
        }

        public bool MatchBasicSignature(IMethodReference method)
        {
            var result = this.Name == method.Name &&
                         this.IsStatic == method.IsStatic &&
                         this.GenericParameters.Count == method.GenericParameterCount;
            return result;
        }

        public bool MatchSignature(IMethodReference method, IDictionary<IType, IType> typeParameterBinding)
		{
            var result = MatchBasicSignature(method) && 
						 this.GenericParameters.Count == method.GenericParameterCount &&
                         this.ReturnType.MatchType(method.ReturnType, typeParameterBinding) &&
						 this.MatchParameters(method, typeParameterBinding);
			return result;
		}

		public bool MatchParameters(IMethodReference method, IDictionary<IType, IType> typeParameterBinding)
		{
			var result = false;

			if (this.Parameters.Count == method.Parameters.Count)
			{
				result = true;

				for (var i = 0; i < this.Parameters.Count && result; ++i)
				{
					var parameterdef = this.Parameters[i];
					var parameterref = method.Parameters[i];

                    result = parameterdef.MatchReference(parameterref, typeParameterBinding);
				}
			}

			return result;
		}

		public override string ToString()
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

			result.AppendFormat("{0} {1}::{2}", this.ReturnType, this.ContainingType.Name, this.Name);

			if (this.GenericParameters.Count > 0)
			{
				var gparameters = string.Join(", ", this.GenericParameters);
				result.AppendFormat("<{0}>", gparameters);
			}

			var parameters = string.Join(", ", this.Parameters);
			result.AppendFormat("({0})", parameters);

			if (this.Body.Instructions.Count > 0)
			{
				result.AppendLine();
				result.AppendLine("{");
				result.Append(this.Body);
				result.AppendLine("}");
			}

			return result.ToString();
		}

        public  string ToSignatureString()
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

            result.AppendFormat("{0} {1}::{2}", this.ReturnType, this.ContainingType.Name, this.Name);

            if (this.GenericParameters.Count > 0)
            {
                var gparameters = string.Join(", ", this.GenericParameters);
                result.AppendFormat("<{0}>", gparameters);
            }

            var parameters = string.Join(", ", this.Parameters);
            result.AppendFormat("({0})", parameters);
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

        string IBasicType.ContainingTypes
        {
            get { return this.GetContainingTypes(); }
        }

        IBasicType IBasicType.GenericType
        {
            get { return null; }
        }

        int IBasicType.GenericParameterCount
        {
            get { return 0; }
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
			if (type is ITypeDefinition)
			{
				return this.Equals(type);
			}

			// TODO: Maybe we should also compare the TypeKind?
			var result = this.Name == type.Name &&
						 this.ContainingNamespace.FullName == type.ContainingNamespace &&
						 this.GetContainingTypes() == type.ContainingTypes &&
						 this.ContainingAssembly.MatchReference(type.ContainingAssembly);
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
		public object Value { get; set; }

		public ConstantDefinition(string name, object value)
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
			if (member is ITypeMemberDefinition)
			{
				return this.Equals(member);
			}

			var constant = member as ConstantDefinition;

			var result = constant != null &&
						 this.Name == constant.Name &&
						 this.ContainingType.MatchReference(constant.ContainingType) &&
						 this.Value.Equals(constant.Value);
			return result;
		}

		public override string ToString()
		{
			return string.Format("{0} = {1}", this.Name, this.Value);
		}
	}

	public class InterfaceDefinition : IReferenceTypeDefinition, IGenericTypeDefinition
    {
		public Assembly ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public ITypeDefinition ContainingType { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public string Name { get; set; }
		public IList<IBasicType> Interfaces { get; private set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
		public IList<MethodDefinition> Methods { get; private set; }

		public InterfaceDefinition(string name)
		{
			this.Name = name;
			this.Attributes = new HashSet<CustomAttribute>();
			this.Interfaces = new List<IBasicType>();
			this.GenericParameters = new List<TypeVariable>();
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

        string IBasicType.ContainingTypes
        {
            get { return this.GetContainingTypes(); }
        }

        IBasicType IBasicType.GenericType
        {
            get { return null; }
        }

        int IBasicType.GenericParameterCount
        {
            get { return this.GenericParameters.Count; }
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
			if (type is ITypeDefinition)
			{
				return this.Equals(type);
			}

			if (type.GenericType != null)
            {
                type = type.GenericType;
            }

			// TODO: Maybe we should also compare the TypeKind?
			var result = this.Name == type.Name &&
						 this.GenericParameters.Count == type.GenericParameterCount &&
						 this.ContainingNamespace.FullName == type.ContainingNamespace &&
						 this.ContainingAssembly.MatchReference(type.ContainingAssembly) &&
						 this.GetContainingTypes() == type.ContainingTypes;
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
	
	public class ClassDefinition : IReferenceTypeDefinition, IGenericTypeDefinition, ITypeDefinitionContainer
	{
		public Assembly ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public ITypeDefinition ContainingType { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public string Name { get; set; }
		public IBasicType Base { get; set; }
		public IList<IBasicType> Interfaces { get; private set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
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
			this.GenericParameters = new List<TypeVariable>();
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

		#region IBasicType members

		IAssemblyReference IBasicType.ContainingAssembly
		{
			get { return this.ContainingAssembly; }
		}

		string IBasicType.ContainingNamespace
		{
			get { return this.ContainingNamespace.FullName; }
		}

        string IBasicType.ContainingTypes
        {
            get { return this.GetContainingTypes(); }
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

        int IBasicType.GenericParameterCount
        {
            get { return this.GenericParameters.Count; }
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
			if (type is ITypeDefinition)
			{
				return this.Equals(type);
			}

			if (type.GenericType != null)
            {
                type = type.GenericType;
            }

			// TODO: Maybe we should also compare the TypeKind?
			var result = this.Name == type.Name &&
						 this.GenericParameters.Count == type.GenericParameterCount &&
						 this.ContainingNamespace.FullName == type.ContainingNamespace &&
						 this.ContainingAssembly.MatchReference(type.ContainingAssembly) &&
						 this.GetContainingTypes() == type.ContainingTypes;
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

	public class MethodBody : IInstructionContainer
	{
		public IList<IVariable> Parameters { get; private set; }
		public ISet<IVariable> LocalVariables { get; private set; }
		public IList<IInstruction> Instructions { get; private set; }
		public IList<ProtectedBlock> ExceptionInformation { get; private set; }
		public ushort MaxStack { get; set; }

		public MethodBody()
		{
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
