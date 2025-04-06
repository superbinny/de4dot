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
using dnlib.DotNet;
// Binny 修改
using de4dot.code.deobfuscators;
using HelpUtil;

namespace de4dot.code.renamer.asmmodules {
	public class Module : IResolver {
		IObfuscatedFile obfuscatedFile;
		TypeDefDict types = new TypeDefDict();
		MemberRefFinder memberRefFinder;
		IList<RefToDef<TypeRef, TypeDef>> typeRefsToRename = new List<RefToDef<TypeRef, TypeDef>>();
		IList<RefToDef<MemberRef, MethodDef>> methodRefsToRename = new List<RefToDef<MemberRef, MethodDef>>();
		IList<RefToDef<MemberRef, FieldDef>> fieldRefsToRename = new List<RefToDef<MemberRef, FieldDef>>();
		List<CustomAttributeRef> customAttributeFieldRefs = new List<CustomAttributeRef>();
		List<CustomAttributeRef> customAttributePropertyRefs = new List<CustomAttributeRef>();
		List<MethodDef> allMethods;

		public class CustomAttributeRef {
			public CustomAttribute cattr;
			public int index;
			public IMemberRef reference;
			public CustomAttributeRef(CustomAttribute cattr, int index, IMemberRef reference) {
				this.cattr = cattr;
				this.index = index;
				this.reference = reference;
			}
		}

		public class RefToDef<R, D> where R : ICodedToken where D : ICodedToken {
			public R reference;
			public D definition;
			public RefToDef(R reference, D definition) {
				this.reference = reference;
				this.definition = definition;
			}
		}

		~Module()
		{
            Logger.v($"find FindAllMemberRefs.memberRefFinder:");
            if (DeobfuscatorBase.m_module_log_file != "")
            {
				if (DeobfuscatorBase.mUnmarkWords.Count > 0)
					BaseFunction.SaveToFile(DeobfuscatorBase.getListString(DeobfuscatorBase.mUnmarkWords), DeobfuscatorBase.m_module_log_file);
            }
        }


        public IEnumerable<RefToDef<TypeRef, TypeDef>> TypeRefsToRename => typeRefsToRename;
		public IEnumerable<RefToDef<MemberRef, MethodDef>> MethodRefsToRename => methodRefsToRename;
		public IEnumerable<RefToDef<MemberRef, FieldDef>> FieldRefsToRename => fieldRefsToRename;
		public IEnumerable<CustomAttributeRef> CustomAttributeFieldRefs => customAttributeFieldRefs;
		public IEnumerable<CustomAttributeRef> CustomAttributePropertyRefs => customAttributePropertyRefs;
		public IObfuscatedFile ObfuscatedFile => obfuscatedFile;
		public string Filename => obfuscatedFile.Filename;
		public ModuleDefMD ModuleDefMD => obfuscatedFile.ModuleDefMD;
		public Module(IObfuscatedFile obfuscatedFile) => this.obfuscatedFile = obfuscatedFile;

		public IEnumerable<MTypeDef> GetAllTypes() => types.GetValues();
		public IEnumerable<MethodDef> GetAllMethods() => allMethods;

