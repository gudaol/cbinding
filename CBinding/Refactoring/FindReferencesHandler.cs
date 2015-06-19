﻿using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using ClangSharp;
using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.Gui;
using System.Runtime.InteropServices;
using System.Collections;

namespace CBinding.Refactoring
{	//Based on code from CSharpBinding
	public class FindReferencesHandler
	{
		CProject project;
		CXCursor cursorReferenced;
		string USRReferenced;
		string spelling;

		public FindReferencesHandler (CProject proj, Document doc) {
			project = proj;
			cursorReferenced = project.cLangManager.getCursorReferenced(
				project.cLangManager.getCursor (
					doc.FileName,
					doc.Editor.CaretLocation
				)
			);
			USRReferenced = project.cLangManager.getCursorUSRString (cursorReferenced);
			spelling = project.cLangManager.getCursorSpelling(cursorReferenced);
		}

		private List<Reference> references = new List<Reference>();
		public CXChildVisitResult Visit(CXCursor cursor, CXCursor parent, IntPtr data){
			CXCursor referenced = project.cLangManager.getCursorReferenced (cursor);
			string USR = project.cLangManager.getCursorUSRString (referenced);
			if (USRReferenced.Equals (USR)) {
				CXSourceRange range = clang.Cursor_getSpellingNameRange (cursor, 0, 0);
				Reference reference = new Reference (project, cursor, range);
				var doc = IdeApp.Workbench.GetDocument (reference.FileName);
				if (doc != null) {
					if (!references.Contains (reference)
					//this check is needed because explicit namespace qualifiers, eg: "std" from std::toupper
					//are also found when finding eg:toupper references, but has the same cursorkind as eg:"toupper"
						&& !doc.Editor.GetCharAt (Convert.ToInt32 (reference.End.Offset + 1)).Equals (':')
					    && !doc.Editor.GetCharAt (Convert.ToInt32 (reference.End.Offset + 2)).Equals (':')) {
						references.Add (reference);
					}			
				}
			}
			return CXChildVisitResult.CXChildVisit_Recurse;
		}

		public void FindRefs (CProject project, CXCursor cursor)
		{
			var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
			ThreadPool.QueueUserWorkItem ( o => {
				try {
					project.cLangManager.findReferences(this);
					foreach (var reference in references) {
						var sr = new SearchResult (
							new FileProvider (reference.FileName),
							Convert.ToInt32 (reference.Offset),
							Convert.ToInt32 (reference.Length)
						);
						monitor.ReportResult (sr);
					}
				} catch (Exception ex) {
					if (monitor != null)
						monitor.ReportError ("Error finding references", ex);
					else
						LoggingService.LogError ("Error finding references", ex);
				} finally {
					if (monitor != null)
						monitor.Dispose ();
				}
			});
		}

		public void Update (CommandInfo info)
		{
			if (clang.Cursor_isNull (cursorReferenced) == 0) {
				info.Enabled = info.Visible = IsReferenceOrDeclaration (cursorReferenced);
				info.Bypass = !info.Visible;
			}
		}

		public void Run ()
		{
			FindRefs (project, cursorReferenced);
		}

		private bool IsReferenceOrDeclaration(CXCursor cursor) {
			switch (cursor.kind) {
			case CXCursorKind.CXCursor_VarDecl:
			case CXCursorKind.CXCursor_VariableRef:
			case CXCursorKind.CXCursor_ClassDecl:
			case CXCursorKind.CXCursor_ClassTemplate:
			case CXCursorKind.CXCursor_ClassTemplatePartialSpecialization:
			case CXCursorKind.CXCursor_FunctionDecl:
			case CXCursorKind.CXCursor_FunctionTemplate:
			case CXCursorKind.CXCursor_FieldDecl:
			case CXCursorKind.CXCursor_MemberRef:
			case CXCursorKind.CXCursor_CXXMethod:
			case CXCursorKind.CXCursor_Namespace:
			case CXCursorKind.CXCursor_NamespaceRef:
			case CXCursorKind.CXCursor_NamespaceAlias:
			case CXCursorKind.CXCursor_EnumDecl:
			case CXCursorKind.CXCursor_EnumConstantDecl:
			case CXCursorKind.CXCursor_StructDecl:
			case CXCursorKind.CXCursor_TypedefDecl:
			case CXCursorKind.CXCursor_TypeRef:
			case CXCursorKind.CXCursor_DeclRefExpr:
			case CXCursorKind.CXCursor_ParmDecl:
			case CXCursorKind.CXCursor_TemplateTypeParameter:
			case CXCursorKind.CXCursor_TemplateTemplateParameter:
			case CXCursorKind.CXCursor_NonTypeTemplateParameter:
				return true;
			}
			return false;

		}
	}
}
