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
using System.IO;
using System.Xml.Linq;
using dnlib.DotNet;
using HelpUtil;

namespace de4dot.code {
	public class Logger : ILogger {
		public readonly static Logger Instance = new Logger();

		int indentLevel = 0;
		readonly int indentSize = 0;
		LoggerEvent maxLoggerEvent = LoggerEvent.Info;
		string indentString = "";
		Dictionary<string, bool> ignoredMessages = new(StringComparer.Ordinal);
		int numIgnoredMessages;
		bool canIgnoreMessages;
		NameManager nameManager = new NameManager();
		BaseFunction baseFunc = new BaseFunction();

		public int IndentLevel {
			get => indentLevel;
			set {
				if (indentLevel == value)
					return;
				indentLevel = value;
				InitIndentString();
			}
		}

		bool is_writeToFile = false;
		string logFile = "log.txt";

		public LoggerEvent MaxLoggerEvent {
			get => maxLoggerEvent;
			set => maxLoggerEvent = value;
		}

		public void Close() {
            if (streamWriter != null) {
                streamWriter.Close();
            }
			this.baseFunc.EndWriteJson();
		}

		public bool CanIgnoreMessages {
			get => canIgnoreMessages;
			set => canIgnoreMessages = value;
		}

		public int NumIgnoredMessages => numIgnoredMessages;

		public string LogFile {
			get => logFile;
			set {
				logFile = value;
				if (streamWriter != null) {
					streamWriter.Close();
				}
				nameManager.BaseFunc = this.baseFunc;
				string baseLogFile = BaseFunction.GetBaseFileName(logFile);
				string jsonFile = BaseFunction.GetNewFileName(logFile, baseLogFile + ".json");
				File.Delete(jsonFile);
				this.nameManager.BaseFunc.BeginWriteJson(jsonFile);

				streamWriter = new StreamWriter(logFile) { AutoFlush = true };
				IsWriteToFile = true;
			}
		}

		public bool IsWriteToFile { get => is_writeToFile; set => is_writeToFile = value; }

		public StreamWriter streamWriter;

		public Logger() : this(2, true) { }

		public Logger(int indentSize, bool canIgnoreMessages) {
			this.indentSize = indentSize;
			this.canIgnoreMessages = canIgnoreMessages;
		}

		public Logger(int indentSize, bool canIgnoreMessages, string logFile) {
			this.indentSize = indentSize;
			this.canIgnoreMessages = canIgnoreMessages;
			nameManager.BaseFunc = this.baseFunc;
			LogFile = logFile;
			IsWriteToFile = true;
		}

		void InitIndentString() {
			if (indentLevel < 0)
				indentLevel = 0;
			indentString = new string(' ', indentLevel * indentSize);
		}

		public void Indent() {
			indentLevel++;
			InitIndentString();
		}

		public void DeIndent() {
			indentLevel--;
			InitIndentString();
		}

		public void Rename(object sender, string reason, string old_name, string new_name, int level = 0) {
			nameManager.WriteTypeChange(reason, old_name, new_name, level: level);
		}
		public void Log(object sender, LoggerEvent loggerEvent, string format, params object[] args) => Log(true, sender, loggerEvent, format, args);
		public void LogErrorDontIgnore(string format, params object[] args) => Log(false, null, LoggerEvent.Error, format, args);

		public void Log(bool canIgnore, object sender, LoggerEvent loggerEvent, string format, params object[] args) {
			if (IgnoresEvent(loggerEvent))
				return;
			if (canIgnore && IgnoreMessage(loggerEvent, format, args))
				return;

			switch (loggerEvent) {
			case LoggerEvent.Error:
				foreach (var l in string.Format(format, args).Split('\n'))
					LogMessage(string.Empty, $"ERROR: {l}");
				break;

			case LoggerEvent.Warning:
				foreach (var l in string.Format(format, args).Split('\n'))
					LogMessage(string.Empty, $"WARNING: {l}");
				break;

			default:
				var indent = loggerEvent <= LoggerEvent.Warning ? "" : indentString;
				LogMessage(indent, format, args);
				break;
			}
		}

		bool IgnoreMessage(LoggerEvent loggerEvent, string format, object[] args) {
			if (loggerEvent != LoggerEvent.Error && loggerEvent != LoggerEvent.Warning)
				return false;
			if (!canIgnoreMessages)
				return false;
			if (ignoredMessages.ContainsKey(format)) {
				numIgnoredMessages++;
				return true;
			}
			ignoredMessages[format] = true;
			return false;
		}

		void WriteMessage(string indent, string format, params object[] args) {
			string testFormat = String.Format(format, args);
			foreach (char c in testFormat) {
				if (!BaseFunction.IsPrintable(c)) {
					return;
				}
			}
			if (args == null || args.Length == 0)
				streamWriter.WriteLine("{0}{1}", indent, format);
			else
				streamWriter.WriteLine(indent + format, args);
		}

		void LogMessage(string indent, string format, params object[] args) {
			if (IsWriteToFile) {
				WriteMessage(indent, format, args);
			}
			if (args == null || args.Length == 0)
				Console.WriteLine("{0}{1}", indent, format);
			else
				Console.WriteLine(indent + format, args);
		}

		public bool IgnoresEvent(LoggerEvent loggerEvent) => loggerEvent > maxLoggerEvent;

		public static void Log(LoggerEvent loggerEvent, string format, params object[] args) => Instance.Log(null, loggerEvent, format, args);
		public static void e(string format, params object[] args) => Instance.Log(null, LoggerEvent.Error, format, args);
		public static void w(string format, params object[] args) => Instance.Log(null, LoggerEvent.Warning, format, args);
		public static void n(string format, params object[] args) => Instance.Log(null, LoggerEvent.Info, format, args);
		public static void v(string format, params object[] args) => Instance.Log(null, LoggerEvent.Verbose, format, args);
		public static void vv(string format, params object[] args) => Instance.Log(null, LoggerEvent.VeryVerbose, format, args);
		public static void r(string reason, string old_name, string new_name,int level=0) => Instance.Rename(null, reason, old_name,new_name, level: level);
	}
}
