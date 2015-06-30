//
// CTextEditorExtension.cs
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
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using System.Threading.Tasks;
using System.Threading;
using ClangSharp;
using System.Runtime.InteropServices;
using MonoDevelop.Ide.Commands;
using CBinding.Refactoring;
using CBinding.Parser;


namespace CBinding
{
	public class CTextEditorExtension : CompletionTextEditorExtension, IPathedDocument
	{
		char previous = ' ';
		List<CXUnsavedFile> unsavedFiles;

		/// <summary>
		/// Allowed chars to be next to an identifier
		/// </summary>
		static char[] allowedChars = new char[] {
			'.', ':', ' ', '\t', '=', '*', '+', '-', '/', '%', ',', '&',
			'|', '^', '{', '}', '[', ']', '(', ')', '\n', '!', '?', '<', '>'
		};

		/// <summary>
		/// Gets the completion language.
		/// </summary>
		/// <value>The completion language.</value>
		public override string CompletionLanguage {
			get {
				return "C/C++";
			}
		}

		/// <summary>
		/// Determines if an opening brace character is specified by c.
		/// </summary>
		/// <returns><c>true</c> if c is an opening brace character; otherwise, <c>false</c>.</returns>
		/// <param name="c">Character under surveillance.</param>
		static bool IsOpenBrace (char c)
		{
			return c == '(' || c == '{' || c == '<' || c == '[';
		}

		/// <summary>
		/// Determines if a closing brace character is specified by c.
		/// </summary>
		/// <returns><c>true</c> if c is an closing brace character; otherwise, <c>false</c>.</returns>
		/// <param name="c">Character under surveillance.</param>
		static bool IsCloseBrace (char c)
		{
			return c == ')' || c == '}' || c == '>' || c == ']';
		}

		/// <summary>
		/// Determines if c is a brace character.
		/// </summary>
		/// <returns><c>true</c> if c is a brace; otherwise, <c>false</c>.</returns>
		/// <param name="c">C.</param>
		static bool IsBrace (char c)
		{
			return IsOpenBrace  (c) || IsCloseBrace (c);
		}

		/// <summary>
		/// Searchs the matching bracket to the bracket under caret position.
		/// </summary>
		/// <returns>The matching bracket.</returns>
		/// <param name="editor">Editor.</param>
		/// <param name="offset">Offset in the text.</param>
		/// <param name="openBracket">Opening bracket character.</param>
		/// <param name="closingBracket">Closing bracket character.</param>
		/// <param name="direction">Direction.</param>
		static int SearchMatchingBracket (IReadonlyTextDocument editor, int offset, char openBracket, char closingBracket, int direction)
		{
			bool isInString       = false;
			bool isInChar         = false;	
			bool isInBlockComment = false;
			int depth = -1;
			while (offset >= 0 && offset < editor.Length) {
				char ch = editor.GetCharAt (offset);
				switch (ch) {
					case '/':
						if (isInBlockComment) 
							isInBlockComment = editor.GetCharAt (offset + direction) != '*';
						if (!isInString && !isInChar && offset - direction < editor.Length) 
							isInBlockComment = offset > 0 && editor.GetCharAt (offset - direction) == '*';
						break;
					case '"':
						if (!isInChar && !isInBlockComment) 
							isInString = !isInString;
						break;
					case '\'':
						if (!isInString && !isInBlockComment) 
							isInChar = !isInChar;
						break;
					default :
						if (ch == closingBracket) {
							if (!(isInString || isInChar || isInBlockComment)) 
								--depth;
						} else if (ch == openBracket) {
							if (!(isInString || isInChar || isInBlockComment)) {
								++depth;
								if (depth == 0) 
									return offset;
							}
						}
						break;
				}
				offset += direction;
			}
			return -1;
		}

		/// <summary>
		/// Gets the closing brace for line.
		/// </summary>
		/// <returns>The closing brace for line.</returns>
		/// <param name="editor">Editor.</param>
		/// <param name="line">Line.</param>
		/// <param name="openingLine">Opening line.</param>
		static int GetClosingBraceForLine (IReadonlyTextDocument editor, IDocumentLine line, out int openingLine)
		{
			int offset = SearchMatchingBracket (editor, line.Offset, '{', '}', -1);
			if (offset == -1) {
				openingLine = -1;
				return -1;
			}
				
			openingLine = editor.OffsetToLineNumber (offset);
			return offset;
		}

