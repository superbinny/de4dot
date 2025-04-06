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
using de4dot.code.renamer.asmmodules;
// Binny 添加
using de4dot.code.deobfuscators;
using dnlib.DotNet;


namespace de4dot.code.renamer {
	public class MemberInfo {
		protected Ref memberRef;
		public string oldFullName;
		public bool renamed;
		// Binny 修改
		private string _suggestedName;
		// Binny 添加
		private string _newName;
		private string _oldName;
		TypeRenamerState state = new TypeRenamerState();
		public string suggestedName {
			set {
				_suggestedName = value;
			}
			get {
				return _suggestedName;
			}
		}
		// Binny 修改
		public string newName {
			set {
				if (value.Contains("SystemDTCollectionsDTIEnumerableDTGetEnumerator")) {
					_newName = _newName;
				}
				_newName = value;
			}
			get {
				return _newName;
			}
		}
		// Binny 修改
		private string rename_field(FieldDef typeDef, string newBaseTypeName) {
			return state.internalTypeNameCreator.Create(typeDef, newBaseTypeName);
		}
		// Binny 修改
		public string newFieldName {
			set {
				if (!DeobfuscatorBase.CheckFieldValidName(value)) {
					newName = DeobfuscatorBase.ReplaceValidName(value);
					if (newName.Length <= 3) {
						FieldDef def = (FieldDef)this.memberRef.memberRef;
						string new_name = rename_field(def, this.memberRef.memberRef.Name.String);
						new_name += "_" + this.memberRef.memberRef.Name.String;
						Rename(new_name);
					}
				}
				else {
					newName = value;
				}
			}
			get {
				return newName;
			}
		}
		// Binny 修改
		public string newTypeName {
			set {
				if (!DeobfuscatorBase.CheckMamberValidName(value)) {
					// 允许小数点
					newName = DeobfuscatorBase.ReplaceValidName(value, true);
				}
				else {
					newName = value;
				}
			}
			get {
				return newName;
			}
		}
		// Binny 修改
		public string oldName {
			set {
				_oldName = value;
			}
			get {
				return _oldName;
			}
		}
		// Binny 添加
		public MemberInfo(Ref memberRef, int type_index) {
			this.memberRef = memberRef;
			oldFullName = memberRef.memberRef.FullName;
			oldName = memberRef.memberRef.Name.String;
			// Binny 添加
			if (type_index == 1)//Type
				newTypeName = memberRef.memberRef.Name.String;
			else if (type_index == 2)//GenericParamInfo
				newTypeName = memberRef.memberRef.Name.String;
			else if (type_index == 3)//PropertyInfo
				newTypeName = memberRef.memberRef.Name.String;
			else if (type_index == 4)//MemberInfo
				newTypeName = memberRef.memberRef.Name.String;
			else if (type_index == 5)//FieldInfo
				newFieldName = memberRef.memberRef.Name.String;
			else if (type_index == 6)//MethodInfo
				newTypeName = memberRef.memberRef.Name.String;
		}

		public void Rename(string newTypeName) {
			renamed = true;
			newName = newTypeName;
		}

		public bool GotNewName() => oldName != newName;
		public override string ToString() => $"O:{oldFullName} -- N:{newName}";
	}

	public class GenericParamInfo : MemberInfo {
		public GenericParamInfo(MGenericParamDef genericParamDef) : base(genericParamDef, 2) { }
	}

	public class PropertyInfo : MemberInfo {
		// Binny 修改
		public PropertyInfo(MPropertyDef propertyDef) : base(propertyDef, 3) { }
	}

	public class EventInfo : MemberInfo {
		// Binny 修改
		public EventInfo(MEventDef eventDef) : base(eventDef, 4) { }
	}

	public class FieldInfo : MemberInfo {
		// Binny 修改
		public FieldInfo(MFieldDef fieldDef) : base(fieldDef, 5) { }
	}

	public class MethodInfo : MemberInfo {
		public MMethodDef MethodDef => (MMethodDef)memberRef;
		// Binny 修改
		public MethodInfo(MMethodDef methodDef) : base(methodDef, 6) { }
	}

	public class ParamInfo {
		public string oldName;
		public string newName;

		public ParamInfo(MParamDef paramDef) {
			oldName = paramDef.ParameterDef.Name;
			newName = paramDef.ParameterDef.Name;
		}

		public bool GotNewName() => oldName != newName;
	}

