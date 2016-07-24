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
		BasicType ContainingType { get; set; }
	}

	public interface ITypeMemberDefinition
	{
		ITypeDefinition ContainingType { get; set; }

		bool MatchReference(ITypeMemberReference member);
	}

	public interface ITypeDefinition : ITypeMemberDefinition
	{
		Assembly ContainingAssembly { get; set; }
		Namespace ContainingNamespace { get; set; }
		ISet<CustomAttribute> Attributes { get; }
		string Name { get; }
		string FullName { get; }
		IEnumerable<ITypeMemberDefinition> Members { get; }

		//BasicType GetReference();
		bool MatchReference(BasicType type);
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

	public class StructDefinition : IValueTypeDefinition, ITypeDefinitionContainer
	{
		public Assembly ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public ITypeDefinition ContainingType { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public string Name { get; set; }
		public IList<BasicType> Interfaces { get; private set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
		public IList<FieldDefinition> Fields { get; private set; }
		public IList<MethodDefinition> Methods { get; private set; }
		public IList<ITypeDefinition> Types { get; private set; }

		public StructDefinition(string name)
		{
			this.Name = name;
			this.Attributes = new HashSet<CustomAttribute>();
			this.Interfaces = new List<BasicType>();
			this.GenericParameters = new List<TypeVariable>();
			this.Fields = new List<FieldDefinition>();
			this.Methods = new List<MethodDefinition>();
			this.Types = new List<ITypeDefinition>();
		}

		public string FullName
		{
			get
			{
				var result = new StringBuilder();
				result.Append(this.Name);

				if (this.GenericParameters.Count > 0)
				{
					var parameters = string.Join(", ", this.GenericParameters);
					result.AppendFormat("<{0}>", parameters);
				}

				return result.ToString();
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

		public bool MatchReference(ITypeMemberReference member)
		{
			return false;
		}

		public bool MatchReference(BasicType type)
		{
			// TODO: Maybe we should also compare the TypeKind?
			var result = this.ContainingAssembly.MatchReference(type.Assembly) &&
						 this.ContainingNamespace.FullName == type.Namespace &&
						 this.Name == type.Name &&
						 this.GenericParameters.Count == type.GenericArguments.Count;
			return result;
		}		

		public override string ToString()
		{
			var result = new StringBuilder();
			result.AppendFormat("struct {0}", this.FullName);

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
		public BasicType ContainingType { get; set; }
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

	public class FieldDefinition : ITypeMemberDefinition //, IFieldReference
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

		public bool MatchReference(ITypeMemberReference member)
		{
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
				default: throw new Exception("Unknown MethodParameterKind.");
			}

			return string.Format("{0}{1}", kind, this.Type);
		}
	}

	public class MethodParameter // : IMethodParameterReference
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
			var result = this.Kind == parameter.Kind &&
						 this.Type.Equals(parameter.Type);
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
				default: throw new Exception("Unknown MethodParameterKind.");
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
		bool IsStatic { get; }
	}

	public class MethodReference : IMethodReference
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public BasicType ContainingType { get; set; }
		public IType ReturnType { get; set; }
		public string Name { get; set; }
		public int GenericParameterCount { get; set; }
		public IList<IMethodParameterReference> Parameters { get; private set; }
		public bool IsStatic { get; set; }

		public MethodReference(string name, IType returnType)
		{
			this.Name = name;
			this.ReturnType = returnType;
			this.Parameters = new List<IMethodParameterReference>();
			this.Attributes = new HashSet<CustomAttribute>();
		}

		public override string ToString()
		{
			var result = new StringBuilder();

			if (this.IsStatic)
			{
				result.Append("static ");
			}

			result.AppendFormat("{0} {1}::{2}", this.ReturnType, this.ContainingType, this.Name);

			if (this.GenericParameterCount > 0)
			{
				var gparameters = string.Join(", T", Enumerable.Range(1, this.GenericParameterCount + 1));
				result.AppendFormat("<T{0}>", gparameters);
			}

			var parameters = string.Join(", ", this.Parameters);
			result.AppendFormat("({0})", parameters);
			return result.ToString();
		}
	}

	public class MethodDefinition : ITypeMemberDefinition //, IMethodReference
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public ITypeDefinition ContainingType { get; set; }
		public IType ReturnType { get; set; }
		public string Name { get; set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
		public IList<MethodParameter> Parameters { get; private set; }
		public bool IsStatic { get; set; }
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

		public bool MatchReference(ITypeMemberReference member)
		{
			var method = member as IMethodReference;

			var result = method != null &&
						 this.ContainingType.MatchReference(method.ContainingType) &&
						 this.MatchSignature(method);
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

		public override string ToString()
		{
			var result = new StringBuilder();

			if (this.IsStatic)
			{
				result.Append("static ");
			}

			result.AppendFormat("{0} {1}::{2}", this.ReturnType, this.ContainingType.FullName, this.Name);

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
	}

	public class EnumDefinition : IValueTypeDefinition
	{
		public Assembly ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public ITypeDefinition ContainingType { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public string Name { get; set; }
		public BasicType UnderlayingType { get; set; }
		public IList<ConstantDefinition> Constants { get; private set; }

		public EnumDefinition(string name)
		{
			this.Name = name;
			this.Attributes = new HashSet<CustomAttribute>();
			this.Constants = new List<ConstantDefinition>();
		}

		public string FullName
		{
			get { return this.Name; }
		}

		public IEnumerable<ITypeMemberDefinition> Members
		{
			get { return this.Constants; }
		}

		public bool MatchReference(ITypeMemberReference member)
		{
			return false;
		}

		public bool MatchReference(BasicType type)
		{
			// TODO: Maybe we should also compare the TypeKind?
			var result = this.ContainingAssembly.MatchReference(type.Assembly) &&
						 this.ContainingNamespace.FullName == type.Namespace &&
						 this.Name == type.Name;
			return result;
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			result.AppendFormat("enum {0} : {1}\n", this.FullName, this.UnderlayingType);
			result.AppendLine("{");

			foreach (var constant in this.Constants)
			{
				result.AppendFormat("  {0};\n", constant);
			}

			result.AppendLine("}");
			return result.ToString();
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

		public bool MatchReference(ITypeMemberReference member)
		{
			return false;
		}

		public override string ToString()
		{
			return string.Format("{0} = {1}", this.Name, this.Value);
		}
	}

	public class InterfaceDefinition : IReferenceTypeDefinition
	{
		public Assembly ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public ITypeDefinition ContainingType { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public string Name { get; set; }
		public IList<BasicType> Interfaces { get; private set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
		public IList<MethodDefinition> Methods { get; private set; }

		public InterfaceDefinition(string name)
		{
			this.Name = name;
			this.Attributes = new HashSet<CustomAttribute>();
			this.Interfaces = new List<BasicType>();
			this.GenericParameters = new List<TypeVariable>();
			this.Methods = new List<MethodDefinition>();
		}

		public string FullName
		{
			get
			{
				var result = new StringBuilder();
				result.Append(this.Name);

				if (this.GenericParameters.Count > 0)
				{
					var parameters = string.Join(", ", this.GenericParameters);
					result.AppendFormat("<{0}>", parameters);
				}

				return result.ToString();
			}
		}

		public IEnumerable<ITypeMemberDefinition> Members
		{
			get { return this.Methods; }
		}

		public bool MatchReference(ITypeMemberReference member)
		{
			return false;
		}

		public bool MatchReference(BasicType type)
		{
			// TODO: Maybe we should also compare the TypeKind?
			var result = this.ContainingAssembly.MatchReference(type.Assembly) &&
						 this.ContainingNamespace.FullName == type.Namespace &&
						 this.Name == type.Name &&
						 this.GenericParameters.Count == type.GenericArguments.Count;
			return result;
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			result.AppendFormat("interface {0}", this.FullName);

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
	}
	
	public class ClassDefinition : IReferenceTypeDefinition, ITypeDefinitionContainer
	{
		public Assembly ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public ITypeDefinition ContainingType { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public string Name { get; set; }
		public BasicType Base { get; set; }
		public IList<BasicType> Interfaces { get; private set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
		public IList<FieldDefinition> Fields { get; private set; }
		public IList<MethodDefinition> Methods { get; private set; }
		public IList<ITypeDefinition> Types { get; private set; }

		public ClassDefinition(string name)
		{
			this.Name = name;
			this.Attributes = new HashSet<CustomAttribute>();
			this.Interfaces = new List<BasicType>();
			this.GenericParameters = new List<TypeVariable>();
			this.Fields = new List<FieldDefinition>();
			this.Methods = new List<MethodDefinition>();
			this.Types = new List<ITypeDefinition>();
		}

		public string FullName
		{
			get
			{
				var result = new StringBuilder();
				result.Append(this.Name);

				if (this.GenericParameters.Count > 0)
				{
					var parameters = string.Join(", ", this.GenericParameters);
					result.AppendFormat("<{0}>", parameters);
				}

				return result.ToString();
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

		public bool MatchReference(ITypeMemberReference member)
		{
			return false;
		}

		public bool MatchReference(BasicType type)
		{
			// TODO: Maybe we should also compare the TypeKind?
			var result = this.ContainingAssembly.MatchReference(type.Assembly) &&
						 this.ContainingNamespace.FullName == type.Namespace &&
						 this.Name == type.Name &&
						 this.GenericParameters.Count == type.GenericArguments.Count;
			return result;
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			result.AppendFormat("class {0}", this.FullName);

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
