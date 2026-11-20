namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

internal static partial class FieldUpdateTestDocuments
{

	internal static Paragraph CreateFieldParagraph(string instruction, string cachedValue, string? trailingText, bool pageBreakBefore)
	{
		var paragraph = new Paragraph();
		if (pageBreakBefore)
		{
			paragraph.AppendChild(new ParagraphProperties(new PageBreakBefore()));
		}

		paragraph.Append(
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode { Space = SpaceProcessingModeValues.Preserve, Text = $" {instruction} " }),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text(cachedValue)),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
		if (trailingText is not null)
		{
			paragraph.Append(new Run(new Text(trailingText) { Space = SpaceProcessingModeValues.Preserve }));
		}

		return paragraph;
	}

	internal static Paragraph CreateStyledParagraph(
		string styleId,
		string text,
		bool pageBreakBefore = false,
		string? bookmarkName = null,
		int bookmarkId = 0)
	{
		var properties = new ParagraphProperties(new ParagraphStyleId { Val = styleId });
		if (pageBreakBefore)
		{
			properties.AppendChild(new PageBreakBefore());
		}

		var paragraph = new Paragraph(properties);
		if (!string.IsNullOrWhiteSpace(bookmarkName))
		{
			paragraph.AppendChild(new BookmarkStart
			{
				Id = bookmarkId.ToString(System.Globalization.CultureInfo.InvariantCulture),
				Name = bookmarkName
			});
		}

		paragraph.AppendChild(new Run(new Text(text)));

		if (!string.IsNullOrWhiteSpace(bookmarkName))
		{
			paragraph.AppendChild(new BookmarkEnd
			{
				Id = bookmarkId.ToString(System.Globalization.CultureInfo.InvariantCulture)
			});
		}

		return paragraph;
	}

	internal static MemoryStream CreateDocxWithSequenceFields()
		=> CreateSeqDocx(("SEQ Figure", " caption text"), ("SEQ Figure", " more text"), ("SEQ Table", " first table"));

	internal static MemoryStream CreateDocxWithSequenceFieldsAndResetSwitch()
		=> CreateSeqDocx(("SEQ Figure", " first"), ("SEQ Figure", " second"), ("SEQ Figure \\r 10", " reset"), ("SEQ Figure", " after reset"));

	internal static MemoryStream CreateDocxWithSequenceFieldsAndHiddenSwitch()
		=> CreateSeqDocx(("SEQ Figure", " visible"), ("SEQ Figure \\h", " hidden counter"), ("SEQ Figure", " skipped two"));

	private static MemoryStream CreateSeqDocx(params (string Instruction, string TrailingText)[] fields)
		=> BuildDocx(fields.Select(field => CreateSeqFieldParagraph(field.Instruction, "X", field.TrailingText)).ToArray());
	internal static Paragraph CreateSeqFieldParagraph(string instruction, string cachedValue, string trailingText)
		=> CreateFieldParagraph(instruction, cachedValue, trailingText, false);
	internal static MemoryStream CreateDocxWithPageRefField()
	{
		return BuildDocx(
			CreateComplexFieldParagraph("PAGEREF _RefTarget", "99"),
			CreateStyledParagraph("Heading1", "Target Heading", pageBreakBefore: true, bookmarkName: "_RefTarget", bookmarkId: 1));
	}

	internal static MemoryStream CreateDocxWithRefField()
	{
		return BuildDocx(
			CreateComplexFieldParagraph("REF _RefBookmark", "old text"),
			CreateBookmarkedParagraph("_RefBookmark", 1, "Target Text"));
	}

	internal static Paragraph CreateBookmarkedParagraph(string bookmarkName, int bookmarkId, string text)
	{
		var idValue = bookmarkId.ToString(System.Globalization.CultureInfo.InvariantCulture);
		return new Paragraph(
			new BookmarkStart { Id = idValue, Name = bookmarkName },
			new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }),
			new BookmarkEnd { Id = idValue });
	}

	internal static Paragraph CreateSeqFigureParagraph(string cachedSequenceText, string trailingText, bool pageBreakBefore = false)
		=> CreateFieldParagraph("SEQ Figure", cachedSequenceText, trailingText, pageBreakBefore);
	internal static Paragraph CreatePlainParagraph(string text)
		=> new(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
}