		public void FindAllMemberRefs(ref int typeIndex) {
			memberRefFinder = new MemberRefFinder();
			memberRefFinder.FindAll(ModuleDefMD);
			allMethods = new List<MethodDef>(memberRefFinder.MethodDefs.Keys);

			var allTypesList = new List<MTypeDef>();
			foreach (var type in memberRefFinder.TypeDefs.Keys) {
				// Binny 修改
				if (!DeobfuscatorBase.CheckValidName(type.Name))
					Logger.v($"find FindAllMemberRefs.memberRefFinder: {type.Name}");
				var typeDef = new MTypeDef(type, this, typeIndex++);
				types.Add(typeDef);
				allTypesList.Add(typeDef);
				typeDef.AddMembers();
			}

			var allTypesCopy = new List<MTypeDef>(allTypesList);
			var typeToIndex = new Dictionary<TypeDef, int>();
			for (int i = 0; i < allTypesList.Count; i++)
				typeToIndex[allTypesList[i].TypeDef] = i;
			foreach (var typeDef in allTypesList) {
					// Binny 修改
					if (!DeobfuscatorBase.CheckValidName(typeDef.TypeDef.Name))
						Logger.v($"find FindAllMemberRefs.allTypesList: {typeDef.TypeDef.Name}");
				if (typeDef.TypeDef.NestedTypes == null)
					continue;
				// Binny 修改
				foreach (var fieldDef in typeDef.AllFields) {
					if (!DeobfuscatorBase.CheckFieldValidName(fieldDef.FieldDef.Name))
						Logger.v($"find FindAllMemberRefs.AllFields: {typeDef.TypeDef.Name}.{fieldDef.FieldDef.Name}");
				}
				// Binny 修改
				foreach (var methodDef in typeDef.AllMethods) {
					if (!DeobfuscatorBase.CheckValidName(methodDef.MethodDef.Name))
						Logger.v($"find FindAllMemberRefs.AllMethods: {typeDef.TypeDef.Name}.{methodDef.MethodDef.Name}");
				}
				// Binny 修改
				foreach (var nestedTypeDef2 in typeDef.TypeDef.NestedTypes) {
					if (!DeobfuscatorBase.CheckValidName(nestedTypeDef2.Name))
						Logger.v($"find FindAllMemberRefs.typeDef.NestedTypes: {nestedTypeDef2.Name}");
					int index = typeToIndex[nestedTypeDef2];
					var nestedTypeDef = allTypesCopy[index];
					allTypesCopy[index] = null;
					if (nestedTypeDef == null)	// Impossible
						throw new ApplicationException("Nested type belongs to two or more types");
					typeDef.Add(nestedTypeDef);
					nestedTypeDef.NestingType = typeDef;
				}
			}
		}
		// Binny 修改
		public void FindAllMemberRefsUseMono(ref int typeIndex) {
			memberRefFinder = new MemberRefFinder();
			memberRefFinder.FindAll(ModuleDefMD);
			allMethods = new List<MethodDef>(memberRefFinder.MethodDefs.Keys);

			var allTypesList = new List<MTypeDef>();
			foreach (var type in memberRefFinder.TypeDefs.Keys) {
				if (!DeobfuscatorBase.CheckMamberValidName(type.Name))
					Logger.v($"find FindAllMemberRefs.memberRefFinder: {type.Name}");
				var typeDef = new MTypeDef(type, this, typeIndex++);
				types.Add(typeDef);
				allTypesList.Add(typeDef);
				typeDef.AddMembers();
			}

			DeobfuscatorBase.set_module_file(Filename);
			var allTypesCopy = new List<MTypeDef>(allTypesList);
			var typeToIndex = new Dictionary<TypeDef, int>();
			for (int i = 0; i < allTypesList.Count; i++)
				typeToIndex[allTypesList[i].TypeDef] = i;
			foreach (var typeDef in allTypesList) {
				if (!DeobfuscatorBase.CheckValidName(typeDef.TypeDef.Name))
					Logger.v($"find FindAllMemberRefs.allTypesList: {typeDef.TypeDef.Name}");
				if (typeDef.TypeDef.NestedTypes == null)
					continue;
				foreach (var fieldDef in typeDef.AllFields) {
					if (!DeobfuscatorBase.CheckFieldValidName(fieldDef.FieldDef.Name))
						Logger.v($"find FindAllMemberRefs.AllFields: {typeDef.TypeDef.Name}.{fieldDef.FieldDef.Name}");
				}

				foreach (var methodDef in typeDef.AllMethods) {
					if (!DeobfuscatorBase.CheckValidName(methodDef.MethodDef.Name))
						Logger.v($"find FindAllMemberRefs.AllMethods: {typeDef.TypeDef.Name}.{methodDef.MethodDef.Name}");
				}

				foreach (var nestedTypeDef2 in typeDef.TypeDef.NestedTypes) {
					if (!DeobfuscatorBase.CheckValidName(nestedTypeDef2.Name))
						Logger.v($"find FindAllMemberRefs.typeDef.NestedTypes: {nestedTypeDef2.Name}");
					int index = typeToIndex[nestedTypeDef2];
					var nestedTypeDef = allTypesCopy[index];
					allTypesCopy[index] = null;
					if (nestedTypeDef == null)  // Impossible
						throw new ApplicationException("Nested type belongs to two or more types");
					typeDef.Add(nestedTypeDef);
					nestedTypeDef.NestingType = typeDef;
				}
			}
		}
		// Binny 修改
		public void ResolveAllRefs(IResolver resolver) {
			foreach (var typeRef in memberRefFinder.TypeRefs.Keys) {
				if (!DeobfuscatorBase.CheckValidName(typeRef.Name))
					Logger.v($"find TypeRefs: {typeRef.Name}");

				if (typeRef.Name.Contains("j__TPar")) {
					throw new ApplicationException("Nested type belongs to two or more types");
				}
				var typeDef = resolver.ResolveType(typeRef);
				if (typeDef != null)
					typeRefsToRename.Add(new RefToDef<TypeRef, TypeDef>(typeRef, typeDef.TypeDef));
			}
			// Binny 修改
			foreach (var memberRef in memberRefFinder.MemberRefs.Keys) {
				if (!DeobfuscatorBase.CheckValidName(memberRef.Name))
					Logger.v($"find MemberRefs: {memberRef.Name}");

				if (memberRef.Name.Contains("j__TPar")) {
					throw new ApplicationException("Nested type belongs to two or more types");
				}
				if (memberRef.IsMethodRef) {
					var methodDef = resolver.ResolveMethod(memberRef);
					if (methodDef != null)
						methodRefsToRename.Add(new RefToDef<MemberRef, MethodDef>(memberRef, methodDef.MethodDef));
				}
				else if (memberRef.IsFieldRef) {
					var fieldDef = resolver.ResolveField(memberRef);
					if (fieldDef != null)
						fieldRefsToRename.Add(new RefToDef<MemberRef, FieldDef>(memberRef, fieldDef.FieldDef));
				}
			}
			// Binny 修改
			foreach (var cattr in memberRefFinder.CustomAttributes.Keys) {
				if (!DeobfuscatorBase.CheckValidName(cattr.AttributeType.Name))
					Logger.v($"find CustomAttributes: {cattr.AttributeType.Name}");

				var typeDef = resolver.ResolveType(cattr.AttributeType);
				if (typeDef == null)
					continue;
				if (cattr.NamedArguments == null)
					continue;
				
				for (int i = 0; i < cattr.NamedArguments.Count; i++) {
					var namedArg = cattr.NamedArguments[i];
					// Binny 修改
					if (!DeobfuscatorBase.CheckValidName(namedArg.Name))
						Logger.v($"find CustomAttributes.NamedArguments: {namedArg.Name}");
					if (namedArg.IsField) {
						var fieldDef = FindField(typeDef, namedArg.Name, namedArg.Type);
						if (fieldDef == null) {
							Logger.w("Could not find field {0} in attribute {1} ({2:X8})",
									Utils.ToCsharpString(namedArg.Name),
									Utils.ToCsharpString(typeDef.TypeDef.Name),
									typeDef.TypeDef.MDToken.ToInt32());
							continue;
						}

						customAttributeFieldRefs.Add(new CustomAttributeRef(cattr, i, fieldDef.FieldDef));
					}
					else {
						var propDef = FindProperty(typeDef, namedArg.Name, namedArg.Type);
						if (propDef == null) {
							Logger.w("Could not find property {0} in attribute {1} ({2:X8})",
									Utils.ToCsharpString(namedArg.Name),
									Utils.ToCsharpString(typeDef.TypeDef.Name),
									typeDef.TypeDef.MDToken.ToInt32());
							continue;
						}

						customAttributePropertyRefs.Add(new CustomAttributeRef(cattr, i, propDef.PropertyDef));
					}
				}
			}
		}