		/// <summary>
		/// Return true if the key press should be processed by the editor.
		///  When a key is pressed, and before the key is processed by the editor, this method will be invoked.
		/// </summary>
		/// <returns><c>true</c>, if press was keyed, <c>false</c> otherwise.</returns>
		/// <param name="descriptor">Descriptor of the key pressed.</param>
		public override bool KeyPress (KeyDescriptor descriptor)
		{
			var line = Editor.GetLine (Editor.CaretLine);
			string lineText = Editor.GetLineText (Editor.CaretLine);
			int lineCursorIndex = Math.Min (lineText.Length, Editor.CaretColumn);
			
			// Smart Indentation
			if (Editor.Options.IndentStyle == IndentStyle.Smart)
			{
				if (descriptor.KeyChar == '}') {
					// Only indent if the brace is preceeded by whitespace.
					if(AllWhiteSpace(lineText.Substring(0, lineCursorIndex))) {
						int braceOpeningLine;
						if(GetClosingBraceForLine(Editor, line, out braceOpeningLine) >= 0)
						{
							Editor.ReplaceText (line.Offset, line.Length, GetIndent(Editor, braceOpeningLine, 0) + "}" + lineText.Substring(lineCursorIndex));
							return false;
						}
					}
				} else {
					switch(descriptor.SpecialKey)
					{
						case SpecialKey.Return:
							// Calculate additional indentation, if any.
							char finalChar = '\0';
							char nextChar = '\0';
							string indent = String.Empty;
							if (!String.IsNullOrEmpty (Editor.SelectedText)) {
								int cursorPos = Editor.SelectionRange.Offset;

								Editor.RemoveText (Editor.SelectionRange);
							
								Editor.CaretOffset = cursorPos;
								
								lineText = Editor.GetLineText (Editor.CaretLine);
								lineCursorIndex = Editor.CaretColumn;
	//							System.Console.WriteLine(TextEditorData.Caret.Offset);
							}
							if(lineText.Length > 0)
							{
								if(lineCursorIndex > 0)
									finalChar = lineText[Math.Min(lineCursorIndex, lineText.Length) - 1];
								
								if(lineCursorIndex < lineText.Length)
									nextChar = lineText[lineCursorIndex];
	
								if(finalChar == '{')
									indent = Editor.Options.GetIndentationString ();
							}
	
							// If the next character is an closing brace, indent it appropriately.
							if(IsBrace(nextChar) && !IsOpenBrace(nextChar))
							{
								int openingLine;
								if(GetClosingBraceForLine (Editor, line, out openingLine) >= 0)
								{
									Editor.InsertAtCaret (Editor.EolMarker + GetIndent(Editor, openingLine, 0));
									return false;
								}
							}
						
							// Default indentation method
							Editor.InsertAtCaret (Editor.EolMarker + indent + GetIndent(Editor, Editor.OffsetToLineNumber (line.Offset), lineCursorIndex));
							return false;
						
					}
				}
			}
			return base.KeyPress (descriptor);
		}

		/// <summary>
		/// Determine if code completion should be invoked by pressed char's (key's) value.
		/// </summary>
		/// <returns><c>true</c>, if complete should be invoked, <c>false</c> otherwise.</returns>
		/// <param name="pressed">Pressed.</param>
		bool shouldCompleteOn(char pressed) {
			switch (pressed) {
			case '.':
				return true;
			case '(':
				return true;
			case ' ':
				return true;
			case '>':
				if (previous == '-')
					return true;
				return false;
			case ':':
				if (previous == ':')
					return true;
				return false;
			default:
				return false;

			}
		}

