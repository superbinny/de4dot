/*
    Copyright (C) 2011-2015 de4dot@gmail.com

    This file is part of de4dot.

    de4dot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    de4dot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with de4dot.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using dnlib.DotNet;
// Binny 修改
using de4dot.code.deobfuscators;

namespace de4dot.code.renamer {
	public interface INameCreator {
		string Create();
		// Binny 修改
		string Create(string oldname);
	}

	public class OneNameCreator : INameCreator {
		string name;
		public OneNameCreator(string name) => this.name = name;
		public string Create() => name;
		// Binny 修改
		public string Create(string oldname) {
			return name + "_" + oldname;
		}
	}

	// Binny 修改
	public abstract partial class NameCreatorCounter : INameCreator {
		protected int num;
		// Binny 修改
		public const string separator = "_";
		public string append = ""; //附加的信息

		public abstract string Create();
		// Binny 修改
		public abstract string Create(string oldname);

		public NameCreatorCounter Merge(NameCreatorCounter other) {
			if (num < other.num)
				num = other.num;
			return this;
		}
		// Binny 修改
		public string GetHashWord(string oldname) {
			//取收尾字符，然后计算一个hash的值
			ulong index = (ulong)oldname.GetHashCode();
			index = index % (ulong)m_names.Length;
			return m_names[index];
		}
	}

	public class GenericParamNameCreator : NameCreatorCounter {
		static string[] names = new string[] { "T", "U", "V", "W", "X", "Y", "Z" };

		public override string Create() {
			if (num < names.Length)
				// Binny 修改
				return $"para_{names[num++]}";
			return $"para_{num++}";
		}
		// Binny 修改
		public override string Create(string oldname) {
			return $"para_{oldname}{num++}";
		}
	}
	
	public class NameCreator : NameCreatorCounter {
		string prefix;

		public NameCreator(string prefix) : this(prefix, 0) { }

		public NameCreator(string prefix, int num) {
			this.prefix = prefix;
			this.num = num;
		}

		public NameCreator Clone() => new NameCreator(prefix, num);
		public override string Create() => prefix + num++;
		// Binny 修改
		public override string Create(string oldname) {
			string name = prefix + num++ + separator + GetHashWord(oldname);
			if (append != "")
				name += separator + append;
			return name;
		}
	}

	// Like NameCreator but don't add the counter the first time
	public class NameCreator2 : NameCreatorCounter {
		string prefix;
		// Binny 修改
		//const string separator = "_";

		public NameCreator2(string prefix)
			: this(prefix, 0) {
		}

		public NameCreator2(string prefix, int num) {
			this.prefix = prefix;
			this.num = num;
		}

		public override string Create() {
			string rv;
			if (num == 0)
				rv = prefix;
			else
				rv = prefix + separator + num;
			num++;
			return rv;
		}
		// Binny 修改
		public override string Create(string oldname) {
			string rv;
			if (num == 0)
				rv = prefix;
			else
				rv = prefix + separator + num + separator + GetHashWord(oldname);
			num++;
			return rv;
		}
	}

	public interface ITypeNameCreator {
		string Create(TypeDef typeDef, string newBaseTypeName);
		// Binny 修改
		string Create(FieldDef typeDef, string newBaseTypeName);
	}

	public class NameInfos {
		IList<NameInfo> nameInfos = new List<NameInfo>();

		class NameInfo {
			public string name;
			public NameCreator nameCreator;
			public NameInfo(string name, NameCreator nameCreator) {
				this.name = name;
				this.nameCreator = nameCreator;
			}
		}

		public void Add(string name, NameCreator nameCreator) => nameInfos.Add(new NameInfo(name, nameCreator));

		public NameCreator Find(string typeName) {
			foreach (var nameInfo in nameInfos) {
				if (typeName.Contains(nameInfo.name))
					return nameInfo.nameCreator;
			}

			return null;
		}
	}

	public class TypeNameCreator : ITypeNameCreator {
		ExistingNames existingNames;
		NameCreator createUnknownTypeName;
		NameCreator createEnumName;
		NameCreator createStructName;
		NameCreator createDelegateName;
		NameCreator createClassName;
		NameCreator createInterfaceName;
		// Binny 修改
		NameCreator createFieldName;
		NameInfos nameInfos = new NameInfos();

		public TypeNameCreator(ExistingNames existingNames) {
			this.existingNames = existingNames;
			createUnknownTypeName = CreateNameCreator("Type");
			createEnumName = CreateNameCreator("Enum");
			createStructName = CreateNameCreator("Struct");
			createDelegateName = CreateNameCreator("Delegate");
			createClassName = CreateNameCreator("Class");
			createInterfaceName = CreateNameCreator("Interface");
			// Binny 修改
			createFieldName = CreateNameCreator("Field");

			var names = new string[] {
				"Exception",
				"EventArgs",
				"Attribute",
				"Form",
				"Dialog",
				"Control",
				"Stream",
			};
			foreach (var name in names)
				nameInfos.Add(name, CreateNameCreator(name));
		}

		protected virtual NameCreator CreateNameCreator(string prefix) => new NameCreator(prefix);

		public string Create(TypeDef typeDef, string newBaseTypeName) {
			var nameCreator = GetNameCreator(typeDef, newBaseTypeName);
			return existingNames.GetName(typeDef.Name.String, nameCreator);
		}
		// Binny 修改
		public string Create(FieldDef typeDef, string newBaseTypeName) {
			var nameCreator = GetNameCreator(typeDef, newBaseTypeName);
			return existingNames.GetName(typeDef.Name.String, nameCreator);
		}
		// Binny 修改
		NameCreator GetNameCreator(FieldDef typeDef, string newBaseTypeName) {
			var fn = typeDef.FieldType.FullName;
			createFieldName.append = DeobfuscatorBase.ReplaceValidName(fn);
			return createFieldName;
		}


		NameCreator GetNameCreator(TypeDef typeDef, string newBaseTypeName) {
			var nameCreator = createUnknownTypeName;
			if (typeDef.IsEnum)
				nameCreator = createEnumName;
			else if (typeDef.IsValueType)
				nameCreator = createStructName;
			else if (typeDef.IsClass) {
				if (typeDef.BaseType != null) {
					var fn = typeDef.BaseType.FullName;
					if (fn == "System.Delegate")
						nameCreator = createDelegateName;
					else if (fn == "System.MulticastDelegate")
						nameCreator = createDelegateName;
					else {
						nameCreator = nameInfos.Find(newBaseTypeName ?? typeDef.BaseType.Name.String);
						if (nameCreator == null)
							nameCreator = createClassName;
					}
				}
				else
					nameCreator = createClassName;
			}
			else if (typeDef.IsInterface)
				nameCreator = createInterfaceName;
			// Binny 修改
			else if (typeDef.IsValueType)
				nameCreator = createInterfaceName;
			return nameCreator;
		}
	}

	public class GlobalTypeNameCreator : TypeNameCreator {
		public GlobalTypeNameCreator(ExistingNames existingNames) : base(existingNames) { }
		protected override NameCreator CreateNameCreator(string prefix) => base.CreateNameCreator("G" + prefix);
	}
}