		static MFieldDef FindField(MTypeDef typeDef, UTF8String name, TypeSig fieldType) {
			while (typeDef != null) {
				foreach (var fieldDef in typeDef.AllFields) {
					// Binny 修改
					if (!DeobfuscatorBase.CheckValidName(fieldDef.FieldDef.Name))
						Logger.v($"find FindField: {fieldDef.FieldDef.Name}");
					if (fieldDef.FieldDef.Name != name)
						continue;
					if (new SigComparer().Equals(fieldDef.FieldDef.FieldSig.GetFieldType(), fieldType))
						return fieldDef;
				}

				if (typeDef.baseType == null)
					break;
				typeDef = typeDef.baseType.typeDef;
			}
			return null;
		}

		static MPropertyDef FindProperty(MTypeDef typeDef, UTF8String name, TypeSig propType) {
			while (typeDef != null) {
				foreach (var propDef in typeDef.AllProperties) {
					// Binny 修改
					if (!DeobfuscatorBase.CheckValidName(propDef.PropertyDef.Name))
						Logger.v($"find FindProperty: {propDef.PropertyDef.Name}");
					if (propDef.PropertyDef.Name != name)
						continue;
					if (new SigComparer().Equals(propDef.PropertyDef.PropertySig.GetRetType(), propType))
						return propDef;
				}

				if (typeDef.baseType == null)
					break;
				typeDef = typeDef.baseType.typeDef;
			}
			return null;
		}

		public void OnTypesRenamed() {
			var newTypes = new TypeDefDict();
			foreach (var typeDef in types.GetValues()) {
				// Binny 修改
				if (!DeobfuscatorBase.CheckValidName(typeDef.TypeDef.Name))
					Logger.v($"find OnTypesRenamed: {typeDef.TypeDef.Name}");
				typeDef.OnTypesRenamed();
				newTypes.Add(typeDef);
			}
			types = newTypes;

			ModuleDefMD.ResetTypeDefFindCache();
		}

		static ITypeDefOrRef GetNonGenericTypeRef(ITypeDefOrRef typeRef) {
			var ts = typeRef as TypeSpec;
			if (ts == null)
				return typeRef;
			var gis = ts.TryGetGenericInstSig();
			if (gis == null || gis.GenericType == null)
				return typeRef;
			return gis.GenericType.TypeDefOrRef;
		}

		public MTypeDef ResolveType(ITypeDefOrRef typeRef) => types.Find(GetNonGenericTypeRef(typeRef));

		public MMethodDef ResolveMethod(IMethodDefOrRef methodRef) {
			var typeDef = types.Find(GetNonGenericTypeRef(methodRef.DeclaringType));
			if (typeDef == null)
				return null;
			return typeDef.FindMethod(methodRef);
		}

		public MFieldDef ResolveField(MemberRef fieldRef) {
			var typeDef = types.Find(GetNonGenericTypeRef(fieldRef.DeclaringType));
			if (typeDef == null)
				return null;
			return typeDef.FindField(fieldRef);
		}
	}
}
