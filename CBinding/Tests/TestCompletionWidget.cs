using System;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;

namespace CBinding
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
}
