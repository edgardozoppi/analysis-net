using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.ThreeAddressCode.Types
{
	public interface ITypeDefinition
	{
		string Name { get; set; }
	}

	public interface IValueTypeDefinition : ITypeDefinition
	{
	}

	public interface IReferenceTypeDefinition : ITypeDefinition
	{
	}

	public class StructDefinition : IValueTypeDefinition
	{
		public string Name { get; set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
		public IDictionary<string, FieldDefinition> Fields { get; private set; }
		public IDictionary<string, MethodDefinition> Methods { get; private set; }

		public StructDefinition(string name)
		{
			this.Name = name;
			this.GenericParameters = new List<TypeVariable>();
			this.Fields = new Dictionary<string, FieldDefinition>();
			this.Methods = new Dictionary<string, MethodDefinition>();
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

			foreach (var field in this.Fields.Values)
			{
				result.AppendFormat("  {0};\n", field);
			}

			foreach (var method in this.Methods.Values)
			{
				result.AppendFormat("  {0};\n", method);
			}

			result.AppendLine("}");
			return result.ToString();
		}
	}

	public class FieldDefinition
	{
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

	public class MethodDefinition
	{
		public IType ReturnType { get; set; }
		public string Name { get; set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
		public IList<IVariable> Parameters { get; private set; }
		public bool IsStatic { get; set; }
		public bool IsConstructor { get; set; }

		public MethodDefinition(string name, IType returnType)
		{
			this.Name = name;
			this.ReturnType = returnType;
			this.GenericParameters = new List<TypeVariable>();
			this.Parameters = new List<IVariable>();
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
		public string Name { get; set; }
		public BasicType UnderlayingType { get; set; }
		public IDictionary<string, Constant> Constants { get; private set; }

		public EnumDefinition(string name)
		{
			this.Name = name;
			this.Constants = new Dictionary<string, Constant>();
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			result.AppendFormat("enum {0} : {1}\n", this.Name, this.UnderlayingType);
			result.AppendLine("{");

			foreach (var constant in this.Constants)
			{
				result.AppendFormat("  {0} = {1};\n", constant.Key, constant.Value);
			}

			result.AppendLine("}");
			return result.ToString();
		}
	}

	public class InterfaceDefinition : IReferenceTypeDefinition
	{
		public string Name { get; set; }
		public IList<BasicType> Interfaces { get; private set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
		public IDictionary<string, MethodDefinition> Methods { get; private set; }

		public InterfaceDefinition(string name)
		{
			this.Name = name;
			this.Interfaces = new List<BasicType>();
			this.GenericParameters = new List<TypeVariable>();
			this.Methods = new Dictionary<string, MethodDefinition>();
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

			foreach (var method in this.Methods.Values)
			{
				result.AppendFormat("  {0};\n", method);
			}

			result.AppendLine("}");
			return result.ToString();
		}
	}
	
	public class ClassDefinition : IReferenceTypeDefinition
	{
		public string Name { get; set; }
		public BasicType Base { get; set; }
		public IList<BasicType> Interfaces { get; private set; }
		public IList<TypeVariable> GenericParameters { get; private set; }
		public IDictionary<string, FieldDefinition> Fields { get; private set; }
		public IDictionary<string, MethodDefinition> Methods { get; private set; }

		public ClassDefinition(string name)
		{
			this.Name = name;
			this.Interfaces = new List<BasicType>();
			this.GenericParameters = new List<TypeVariable>();
			this.Fields = new Dictionary<string, FieldDefinition>();
			this.Methods = new Dictionary<string, MethodDefinition>();
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

			foreach (var field in this.Fields.Values)
			{
				result.AppendFormat("  {0};\n", field);
			}

			foreach (var field in this.Fields.Values)
			{
				result.AppendFormat("  {0};\n", field);
			}

			foreach (var method in this.Methods.Values)
			{
				result.AppendFormat("  {0};\n", method);
			}

			result.AppendLine("}");
			return result.ToString();
		}
	}
}