	public class MemberInfos {
		Dictionary<MTypeDef, TypeInfo> allTypeInfos = new Dictionary<MTypeDef, TypeInfo>();
		Dictionary<MPropertyDef, PropertyInfo> allPropertyInfos = new Dictionary<MPropertyDef, PropertyInfo>();
		Dictionary<MEventDef, EventInfo> allEventInfos = new Dictionary<MEventDef, EventInfo>();
		Dictionary<MFieldDef, FieldInfo> allFieldInfos = new Dictionary<MFieldDef, FieldInfo>();
		Dictionary<MMethodDef, MethodInfo> allMethodInfos = new Dictionary<MMethodDef, MethodInfo>();
		Dictionary<MGenericParamDef, GenericParamInfo> allGenericParamInfos = new Dictionary<MGenericParamDef, GenericParamInfo>();
		Dictionary<MParamDef, ParamInfo> allParamInfos = new Dictionary<MParamDef, ParamInfo>();
		DerivedFrom checkWinFormsClass;

		static string[] WINFORMS_CLASSES = new string[] {
#region Win Forms class names
			"System.Windows.Forms.Control",
			"System.Windows.Forms.AxHost",
			"System.Windows.Forms.ButtonBase",
			"System.Windows.Forms.Button",
			"System.Windows.Forms.CheckBox",
			"System.Windows.Forms.RadioButton",
			"System.Windows.Forms.DataGrid",
			"System.Windows.Forms.DataGridView",
			"System.Windows.Forms.DataVisualization.Charting.Chart",
			"System.Windows.Forms.DateTimePicker",
			"System.Windows.Forms.GroupBox",
			"System.Windows.Forms.Integration.ElementHost",
			"System.Windows.Forms.Label",
			"System.Windows.Forms.LinkLabel",
			"System.Windows.Forms.ListControl",
			"System.Windows.Forms.ComboBox",
			"Microsoft.VisualBasic.Compatibility.VB6.DriveListBox",
			"System.Windows.Forms.DataGridViewComboBoxEditingControl",
			"System.Windows.Forms.ListBox",
			"Microsoft.VisualBasic.Compatibility.VB6.DirListBox",
			"Microsoft.VisualBasic.Compatibility.VB6.FileListBox",
			"System.Windows.Forms.CheckedListBox",
			"System.Windows.Forms.ListView",
			"System.Windows.Forms.MdiClient",
			"System.Windows.Forms.MonthCalendar",
			"System.Windows.Forms.PictureBox",
			"System.Windows.Forms.PrintPreviewControl",
			"System.Windows.Forms.ProgressBar",
			"System.Windows.Forms.ScrollableControl",
			"System.Windows.Forms.ContainerControl",
			"System.Windows.Forms.Form",
			"System.ComponentModel.Design.CollectionEditor.CollectionForm",
			"System.Messaging.Design.QueuePathDialog",
			"System.ServiceProcess.Design.ServiceInstallerDialog",
			"System.Web.UI.Design.WebControls.CalendarAutoFormatDialog",
			"System.Web.UI.Design.WebControls.RegexEditorDialog",
			"System.Windows.Forms.Design.ComponentEditorForm",
			"System.Windows.Forms.PrintPreviewDialog",
			"System.Windows.Forms.ThreadExceptionDialog",
			"System.Workflow.Activities.Rules.Design.RuleConditionDialog",
			"System.Workflow.Activities.Rules.Design.RuleSetDialog",
			"System.Workflow.ComponentModel.Design.ThemeConfigurationDialog",
			"System.Workflow.ComponentModel.Design.TypeBrowserDialog",
			"System.Workflow.ComponentModel.Design.WorkflowPageSetupDialog",
			"System.Windows.Forms.PropertyGrid",
			"System.Windows.Forms.SplitContainer",
			"System.Windows.Forms.ToolStripContainer",
			"System.Windows.Forms.ToolStripPanel",
			"System.Windows.Forms.UpDownBase",
			"System.Windows.Forms.DomainUpDown",
			"System.Windows.Forms.NumericUpDown",
			"System.Windows.Forms.UserControl",
			"Microsoft.VisualBasic.Compatibility.VB6.ADODC",
			"System.Web.UI.Design.WebControls.ParameterEditorUserControl",
			"System.Workflow.ComponentModel.Design.WorkflowOutline",
			"System.Workflow.ComponentModel.Design.WorkflowView",
			"System.Windows.Forms.Design.ComponentTray",
			"System.Windows.Forms.Panel",
			"System.Windows.Forms.Design.ComponentEditorPage",
			"System.Windows.Forms.FlowLayoutPanel",
			"System.Windows.Forms.SplitterPanel",
			"System.Windows.Forms.TableLayoutPanel",
			"System.ComponentModel.Design.ByteViewer",
			"System.Windows.Forms.TabPage",
			"System.Windows.Forms.ToolStripContentPanel",
			"System.Windows.Forms.ToolStrip",
			"System.Windows.Forms.BindingNavigator",
			"System.Windows.Forms.MenuStrip",
			"System.Windows.Forms.StatusStrip",
			"System.Windows.Forms.ToolStripDropDown",
			"System.Windows.Forms.ToolStripDropDownMenu",
			"System.Windows.Forms.ContextMenuStrip",
			"System.Windows.Forms.ToolStripOverflow",
			"System.Windows.Forms.ScrollBar",
			"System.Windows.Forms.HScrollBar",
			"System.Windows.Forms.VScrollBar",
			"System.Windows.Forms.Splitter",
			"System.Windows.Forms.StatusBar",
			"System.Windows.Forms.TabControl",
			"System.Windows.Forms.TextBoxBase",
			"System.Windows.Forms.MaskedTextBox",
			"System.Windows.Forms.RichTextBox",
			"System.Windows.Forms.TextBox",
			"System.Windows.Forms.DataGridTextBox",
			"System.Windows.Forms.DataGridViewTextBoxEditingControl",
			"System.Windows.Forms.ToolBar",
			"System.Windows.Forms.TrackBar",
			"System.Windows.Forms.TreeView",
			"System.ComponentModel.Design.ObjectSelectorEditor.Selector",
			"System.Windows.Forms.WebBrowserBase",
			"System.Windows.Forms.WebBrowser",
#endregion
		};

