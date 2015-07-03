//
// FunctionNodeBuilder.cs
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;

namespace CBinding.Navigation
{
	public class MemberFunctionNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(MemberFunction); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(SymbolCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((MemberFunction)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder,
		                                object dataObject,
		                                NodeInfo nodeInfo)
		{
			MemberFunction f = (MemberFunction)dataObject;
				
			nodeInfo.Label = f.Name;
			
			switch (f.Access)
			{
			case CX_CXXAccessSpecifier.CX_CXXPublic:
				nodeInfo.Icon = Context.GetIcon (Stock.Method);
				break;
			case CX_CXXAccessSpecifier.CX_CXXProtected:
				nodeInfo.Icon = Context.GetIcon (Stock.ProtectedMethod);
				break;
			case CX_CXXAccessSpecifier.CX_CXXPrivate:
				nodeInfo.Icon = Context.GetIcon (Stock.PrivateMethod);
				break;
			case CX_CXXAccessSpecifier.CX_CXXInvalidAccessSpecifier:
				nodeInfo.Icon = Context.GetIcon (Stock.Method);
				break;
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is Typedef)
				return 1;
			else
				return -1;
		}
	}
}
