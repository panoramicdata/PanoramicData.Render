namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static DocumentPropertyMap BuildDocumentPropertyMap(DocxDocument doc, RenderOptions renderOptions)
		=> new(
			doc.Title ?? string.Empty,
			doc.Author ?? string.Empty,
			doc.Subject ?? string.Empty,
			doc.Keywords ?? string.Empty,
			doc.Description ?? string.Empty,
			renderOptions.SourceFilename ?? "(document)");

	private static string GetFieldName(FieldUpdateKind kind)
		=> kind switch
		{
			FieldUpdateKind.Page => "PAGE",
			FieldUpdateKind.NumPages => "NUMPAGES",
			FieldUpdateKind.Toc => "TOC",
			FieldUpdateKind.Title => "TITLE",
			FieldUpdateKind.Author => "AUTHOR",
			FieldUpdateKind.Subject => "SUBJECT",
			FieldUpdateKind.Keywords => "KEYWORDS",
			FieldUpdateKind.Description => "DESCRIPTION",
			FieldUpdateKind.Filename => "FILENAME",
			_ => string.Empty
		};

	private sealed class ActiveFieldUpdate
	{
		public StringBuilder InstructionBuilder { get; } = new();

		public bool IsResultSection { get; private set; }

		public FieldUpdateKind Kind { get; private set; } = FieldUpdateKind.Other;

		public bool HasWrittenValue { get; set; }

		public void BeginResult(FieldUpdateKind kind)
		{
			IsResultSection = true;
			Kind = kind;
		}
	}

	private enum FieldUpdateKind
	{
		Other,
		Page,
		NumPages,
		Toc,
		Title,
		Author,
		Subject,
		Keywords,
		Description,
		Filename
	}
}
