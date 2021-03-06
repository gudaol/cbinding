//
// ProjectNodeBuilderExtension.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using CBinding;
using CBinding.Parser;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using ClangSharp;

namespace CBinding.Navigation
{
	public class ProjectNodeBuilderExtension : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(CProject).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ProjectNodeBuilderExtensionHandler); }
		}

		//this is unneeded?
		/*
		protected override void Initialize ()
		{
			//TagDatabaseManager.Instance.FileUpdated += OnFinishedBuildingTree;
		}
		
		public override void Dispose ()
		{
			//TagDatabaseManager.Instance.FileUpdated -= OnFinishedBuildingTree;
		}
		
		public static void CreatePadTree (object o)
		{
			CProject p = o as CProject;
			if (o == null) return;

			//this happens in CDocumentParser.Parse
			/*try {
				foreach (ProjectFile f in p.Files) {
					if (f.BuildAction == BuildAction.Compile)
						TagDatabaseManager.Instance.UpdateFileTags (p, f.Name);
				}
			} catch (IOException) {
				return;
			}
		}*/
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{			
			CProject p = (CProject)dataObject;
			
			if (p == null) return;
			
			bool nestedNamespaces = builder.Options["NestedNamespaces"];
			
			ClangProjectSymbolDatabase info = p.DB;

			// Namespaces
			foreach (Namespace n in info.Namespaces.Values) {
				/*CXSourceLocation loc = clang.getCursorLocation (n.Represented);
				CXFile file;
				uint line, column, offset;
				clang.getExpansionLocation (loc, out file, out line, out column, out offset);
				var fileName = clang.getFileName (file).ToString ();
				if(p.IsFileInProject (fileName))*/
				if(n.Ours){
					if (nestedNamespaces) {
						if (n.Parent == null) {
							//if nested is enabled only add if top (Parent is null if the parentCursor is a translation unit)
							builder.AddChild (n);
						}
					} else {
						//else add every namespace
						builder.AddChild (n);
					}
				}			
			}
			
			// Globals
			builder.AddChild (info.GlobalDefinitions);
			
			// Macro Definitions
			builder.AddChild (info.MacroDefinitions);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		void OnFinishedBuildingTree (ClassPadEventArgs e)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (e.Project);
			if (null != builder)
				builder.UpdateChildren ();
		}
	}
	
	public class ProjectNodeBuilderExtensionHandler : NodeCommandHandler
	{
//		public override void ActivateItem ()
//		{
//			CProject p = CurrentNode.DataItem as CProject;
//			
//			if (p == null) return;
//			
//			Thread builderThread = new Thread (new ParameterizedThreadStart (ProjectNodeBuilderExtension.CreatePadTree));
//			builderThread.Name = "PadBuilder";
//			builderThread.IsBackground = true;
//			builderThread.Start (p);
//		}
		
		[CommandHandler (CProjectCommands.UpdateClassPad)]
		public void UpdateClassPad ()
		{
		}

		public override void RefreshItem ()
		{
			//TODO need a nice method to sleep while parsing is in process to avoid crashes!
			base.RefreshItem ();
		}
	}
}
