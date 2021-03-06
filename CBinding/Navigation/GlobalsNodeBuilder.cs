//
// GlobalsNodeBuilder.cs
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
using CBinding.Parser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using ClangSharp;

namespace CBinding.Navigation
{
	public class Globals
	{
		Project project;
		
		public Globals (Project project)
		{
			this.project = project;
		}
		
		public Project Project {
			get { return project; }
		}
	}
	
	public class GlobalsNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(Globals); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "Globals";
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder,
										object dataObject,
										NodeInfo nodeInfo)
		{
			nodeInfo.Label = "Globals";
			nodeInfo.Icon = Context.GetIcon (Stock.OpenFolder);
			nodeInfo.ClosedIcon = Context.GetIcon (Stock.ClosedFolder);
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			CProject p = (CProject)treeBuilder.GetParentDataItem (typeof(CProject), false);
			
			if (p == null) return;
			
			ClangProjectSymbolDatabase info = p.DB;
			
			foreach (Symbol glob in info.Globals.Values) {
				/*CXSourceLocation loc = clang.getCursorLocation (glob.Represented);
				CXFile file;
				uint line, column, offset;
				clang.getExpansionLocation (loc, out file, out line, out column, out offset);
				var fileName = clang.getFileName (file).ToString ();
				if(p.IsFileInProject (fileName))*/
				if(glob.Ours)
					treeBuilder.AddChild (glob);
}		
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is Struct)
				return 1;
			else
				return -1;
		}
	}
}
