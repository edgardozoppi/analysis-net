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

	public interface ITypeDefinition
	{
		Assembly ContainingAssembly { get; set; }
		Namespace ContainingNamespace { get; set; }
		ITypeDefinition ContainingType { get; set; }
		string Name { get; }

		bool MatchReference(BasicType type);
	}

	public interface IValueTypeDefinition : ITypeDefinition
	{
	}

	public interface IReferenceTypeDefinition : ITypeDefinition
	{
	}

	public class StructDefinition : IValueTypeDefinition, ITypeDefinitionContainer
	{
		public Assembly ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public ITypeDefinition ContainingType { get; set; }
		public string Name { get; set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
		public IList<FieldDefinition> Fields { get; private set; }
		public IList<MethodDefinition> Methods { get; private set; }
		public IList<ITypeDefinition> Types { get; private set; }

		public StructDefinition(string name)
		{
			this.Name = name;
			this.GenericParameters = new List<TypeVariable>();
			this.Fields = new List<FieldDefinition>();
			this.Methods = new List<MethodDefinition>();
			this.Types = new List<ITypeDefinition>();
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
			result.AppendFormat("struct {0}", this.Name);

			if (this.GenericParameters.Count > 0)
			{
				var gparameters = string.Join(", ", this.GenericParameters);
				result.AppendFormat("<{0}>", gparameters);				
			}

			result.AppendLine();
			result.AppendLine("{");

			foreach (var field in this.Fields)
			{
				result.AppendFormat("  {0};\n", field);
			}

			foreach (var method in this.Methods)
			{
				result.AppendFormat("  {0};\n", method);
			}

			result.AppendLine("}");
			return result.ToString();
		}
	}

	public interface IFieldReference : IMetadataReference
	{
		IType ContainingType { get; }
		IType Type { get; }
		string Name { get; }
		bool IsStatic { get; } 
	}

	public class FieldDefinition //: IFieldReference
	{
		public IType ContainingType { get; set; }
		public IType Type { get; set; }
		public string Name { get; set; }
		public bool IsStatic { get; set; }

		public FieldDefinition(string name, IType type)
		{
			this.Name = name;
			this.Type = type;
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
		public string Name { get; set; }
		public IType Type { get; set; }
		public MethodParameterKind Kind {get; set; }

		public MethodParameter(string name, IType type)
		{
			this.Name = name;
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

			return string.Format("{0}{1} {2}", kind, this.Type, this.Name);
		}
	}

	public interface IMethodReference : IMetadataReference
	{
		IType ContainingType { get; }
		IType ReturnType { get; }
		string Name { get; }
		int GenericParameterCount { get; }
		IList<IMethodParameterReference> Parameters { get; }
		bool IsStatic { get; }
	}

	public class MethodReference : IMethodReference
	{
		public IType ContainingType { get; set; }
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
		}

		public override string ToString()
		{
			var result = new StringBuilder();

			if (this.IsStatic)
			{
				result.Append("static ");
			}

			result.AppendFormat("{0} {1}", this.ReturnType, this.Name);

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

	public class MethodDefinition //: IMethodReference
	{
		public IType ContainingType { get; set; }
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
			this.GenericParameters = new List<TypeVariable>();
			this.Parameters = new List<MethodParameter>();
		}

		public override string ToString()
		{
			var result = new StringBuilder();

			if (this.IsStatic)
			{
				result.Append("static ");
			}

			result.AppendFormat("{0} {1}", this.ReturnType, this.Name);

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
		public string Name { get; set; }
		public BasicType UnderlayingType { get; set; }
		public IList<ConstantDefinition> Constants { get; private set; }

		public EnumDefinition(string name)
		{
			this.Name = name;
			this.Constants = new List<ConstantDefinition>();
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
			result.AppendFormat("enum {0} : {1}\n", this.Name, this.UnderlayingType);
			result.AppendLine("{");

			foreach (var constant in this.Constants)
			{
				result.AppendFormat("  {0};\n", constant);
			}

			result.AppendLine("}");
			return result.ToString();
		}
	}

	public class ConstantDefinition
	{
		public IType ContainingType { get; set; }
		public string Name { get; set; }
		public object Value { get; set; }

		public ConstantDefinition(string name, object value)
		{
			this.Name = name;
			this.Value = value;
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
		public string Name { get; set; }
		public IList<BasicType> Interfaces { get; private set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
		public IList<MethodDefinition> Methods { get; private set; }

		public InterfaceDefinition(string name)
		{
			this.Name = name;
			this.Interfaces = new List<BasicType>();
			this.GenericParameters = new List<TypeVariable>();
			this.Methods = new List<MethodDefinition>();
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
			result.AppendFormat("interface {0}", this.Name);

			if (this.GenericParameters.Count > 0)
			{
				var gparameters = string.Join(", ", this.GenericParameters);
				result.AppendFormat("<{0}>", gparameters);
			}

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
			this.Interfaces = new List<BasicType>();
			this.GenericParameters = new List<TypeVariable>();
			this.Fields = new List<FieldDefinition>();
			this.Methods = new List<MethodDefinition>();
			this.Types = new List<ITypeDefinition>();
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
			result.AppendFormat("class {0}", this.Name);

			if (this.GenericParameters.Count > 0)
			{
				var gparameters = string.Join(", ", this.GenericParameters);
				result.AppendFormat("<{0}>", gparameters);
			}

			result.AppendFormat(" : {0}", this.Base);

			if (this.Interfaces.Count > 0)
			{
				var interfaces = string.Join(", ", this.Interfaces);
				result.AppendFormat(", {0}", interfaces);
			}

			result.AppendLine();
			result.AppendLine("{");

			foreach (var field in this.Fields)
			{
				result.AppendFormat("  {0};\n", field);
			}

			foreach (var field in this.Fields)
			{
				result.AppendFormat("  {0};\n", field);
			}

			foreach (var method in this.Methods)
			{
				result.AppendFormat("  {0};\n", method);
			}

			result.AppendLine("}");
			return result.ToString();
		}
	}

	public class MethodBody
	{
		public IList<IVariable> Parameters { get; private set; }
		public IList<IVariable> LocalVariables { get; private set; }
		public IList<IInstruction> Instructions { get; private set; }
		public IList<IExceptionHandlerBlock> ExceptionInformation { get; private set; }

		public MethodBody()
		{
			this.Parameters = new List<IVariable>();
			this.LocalVariables = new List<IVariable>();
			this.Instructions = new List<IInstruction>();
			this.ExceptionInformation = new List<IExceptionHandlerBlock>();
		}
	}
}
