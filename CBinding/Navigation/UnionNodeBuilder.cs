//
// UnionNodeBuilder.cs
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
using ClangSharp;
using MonoDevelop.Ide.Gui.Components;

namespace CBinding.Navigation
{
	public class UnionNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(Union); }
		}

		public override Type CommandHandlerType {
			get { return typeof(SymbolCommandHandler); }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			Union u = (Union)dataObject;
			return u.IsDefinition
				? 
				u.Name
					:
				u.Name + " declaration";
		}

		public override void BuildNode (ITreeBuilder treeBuilder,
										object dataObject,
										NodeInfo nodeInfo)
		{
			Union u = (Union)dataObject;
				
			nodeInfo.Label = u.IsDefinition
				? 
				u.Name
				:
				u.Name + " declaration";
			
			switch (u.Access) {
			case CX_CXXAccessSpecifier.@Public:
				nodeInfo.Icon = Context.GetIcon ("md-union");
				break;
			case CX_CXXAccessSpecifier.@Protected:
				nodeInfo.Icon = Context.GetIcon ("md-protected-union");
				break;
			case CX_CXXAccessSpecifier.@Private:
				nodeInfo.Icon = Context.GetIcon ("md-private-union");
				break;
			case CX_CXXAccessSpecifier.@InvalidAccessSpecifier:
				nodeInfo.Icon = Context.GetIcon ("md-union");
				break;
			}
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			CProject p = (CProject)treeBuilder.GetParentDataItem (typeof(CProject), false);
			
			if (p == null)
				return;
			
			SymbolDatabaseMediator info = p.DB;
			
			bool publicOnly = treeBuilder.Options ["PublicApiOnly"];
			Union thisUnion = (Union)dataObject;

			/*foreach (Symbol s in info.CanBeInClasses.Values)
				if (s.Ours && s.Parent != null && s.Parent.Equals (thisUnion) && (!publicOnly || s.Access == CX_CXXAccessSpecifier.@Public))
					treeBuilder.AddChild (s);*/
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
