using System;
using NUnit.Framework;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Projects;
using Mono.Addins;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using Mono.TextEditor;

namespace CBinding
{
	[TestFixture]
	public class Tests : TestBase
	{
		string mainSourceCode =
			"struct TesterClass" + System.Environment.NewLine +
			"{" + System.Environment.NewLine +
			"int field;" + System.Environment.NewLine +
			"void method(){" + System.Environment.NewLine +
			"}" + System.Environment.NewLine +
			"TesterClass & operator=(const TesterClass &rhs){" + System.Environment.NewLine +
			"return *this;" + System.Environment.NewLine +
			"}" + System.Environment.NewLine +
			"};" + System.Environment.NewLine +
			"int main(){" + System.Environment.NewLine +
			"TesterClass tester;" + System.Environment.NewLine +
			"tester." + System.Environment.NewLine +
			"}";
		[Test]
		public void TestCodeCompletion ()
		{
			AddinManager.Initialize (".", "/home/qz9n1f/sandbox/GSoCMONO/monodevelop-WIP/monodevelop/main/build/AddIns/");
			IWorkbenchWindow wBW = new TestWorkbenchWindow();
			var content = new TestViewContent ();
			wBW.ActiveViewContent = content;
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