		/// <summary>
		/// Handles the code completion async. Invoked automatically on every keypress.
		/// </summary>
		/// <returns>The code completion async.</returns>
		/// <param name="completionContext">Completion context.</param>
		/// <param name="completionChar">Completion char.</param>
		/// <param name="token">Token.</param>
		public override Task<ICompletionDataList> HandleCodeCompletionAsync (CodeCompletionContext completionContext, char completionChar, CancellationToken token = default(CancellationToken))
		{
			ICompletionDataList list = new CompletionDataList ();
			if (shouldCompleteOn(completionChar)) {
				CProject project = DocumentContext.Project as CProject;
				unsavedFiles = new List<CXUnsavedFile> ();
				foreach (Document openDocument in MonoDevelop.Ide.IdeApp.Workbench.Documents) {
					if (openDocument.IsDirty) {
						CXUnsavedFile unsavedFile = new CXUnsavedFile ();
						unsavedFile.Filename = openDocument.FileName;
						unsavedFile.Length = openDocument.Editor.Text.Length;
						unsavedFile.Contents = openDocument.Editor.Text;
						if (project.BOMPresentInFile [openDocument.FileName]) {
							unsavedFile.Length += 3;
							unsavedFile.Contents = "   " + unsavedFile.Contents;
						}
						unsavedFiles.Add (unsavedFile);
					}
				}
				string operatorPattern = "operator\\s*(\\+|\\-|\\*|\\/|\\%|\\^|\\&|\\||\\~|\\!|\\=|\\<|\\>|\\(\\s*\\)|\\[\\s*\\]|new|delete)";
				bool fieldOrMethodMode = completionChar == '.' || completionChar == '>' ? true : false;
				IntPtr pResults = project.cLangManager.codeComplete (completionContext, unsavedFiles.ToArray (), DocumentContext.Name);
				CXCodeCompleteResults results = Marshal.PtrToStructure<CXCodeCompleteResults> (pResults);
				if (results.Results.ToInt64 () != 0) {
					for(int i = 0; i < results.NumResults; i++) {
						IntPtr iteratingPointer = results.Results + i * Marshal.SizeOf<CXCompletionResult>();
						CXCompletionResult resultItem = Marshal.PtrToStructure<CXCompletionResult> (iteratingPointer);
						CXCompletionString completionString = new CXCompletionString(resultItem.CompletionString);
						uint completionchunknum = clang.getNumCompletionChunks (completionString.Pointer);
						for (uint j = 0; j < completionchunknum; j++) {
							if (clang.getCompletionChunkKind(completionString.Pointer, j) != CXCompletionChunkKind.CXCompletionChunk_TypedText)
								continue;
							uint priority = clang.getCompletionPriority (completionString.Pointer);
							CXString cxstring = clang.getCompletionChunkText(completionString.Pointer, j);
							string realstring = Marshal.PtrToStringAnsi (clang.getCString(cxstring));
							clang.disposeString (cxstring);
							if (resultItem.CursorKind == CXCursorKind.CXCursor_Destructor
								|| resultItem.CursorKind == CXCursorKind.CXCursor_UnaryOperator
								|| resultItem.CursorKind == CXCursorKind.CXCursor_BinaryOperator
								|| resultItem.CursorKind == CXCursorKind.CXCursor_CompoundAssignOperator
								|| System.Text.RegularExpressions.Regex.IsMatch (realstring, operatorPattern)
								|| (fieldOrMethodMode
									&& (resultItem.CursorKind == CXCursorKind.CXCursor_ClassDecl 
									|| resultItem.CursorKind == CXCursorKind.CXCursor_StructDecl)
								)){
								continue;
							}
							ClangCompletionUnit item = new ClangCompletionUnit (resultItem, realstring, priority);
							list.Add (item);
						}
					}
				}
				clang.disposeCodeCompleteResults (pResults);
			}
			previous = completionChar;
			list.Sort ((x, y) => x.CompareTo (y));
			return Task.FromResult (list);
		}

		/// <summary>
		/// Code completion command invoked on explicit code completion requests.
		/// </summary>
		/// <returns>The completion command.</returns>
		/// <param name="completionContext">Completion context.</param>
		public override Task<ICompletionDataList> CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			return HandleCodeCompletionAsync (completionContext, ' ');
		}

