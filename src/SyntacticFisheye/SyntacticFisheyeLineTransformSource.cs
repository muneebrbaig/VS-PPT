using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.SyntacticFisheye
{
  [Export(typeof(EditorOptionDefinition))]
  [Name(StaticName)]
  public sealed class SyntacticFisheyeCompressBlankLines : ViewOptionDefinition<bool>
  {
    public const string StaticName = "SyntacticFisheyeCompressBlankLines";
    public static readonly EditorOptionKey<bool> StaticKey = new EditorOptionKey<bool>(StaticName);

    public override bool Default => true;

    public override EditorOptionKey<bool> Key => StaticKey;
  }

  [Export(typeof(EditorOptionDefinition))]
  [Name(StaticName)]
  public sealed class SyntacticFisheyeCompressSimpleLines : ViewOptionDefinition<bool>
  {
    public const string StaticName = "SyntacticFisheyeCompressSimpleLines";
    public static readonly EditorOptionKey<bool> StaticKey = new EditorOptionKey<bool>(StaticName);
    public override bool Default => true;
    public override EditorOptionKey<bool> Key => StaticKey;
  }

  public class SyntacticFisheyeLineTransformSource : ILineTransformSource
  {
    private readonly IWpfTextView _view;
    private bool _compressBlankLines;
    private bool _compressSimpleLines;

    private SyntacticFisheyeLineTransformSource(IWpfTextView view)
    {
      _view = view;

      _compressBlankLines = _view.Options.GetOptionValue(SyntacticFisheyeCompressBlankLines.StaticKey);
      _compressSimpleLines = _view.Options.GetOptionValue(SyntacticFisheyeCompressSimpleLines.StaticKey);
      _view.Options.OptionChanged += OnOptionChanged;
      _view.Closed += OnClosed;
    }

    #region ILineTransformSource Members

    public LineTransform GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement)
    {
      if (!(_compressBlankLines || _compressSimpleLines) ||
          line.Length > 100 || line.End > line.Start.GetContainingLine().End ||
          !line.IsFirstTextViewLineForSnapshotLine || !line.IsLastTextViewLineForSnapshotLine)
        return
          s_defaultTransform; //Long or wrapped lines -- even if they don't contain interesting characters -- get the default transform to avoid the cost of checking the entire line.

      // Get the line text for analysis
      var lineText = GetLineText(line);

      // Check if line should be compressed based on content
      if (ShouldCompressLine(lineText, out var isBlankLine))
      {
        // Don't compress blank lines if the option is disabled
        if (isBlankLine && !_compressBlankLines) return s_defaultTransform;

        // Don't compress simple/structural lines if the option is disabled
        if (!isBlankLine && !_compressSimpleLines) return s_defaultTransform;

        return s_simpleTransform;
      }

      return s_defaultTransform;
    }

    #endregion

    /// <summary>
    ///   Static class factory that ensures a single instance of the line transform source/view.
    /// </summary>
    public static SyntacticFisheyeLineTransformSource Create(IWpfTextView view) =>
      view.Properties.GetOrCreateSingletonProperty(delegate { return new SyntacticFisheyeLineTransformSource(view); });

    private void OnOptionChanged(object sender, EditorOptionChangedEventArgs e)
    {
      if (e.OptionId == SyntacticFisheyeCompressBlankLines.StaticName ||
          e.OptionId == SyntacticFisheyeCompressSimpleLines.StaticName)
      {
        _compressBlankLines = _view.Options.GetOptionValue(SyntacticFisheyeCompressBlankLines.StaticKey);
        _compressSimpleLines = _view.Options.GetOptionValue(SyntacticFisheyeCompressSimpleLines.StaticKey);

        if (!(_view.IsClosed || _view.InLayout))
        {
          var firstLine = _view.TextViewLines.FirstVisibleLine;
          _view.DisplayTextLineContainingBufferPosition(firstLine.Start, firstLine.Top - _view.ViewportTop,
            ViewRelativePosition.Top);
        }
      }
    }

    private void OnClosed(object sender, EventArgs e)
    {
      _view.Options.OptionChanged -= OnOptionChanged;
      _view.Closed -= OnClosed;
    }

    #region private members

    private static readonly LineTransform s_defaultTransform = new LineTransform(0.0, 0.0, 1.0); //No compression

    private static readonly LineTransform
      s_simpleTransform = new LineTransform(0.0, 0.0, 0.75); //75% vertical compression
    //private static readonly LineTransform _blankTransform = new LineTransform(0.0, 0.0, 0.5);    //50% vertical compression

    #endregion

    #region Helper Methods

    /// <summary>
    ///   Extracts the text content of a line
    /// </summary>
    private static string GetLineText(ITextViewLine line)
    {
      if (line.Length == 0)
        return string.Empty;

      var snapshot = line.Snapshot;
      var start = line.Start.Position;
      var length = line.End.Position - start;

      return snapshot.GetText(start, length);
    }

    /// <summary>
    ///   Determines if a line should be compressed based on its content
    /// </summary>
    /// <param name="lineText">The text content of the line</param>
    /// <param name="isBlankLine">Output parameter indicating if this is a blank line</param>
    /// <returns>True if the line should be compressed, false otherwise</returns>
    private static bool ShouldCompressLine(string lineText, out bool isBlankLine)
    {
      isBlankLine = false;

      if (string.IsNullOrEmpty(lineText))
      {
        isBlankLine = true;
        return true;
      }

      var trimmedLine = lineText.TrimStart();

      // Check for blank lines (only whitespace)
      if (string.IsNullOrWhiteSpace(lineText))
      {
        isBlankLine = true;
        return true;
      }

      // Check for using declarations
      if (trimmedLine.StartsWith("using ") && (trimmedLine.Contains(";") || trimmedLine.Contains("="))) return true;

      // Check for namespace declarations
      if (trimmedLine.StartsWith("namespace ") || trimmedLine.StartsWith("#")) return true;

      // Check for single-line comments
      if (trimmedLine.StartsWith("//")) return true;

      // Check for multi-line comment start
      if (trimmedLine.StartsWith("/*") || trimmedLine.StartsWith("*")) return true;

      // Check for multi-line comment end
      if (trimmedLine.EndsWith("*/")) return true;

      // Check for attributes
      if (trimmedLine.StartsWith("[") && trimmedLine.Contains("]")) return true;

      // Check for lines with no letters or digits (original simple line logic)
      var hasLetterOrDigit = false;
      var allWhiteSpace = true;

      foreach (var c in lineText)
      {
        if (char.IsLetterOrDigit(c))
        {
          hasLetterOrDigit = true;
          break;
        }

        if (!char.IsWhiteSpace(c)) allWhiteSpace = false;
      }

      if (!hasLetterOrDigit)
      {
        isBlankLine = allWhiteSpace;
        return true;
      }

      return false;
    }

    #endregion
  }
}