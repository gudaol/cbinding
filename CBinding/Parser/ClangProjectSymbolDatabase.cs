using System;
using ClangSharp;
using System.Collections.Generic;
using GLib;
using System.Runtime.InteropServices;
using System.Linq;

namespace CBinding.Parser
{	
	/// <summary>
	/// Symbol database belonging to a project
	/// </summary>
	public class ClangProjectSymbolDatabase {
		protected CProject project;
		protected Dictionary<string, ClangFileSymbolDatabase> db;

		public ClangProjectSymbolDatabase(CProject proj) {
			project = proj;
			db = new Dictionary<string, ClangFileSymbolDatabase> ();
		}

		/// <summary>
		/// Adds the cursor to the database associated with filename
		/// </summary>
		/// <param name="file">The filename of the file.</param>
		/// <param name="cursor">Cursor.</param>
		public void AddToDatabase (string file, CXCursor cursor)
		{
			try {
				if (!db.ContainsKey (file))
					db.Add (file, new ClangFileSymbolDatabase(project, file));
				db [file].AddToDatabase (cursor);
			} catch (ArgumentException) {
			}
		}

		/// <summary>
		/// Reset/empty the database associated with the filename
		/// </summary>
		/// <param name="file">Filename</param>
		public void Reset(string file){
			if (db.ContainsKey (file))
				db [file] = new ClangFileSymbolDatabase (project, file);
			else
				db.Add (file, new ClangFileSymbolDatabase (project, file));
		}

		public Dictionary<CXCursor, Namespace> Namespaces {
			get {
				Dictionary<CXCursor, Namespace> ret = new Dictionary<CXCursor, Namespace>();
				foreach (var iter in db)
					iter.Value.Namespaces.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;
			}
		}

		public Dictionary<CXCursor, Function> Functions {
			get {
				Dictionary<CXCursor, Function> ret = new Dictionary<CXCursor, Function>();
				foreach (var iter in db)
					iter.Value.Functions.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);				
				return ret;
			}
		}

		public Dictionary<CXCursor, MemberFunction> MemberFunctions {
			get {
				Dictionary<CXCursor, MemberFunction> ret = new Dictionary<CXCursor, MemberFunction>();
				foreach (var iter in db)
					iter.Value.MemberFunctions.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;			
			}
		}

		public Dictionary<CXCursor, FunctionTemplate> FunctionTemplates {
			get {
				Dictionary<CXCursor, FunctionTemplate> ret = new Dictionary<CXCursor, FunctionTemplate>();
				foreach (var iter in db)
					iter.Value.FunctionTemplates.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;
			}
		}

		public Dictionary<CXCursor, Class> Classes {
			get {
				Dictionary<CXCursor, Class> ret = new Dictionary<CXCursor, Class>();
				foreach (var iter in db)
					iter.Value.Classes.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;
			}
		}

		public Dictionary<CXCursor, ClassTemplate> ClassTemplates {
			get {
				Dictionary<CXCursor, ClassTemplate> ret = new Dictionary<CXCursor, ClassTemplate>();
				foreach (var iter in db)
					iter.Value.ClassTemplates.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;
			}
		}

		public Dictionary<CXCursor, ClassTemplatePartial> ClassTemplatesPartials {
			get {
				Dictionary<CXCursor, ClassTemplatePartial> ret = new Dictionary<CXCursor, ClassTemplatePartial>();
				foreach (var iter in db)
					iter.Value.ClassTemplatePartials.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;
			}
		}

		public Dictionary<CXCursor, Struct> Structs {
			get {
				Dictionary<CXCursor, Struct> ret = new Dictionary<CXCursor, Struct>();
				foreach (var iter in db)
					iter.Value.Structs.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;
			}
		}

		public Dictionary<CXCursor, Enumeration> Enumerations {
			get {
				Dictionary<CXCursor, Enumeration> ret = new Dictionary<CXCursor, Enumeration>();
				foreach (var iter in db)
					iter.Value.Enumerations.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;
			}
		}

		public Dictionary<CXCursor, Enumerator> Enumerators {
			get {
				Dictionary<CXCursor, Enumerator> ret = new Dictionary<CXCursor, Enumerator>();
				foreach (var iter in db)
					iter.Value.Enumerators.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;
			}
		}

		public Dictionary<CXCursor, Variable> Variables {
			get {
				Dictionary<CXCursor, Variable> ret = new Dictionary<CXCursor, Variable>();
				foreach (var iter in db)
					iter.Value.Variables.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;
			}
		}

		public Dictionary<CXCursor, Macro> Macros {
			get {
				Dictionary<CXCursor, Macro> ret = new Dictionary<CXCursor, Macro>();
				foreach (var iter in db)
					iter.Value.Macros.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;
			}
		}

		public Dictionary<CXCursor, Union> Unions {
			get {
				Dictionary<CXCursor, Union> ret = new Dictionary<CXCursor, Union>();
				foreach (var iter in db)
					iter.Value.Unions.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;
			}
		}

		public Dictionary<CXCursor, Typedef> Typedefs {
			get {
				Dictionary<CXCursor, Typedef> ret = new Dictionary<CXCursor, Typedef>();
				foreach (var iter in db)
					iter.Value.Typedefs.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;
			}
		}

		public Dictionary<CXCursor, Symbol> Others {
			get {
				Dictionary<CXCursor, Symbol> ret = new Dictionary<CXCursor, Symbol>();
				foreach (var iter in db)
					iter.Value.Others.ToList().ForEach(
						x => {
							ret.Add(x.Key, x.Value);
						}
					);
				return ret;
			}
		}

		public CXCursor getDefinition (CXCursor cursor) {
			try {
				string USR = project.cLangManager.getCursorUSRString (cursor);
				foreach (var T in Functions){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in MemberFunctions){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in Classes){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in ClassTemplates){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in ClassTemplatesPartials){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in Structs){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in FunctionTemplates){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in Enumerations){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in Enumerators){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in Variables){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in Typedefs){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in Unions){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in Namespaces){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in Macros){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
				foreach (var T in Others){
					if(T.Value.USR.Equals (USR) && T.Value.IsDefinition)
						return T.Key;
				}
			} catch (Exception) {
				return clang.getNullCursor ();
			}
			return clang.getNullCursor ();
		}

		public CXCursor getDeclaration (CXCursor cursor) {
			try {
				string USR = project.cLangManager.getCursorUSRString (cursor);
				foreach (var T in Functions){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in MemberFunctions){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in Classes){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in ClassTemplates){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in ClassTemplatesPartials){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in Structs){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in FunctionTemplates){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in Enumerations){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in Enumerators){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in Variables){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in Typedefs){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in Unions){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in Namespaces){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in Macros){
					if(T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
				foreach (var T in Others) {
					if (T.Value.USR.Equals (USR) && T.Value.IsDeclaration)
						return T.Key;
				}
			} catch (Exception) {
				return clang.getNullCursor ();
			}
			return clang.getNullCursor ();
		}
	}	
}