		/// <summary>
		/// Handles the parameter completion async. Invoked automatically on every keypress.
		/// </summary>
		/// <returns>The parameter completion async.</returns>
		/// <param name="completionContext">Completion context.</param>
		/// <param name="completionChar">Completion char.</param>
		/// <param name="token">Token.</param>s
		public override Task<MonoDevelop.Ide.CodeCompletion.ParameterHintingResult> HandleParameterCompletionAsync (CodeCompletionContext completionContext, char completionChar, CancellationToken token = default(CancellationToken))
		{
			if (completionChar != '(')
				return null;

			CProject project = DocumentContext.Project as CProject;

			if (project == null)
				return Task.FromResult<MonoDevelop.Ide.CodeCompletion.ParameterHintingResult> (null);

			var functions = (DocumentContext.Project as CProject).db.Functions;
			string lineText = Editor.GetLineText (Editor.CaretLine).TrimEnd ();
			if (lineText.EndsWith (completionChar.ToString (), StringComparison.Ordinal))
				lineText = lineText.Remove (lineText.Length - 1).TrimEnd ();

			int nameStart = lineText.LastIndexOfAny (allowedChars);

			nameStart++;

			string functionName = lineText.Substring (nameStart).Trim ();

			if (string.IsNullOrEmpty (functionName))
				return Task.FromResult<MonoDevelop.Ide.CodeCompletion.ParameterHintingResult> (null);

			return Task.FromResult (
				(MonoDevelop.Ide.CodeCompletion.ParameterHintingResult)
				new ParameterDataProvider (nameStart, Editor,functions.Values.ToList (), functionName)
			);
		}

		/// <summary>
		/// Determines if the line containts only whitespaces.
		/// </summary>
		/// <returns><c>true</c>, if line contains only whitespaces, <c>false</c> otherwise.</returns>
		/// <param name="lineText">Line text.</param>
		bool AllWhiteSpace (string lineText)
		{
			// We will almost definately need a faster method than this
			foreach (char c in lineText)
				if (!char.IsWhiteSpace (c))
					return false;
			
			return true;
		}
		
		// Snatched from DefaultFormattingStrategy
		/// <summary>
		/// Gets the indentation of the line.
		/// </summary>
		/// <returns>The indent.</returns>
		/// <param name="d">D.</param>
		/// <param name="lineNumber">Line number.</param>
		/// <param name="terminateIndex">Terminate index.</param>
		string GetIndent (IReadonlyTextDocument d, int lineNumber, int terminateIndex)
		{
			string lineText = d.GetLineText (lineNumber);
			if(terminateIndex > 0)
				lineText = terminateIndex < lineText.Length ? lineText.Substring(0, terminateIndex) : lineText;
			
			StringBuilder whitespaces = new StringBuilder ();
			
			foreach (char ch in lineText) {
				if (!char.IsWhiteSpace (ch))
					break;
				whitespaces.Append (ch);
			}
			
			return whitespaces.ToString ();
		}

		/// <summary>
		/// Opens up matching file to cpp/hpp
		/// </summary>
		[CommandHandler (MonoDevelop.DesignerSupport.Commands.SwitchBetweenRelatedFiles)]
		protected void Run ()
		{
			var cp = this.DocumentContext.Project as CProject;
			if (cp != null) {
				string match = cp.MatchingFile (this.DocumentContext.Name);
				if (match != null)
					MonoDevelop.Ide.IdeApp.Workbench.OpenDocument (match, cp, true);
			}
		}

		/// <summary>
		/// Update the specified info with information about a matching file existing or not.
		/// </summary>
		/// <param name="info">Info.</param>
		[CommandUpdateHandler (MonoDevelop.DesignerSupport.Commands.SwitchBetweenRelatedFiles)]
		protected void Update (CommandInfo info)
		{
			var cp = this.DocumentContext.Project as CProject;
			info.Visible = info.Visible = cp != null && cp.MatchingFile (this.DocumentContext.Name) != null;
		}
		
		#region IPathedDocument implementation
		
		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;
		