		public MemberInfos() => checkWinFormsClass = new DerivedFrom(WINFORMS_CLASSES);
		public bool IsWinFormsClass(MTypeDef type) => checkWinFormsClass.Check(type);
		public TypeInfo Type(MTypeDef t) => allTypeInfos[t];
		public bool TryGetType(MTypeDef t, out TypeInfo info) => allTypeInfos.TryGetValue(t, out info);
		public bool TryGetEvent(MEventDef e, out EventInfo info) => allEventInfos.TryGetValue(e, out info);
		public bool TryGetProperty(MPropertyDef p, out PropertyInfo info) => allPropertyInfos.TryGetValue(p, out info);
		// Binny 添加
		public PropertyInfo Property(MPropertyDef prop) {
			var oldFullName = allPropertyInfos[prop].suggestedName;
			if (oldFullName == null) {
				oldFullName = allPropertyInfos[prop].newName;
			}
			return allPropertyInfos[prop];
		}
		public EventInfo Event(MEventDef evt) => allEventInfos[evt];
		public FieldInfo Field(MFieldDef field) => allFieldInfos[field];
		public MethodInfo Method(MMethodDef method) => allMethodInfos[method];
		public GenericParamInfo GenericParam(MGenericParamDef gparam) => allGenericParamInfos[gparam];
		public ParamInfo Param(MParamDef param) => allParamInfos[param];
		public void Add(MPropertyDef prop) => allPropertyInfos[prop] = new PropertyInfo(prop);
		public void Add(MEventDef evt) => allEventInfos[evt] = new EventInfo(evt);

		public void Initialize(Modules modules) {
			foreach (var type in modules.AllTypes) {
				allTypeInfos[type] = new TypeInfo(type, this);

				foreach (var gp in type.GenericParams)
					allGenericParamInfos[gp] = new GenericParamInfo(gp);

				foreach (var field in type.AllFields)
					allFieldInfos[field] = new FieldInfo(field);

				foreach (var evt in type.AllEvents)
					Add(evt);

				foreach (var prop in type.AllProperties)
					Add(prop);

				foreach (var method in type.AllMethods) {
					allMethodInfos[method] = new MethodInfo(method);
					foreach (var gp in method.GenericParams)
						allGenericParamInfos[gp] = new GenericParamInfo(gp);
					foreach (var param in method.AllParamDefs)
						allParamInfos[param] = new ParamInfo(param);
				}
			}
		}
	}
}
