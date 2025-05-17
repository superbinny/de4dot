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

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using dnlib.DotNet;
using HelpUtil;

namespace de4dot.code.renamer {
	public interface INameCreator {
		string Create();
		string Create(string oldName);
	}

	public class OneNameCreator : INameCreator {
		string name;
		string oldName;
		public OneNameCreator(string name) {
			this.name = name;
		}
		public string Create() => name;
		public string Create(string oldName) => name;
	}

	public abstract class NameCreatorCounter : INameCreator {
		protected int num;

		public abstract string Create();
		public abstract string Create(string oldName);

		public NameCreatorCounter Merge(NameCreatorCounter other) {
			if (num < other.num)
				num = other.num;
			return this;
		}
	}

	public class GenericParamNameCreator : NameCreatorCounter {
		static string[] names = new string[] { "T", "U", "V", "W", "X", "Y", "Z" };

		public override string Create() {
			if (num < names.Length)
				return names[num++];
			return $"T{num++}";
		}
		public override string Create(string oldName) {
			oldName = oldName;
			return this.Create();
		}
	}

	public class NameCreator : NameCreatorCounter {
		EnumFlag prefix;
		public string new_prefix;
		string oldName;
		public NameCreator(EnumFlag prefix) : this(prefix, 0) { }
		public NameCreator(string new_prefix) : this(new_prefix, 0) { }

		public NameCreator(EnumFlag prefix, string oldName) : this(prefix, oldName, 0) { }

		public NameCreator(EnumFlag prefix, int num) {
			this.prefix = prefix;
			this.num = num;
		}

		public NameCreator(EnumFlag prefix, string oldName, int num) {
			this.prefix = prefix;
			this.oldName = oldName;
			this.num = num;
		}

		public NameCreator(string new_prefix, int num) {
			this.prefix = EnumFlag.PREFIX_ADDNEW;
			this.new_prefix = new_prefix;
			this.num = num;
		}

		public NameCreator Clone(string oldName) => new NameCreator(prefix, oldName, num);
		// public override string Create() => prefix + num++;
		public override string Create() {
			if (prefix == EnumFlag.PREFIX_ADDNEW) {
				if (this.new_prefix == "" || this.new_prefix == null) {
					throw new ApplicationException($"Don't allow new_prefix is null");
				}
				return this.new_prefix + num++;
			}
			else
				return prefix.GetString() + num++;
		}
		public override string Create(string oldName) {
			num++;
			string _prefix;
			long next = mRrandomGenerator.Next();
			if (prefix == EnumFlag.PREFIX_ADDNEW)
				_prefix = new_prefix;
			else
				_prefix = prefix.GetString();
			return _prefix + mRrandomGenerator.GetUniqueWord(oldName + next.ToString());
		}

		private RandomNumberGenerator mRrandomGenerator = new RandomNumberGenerator(1);
	}

	// Like NameCreator but don't add the counter the first time
	public class NameCreator2 : NameCreatorCounter {
		EnumFlag prefix;
		string new_prefix;
		const string separator = "_";
		private RandomNumberGenerator mRrandomGenerator = new RandomNumberGenerator(1);

		public NameCreator2(EnumFlag prefix, string oldName)
			: this(prefix, 0) {
		}

		public NameCreator2(string new_prefix) : this(EnumFlag.PREFIX_ADDNEW, 0){
			this.new_prefix = new_prefix;
		}

		public NameCreator2(EnumFlag prefix, int num) {
			this.prefix = prefix;
			this.num = num;
		}

		public override string Create() {
			string rv;
			if (num == 0)
				rv = prefix.GetString();
			else
				rv = prefix.GetString() + separator + num;
			num++;
			return rv;
		}

		public override string Create(string oldName) {
			string rv;
			if (num == 0) {
				if (prefix == EnumFlag.PREFIX_ADDNEW)
					rv = this.new_prefix;
				else
					rv = prefix.GetString();
			}
			else {
				string _prefix;
				if (prefix == EnumFlag.PREFIX_ADDNEW)
					_prefix = this.new_prefix;
				else
					_prefix = prefix.GetString();
				long seed = mRrandomGenerator.Next();
				rv = _prefix + separator + mRrandomGenerator.GetFixWord(oldName + seed.ToString());
			}
			num++;
			return rv;
		}

	}

	public interface ITypeNameCreator {
		string Create(TypeDef typeDef, string newBaseTypeName);
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
		NameInfos nameInfos = new NameInfos();

		public TypeNameCreator(ExistingNames existingNames) {
			this.existingNames = existingNames;
			createUnknownTypeName = CreateNameCreator(EnumFlag.PREFIX_TYPE);
			createEnumName = CreateNameCreator(EnumFlag.PREFIX_ENUM);
			createStructName = CreateNameCreator(EnumFlag.PREFIX_STRUCT);
			createDelegateName = CreateNameCreator(EnumFlag.PREFIX_DELEGATE);
			createClassName = CreateNameCreator(EnumFlag.PREFIX_CLASS);
			createInterfaceName = CreateNameCreator(EnumFlag.PREFIX_INTERFACE);

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

		protected virtual NameCreator CreateNameCreator(EnumFlag prefix) => new NameCreator(prefix);
		protected virtual NameCreator CreateNameCreator(string new_prefix) => new NameCreator(new_prefix);

		public string Create(TypeDef typeDef, string newBaseTypeName) {
			var nameCreator = GetNameCreator(typeDef, newBaseTypeName);
			return existingNames.GetName(typeDef.Name.String, nameCreator);
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
			return nameCreator;
		}
	}

	public class GlobalTypeNameCreator : TypeNameCreator {
		public GlobalTypeNameCreator(ExistingNames existingNames) : base(existingNames) { }
		protected override NameCreator CreateNameCreator(EnumFlag prefix) => base.CreateNameCreator("G" + prefix.GetString());
	}
}