		public Gtk.Widget CreatePathWidget (int index)
		{
			PathEntry[] path = CurrentPath;
			if (null == path || 0 > index || path.Length <= index) {
				return null;
			}
			
			object tag = path[index].Tag;
			DropDownBoxListWindow.IListDataProvider provider = null;
			if (tag is ParsedDocument) {
			} else {
				// TODO: Roslyn port
				//provider = new DataProvider (Editor, DocumentContext, tag, new NetAmbience ());
			}
			
			DropDownBoxListWindow window = new DropDownBoxListWindow (provider);
			window.SelectItem (tag);
			return window;
		}

		public PathEntry[] CurrentPath {
			get;
			private set;
		}
		
		protected virtual void OnPathChanged (DocumentPathChangedEventArgs args)
		{
			if (PathChanged != null)
				PathChanged (this, args);
		}
		
		#endregion
		
		protected override void Initialize ()
		{
			base.Initialize ();
		}
			
		public override void Dispose ()
		{
			base.Dispose ();
		}

		/// <summary>
		/// Command binding
		/// Invokes Go to declaration handler's Run()
		/// </summary>
		[CommandHandler (MonoDevelop.Refactoring.RefactoryCommands.GotoDeclaration)]
		public void GotoDeclaration ()
		{
			GotoDeclarationHandler gotoDec = new GotoDeclarationHandler();
			gotoDec.Run ();
		}

		/// <summary>
		/// Command binding.
		/// Determines whether go to declaration is possible
		/// Invoked GotoDeclarationHandler.Update()
		/// </summary>
		/// <returns><c>true</c> if can go to declaration of the cursor at caret position; otherwise, <c>false</c>.</returns>
		/// <param name="item">Item.</param>
		[CommandUpdateHandler (MonoDevelop.Refactoring.RefactoryCommands.GotoDeclaration)]
		public void CanGotoDeclaration (CommandInfo item)
		{
			GotoDeclarationHandler gotoDec = new GotoDeclarationHandler();
			gotoDec.Update (item);
		}
			
		#region copied with modifications from CSharpBinding
		/// <summary>
		/// Find references handler update bind.
		/// </summary>
		/// <param name="ci">Ci.</param>
		[CommandUpdateHandler (MonoDevelop.Refactoring.RefactoryCommands.FindReferences)]
		public void FindReferencesHandler_Update (CommandInfo ci)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			FindReferencesHandler findReferencesHandler = new FindReferencesHandler (
				DocumentContext.Project as CProject,
				doc
			);
			findReferencesHandler.Update (ci);
		}

		/// <summary>
		/// Finds the references command bind.
		/// </summary>
		[CommandHandler (MonoDevelop.Refactoring.RefactoryCommands.FindReferences)]
		public void FindReferences ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			FindReferencesHandler findReferencesHandler = new FindReferencesHandler (
				DocumentContext.Project as CProject,
				doc
			);
			findReferencesHandler.Run ();
		}

		/*readonly FindDerivedSymbolsHandler findDerivedSymbolsHandler = new FindDerivedSymbolsHandler (DocumentContext.Project as CProject);
		[CommandUpdateHandler (MonoDevelop.Refactoring.RefactoryCommands.FindDerivedClasses)]
		public void FindDerivedClasses_Update (CommandInfo ci)
		{
			findDerivedSymbolsHandler.Update (ci);
		}

		[CommandHandler (MonoDevelop.Refactoring.RefactoryCommands.FindDerivedClasses)]
		public void FindDerivedClasses ()
		{
			findDerivedSymbolsHandler.Run (DocumentContext.Project as CProject);
		}*/

		/// <summary>
		/// Rename command update and binding.
		/// </summary>
		/// <param name="ci">Ci.</param>
		[CommandUpdateHandler (EditCommands.Rename)]
		public void RenameCommand_Update (CommandInfo ci)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			RenameHandlerDialog renameHandler = new RenameHandlerDialog (DocumentContext.Project as CProject, doc);
			renameHandler.Update (ci);
		}

		/// <summary>
		/// Rename command run and binding.
		/// </summary>
		[CommandHandler (EditCommands.Rename)]
		public void RenameCommand ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			RenameHandlerDialog renameHandler = new RenameHandlerDialog (DocumentContext.Project as CProject, doc);
			renameHandler.RunRename ();
		}
		#endregion
	}
}
