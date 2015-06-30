//
// NamedArgumentCompletionTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using NUnit.Framework;

namespace CBinding
{
	[TestFixture]
	public class NamedArgumentCompletionTests : TestBase
	{
		class TestCompletionWidget : ICompletionWidget 
		{
			DocumentContext documentContext;

			TextEditor editor;

			public TestCompletionWidget (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext documentContext)
			{
				this.editor = editor;
				this.documentContext = documentContext;
			}

			public string CompletedWord {
				get;
				set;
			}
			#region ICompletionWidget implementation
			public event EventHandler CompletionContextChanged {
				add { /* TODO */ }
				remove { /* TODO */ }
			}

			public string GetText (int startOffset, int endOffset)
			{
				return editor.GetTextBetween (startOffset, endOffset);
			}

			public char GetChar (int offset)
			{
				return  editor.GetCharAt (offset);
			}

			public CodeCompletionContext CreateCodeCompletionContext (int triggerOffset)
			{
				var line = editor.GetLineByOffset (triggerOffset); 
				return new CodeCompletionContext {
					TriggerOffset = triggerOffset,
					TriggerLine = line.LineNumber,
					TriggerLineOffset = line.Offset,
					TriggerXCoord = 0,
					TriggerYCoord = 0,
					TriggerTextHeight = 0,
					TriggerWordLength = 0
				};
			}

			public CodeCompletionContext CurrentCodeCompletionContext {
				get {
					return CreateCodeCompletionContext (editor.CaretOffset);
				}
			}

			public string GetCompletionText (CodeCompletionContext ctx)
			{
				return "";
			}

			public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
			{
				this.CompletedWord = complete_word;
			}

			public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int offset)
			{
				this.CompletedWord = complete_word;
			}

			public void Replace (int offset, int count, string text)
			{
			}

			public int CaretOffset {
				get {
					return editor.CaretOffset;
				}
				set {
					editor.CaretOffset = value;
				}
			}

			public int TextLength {
				get {
					return editor.Length;
				}
			}

			public int SelectedLength {
				get {
					return 0;
				}
			}

			public Gtk.Style GtkStyle {
				get {
					return null;
				}
			}

			double ICompletionWidget.ZoomLevel {
				get {
					return 1;
				}
			}

			void ICompletionWidget.AddSkipChar (int cursorPosition, char c)
			{
				// ignore
			}
			#endregion
		}


		static CTextEditorExtension Setup (string input, out TestViewContent content)
		{
			var tww = new TestWorkbenchWindow ();
			content = new TestViewContent ();
			tww.ViewContent = content;
			content.ContentName = "/main.cpp";
			content.Data.MimeType = "text/x-c++src";

			var doc = new MonoDevelop.Ide.Gui.Document (tww);

			var text = input;
			int endPos = text.IndexOf ('$');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			content.Text = text;
			content.CursorPosition = System.Math.Max (0, endPos);

			var project = MonoDevelop.Projects.Services.ProjectService.CreateProject ("C#");
			project.Name = "test";
			project.FileName = "test.csproj";
			project.Files.Add (new ProjectFile (content.ContentName, BuildAction.Compile)); 

			var solution = new MonoDevelop.Projects.Solution ();
			solution.AddConfiguration ("", true); 
			solution.DefaultSolutionFolder.AddItem (project);
			using (var monitor = new ProgressMonitor ())
				TypeSystemService.Load (solution, monitor, false);
			content.Project = project;
			doc.SetProject (project);

			var compExt = new CTextEditorExtension ();
			compExt.Initialize (doc.Editor, doc);
			content.Contents.Add (compExt);

			doc.UpdateParseDocument ();
			TypeSystemService.Unload (solution);
			return compExt;
		}

		string Test(string input, string type, string member, Gdk.Key key = Gdk.Key.Return)
		{
			TestViewContent content;
			var ext = Setup (input, out content);

			var listWindow = new CompletionListWindow ();
			var widget = new TestCompletionWidget (ext.Editor, ext.DocumentContext);
			listWindow.CompletionWidget = widget;
			listWindow.CodeCompletionContext = widget.CurrentCodeCompletionContext;
			var sm = ext.DocumentContext.ParsedDocument.GetAst<SemanticModel> ();

			var t = sm.Compilation.GetTypeByMetadataName (type); 
			var foundMember = t.GetMembers().First (m => m.Name == member);
			var factory = new RoslynCodeCompletionFactory (ext, sm);
			var data = new RoslynSymbolCompletionData (null, factory, foundMember);
			data.DisplayFlags |= DisplayFlags.NamedArgument;
			KeyActions ka = KeyActions.Process;
			data.InsertCompletionText (listWindow, ref ka, KeyDescriptor.FromGtk (key, (char)key, Gdk.ModifierType.None)); 

			return widget.CompletedWord;
		}


		[Test]
		public void TestSimpleCase ()
		{
			IdeApp.Preferences.AddParenthesesAfterCompletion.Set (true); 
			string completion = Test (
				@"
					struct TesterClass
					{
						int foo;
						void method ()
						{
						}
						TesterClass & operator=(const TesterClass &rhs) 
						{
							return *this;
						}
					}
					
					int main(){
						TesterClass tester;
						tester.$
					}
				",
				"MyClass",
				"foo"
			);
			Assert.AreEqual ("foo = ", completion); 
		}


		[Test]
		public void TestNoAutoCase ()
		{
			IdeApp.Preferences.AddParenthesesAfterCompletion.Set (false); 
			string completion = Test (
				@"
					struct TesterClass
					{
						int foo;
						void method ()
						{
						}
						TesterClass & operator=(const TesterClass &rhs) 
						{
							return *this;
						}
					}
					
					int main(){
						TesterClass tester;
						tester.$
					}
				",
				"MyClass",
				"foo",
				Gdk.Key.space
			);
			Assert.AreEqual ("foo", completion); 
		}


	}
}



/*
 * using System;
using NUnit.Framework;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace CBinding
{
	[TestFixture]
	public class Tests : TestBase
	{
		string mainSourceCode =
			"struct TesterClass" + Environment.NewLine +
			"{" + Environment.NewLine +
			"int field;" + Environment.NewLine +
			"void method(){" + Environment.NewLine +
			"}" + Environment.NewLine +
			"TesterClass & operator=(const TesterClass &rhs){" + Environment.NewLine +
			"return *this;" + Environment.NewLine +
			"}" + Environment.NewLine +
			"};" + Environment.NewLine +
			"int main(){" + Environment.NewLine +
			"TesterClass tester;" + Environment.NewLine +
			"tester." + Environment.NewLine +
			"}";
		
		[Test]
		public void TestCodeCompletion ()
		{
			var wBW = new TestWorkbenchWindow();
			var content = new TestViewContent ();
			wBW.ViewContent = content;
			content.ContentName = "main.cpp";
			content.GetTextEditorData ().MimeType = "text/x-c++src";
			var tCEE = new TestCTextEditorExtension();
			var document = new Document (wBW);
			document.SetProject (new CProject() as Project);
			document.Editor.FileName = "main.cpp";
			document.Editor.Text = mainSourceCode;
			document.Editor.SetCaretLocation (12, 7);
			tCEE.Initialize (document.Editor, document);
			var cCC = new CodeCompletionContext();
			cCC.TriggerLine = 1;
			cCC.TriggerLineOffset = 1;
			var cS = tCEE.HandleCodeCompletionAsync (cCC, ' ');
			foreach(var S in cS.Result){
				Console.WriteLine (S.CompletionText);
			}
		}
	}
}

*/