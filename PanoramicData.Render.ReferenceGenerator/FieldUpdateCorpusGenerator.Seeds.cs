namespace PanoramicData.Render.ReferenceGenerator;

using System.Runtime.InteropServices;

internal static partial class FieldUpdateCorpusGenerator
{
	// --- Seed Creators (late-bound Word COM) ---

	private static void BuildSeed(object wordApp, string path, string title, Action<dynamic> build)
	{
		dynamic doc = ((dynamic)wordApp).Documents.Add();
		try
		{
			dynamic titlePara = doc.Paragraphs.Add();
			titlePara.Range.Text = title;
			titlePara.Range.Font.Size = 16;
			titlePara.Range.Font.Bold = 1;
			titlePara.Range.InsertParagraphAfter();

			build(doc);

			doc.SaveAs2(FileName: path, FileFormat: WdFormatXMLDocument);
		}
		finally
		{
			doc.Close(SaveChanges: WordInterop.WdDoNotSaveChanges);
			if (Marshal.IsComObject(doc))
			{
				Marshal.ReleaseComObject(doc);
			}
		}
	}

	private static dynamic AddParagraph(dynamic doc, string text, int? style = null)
	{
		dynamic paragraph = doc.Paragraphs.Add();
		paragraph.Range.Text = text;
		if (style is not null)
		{
			paragraph.Style = style.Value;
		}

		return paragraph;
	}

	private static void CreateTocSeed(object wordApp, string path)
		=> BuildSeed(wordApp, path, "Document with Table of Contents", doc =>
		{
			AddParagraph(doc, "Initial Chapter", WdStyleHeading1).Range.InsertParagraphAfter();
			AddParagraph(doc, "This is the body text of the initial chapter.").Range.InsertParagraphAfter();

			// Insert a TOC at the beginning (after the title)
			dynamic tocRange = doc.Paragraphs[2].Range;
			tocRange.Collapse(WdCollapseStart);
			doc.TablesOfContents.Add(
				Range: tocRange,
				UseHeadingStyles: true,
				UpperHeadingLevel: 1,
				LowerHeadingLevel: 3);

			doc.Fields.Update();
		});

	private static void CreateTofSeed(object wordApp, string path)
		=> BuildSeed(wordApp, path, "Document with Table of Figures", doc =>
		{
			// Word's InsertCaption creates a proper SEQ field
			dynamic captionRange = doc.Paragraphs.Add().Range;
			captionRange.InsertCaption(Label: "Figure", Title: ". Initial Diagram");

			AddParagraph(doc, "Description of the initial diagram.").Range.InsertParagraphAfter();

			dynamic tofRange = doc.Paragraphs[2].Range;
			tofRange.Collapse(WdCollapseStart);
			doc.TablesOfFigures.Add(
				Range: tofRange,
				Caption: "Figure");

			doc.Fields.Update();
			foreach (dynamic tof in doc.TablesOfFigures)
			{
				tof.Update();
			}
		});

	private static void CreatePageOfSeed(object wordApp, string path)
		=> BuildSeed(wordApp, path, "Document with Page X of Y Footer", doc =>
		{
			AddParagraph(doc, "This document tests PAGE and NUMPAGES field updates in the footer.").Range.InsertParagraphAfter();

			dynamic footer = doc.Sections[1].Footers[1]; // wdHeaderFooterPrimary = 1
			footer.Range.ParagraphFormat.Alignment = WdAlignParagraphCenter;
			footer.Range.Text = "Page ";
			AppendFooterField(footer, WdFieldPage);
			AppendFooterText(footer, " of ");
			AppendFooterField(footer, WdFieldNumPages);

			doc.Fields.Update();
			UpdateHeaderFooterFields(doc, includeHeaders: false);
		});

	private static void AppendFooterField(dynamic footer, int fieldType)
	{
		dynamic range = footer.Range;
		range.Collapse(WdCollapseEnd);
		range.Fields.Add(range, fieldType);
	}

	private static void AppendFooterText(dynamic footer, string text)
	{
		dynamic range = footer.Range;
		range.Collapse(WdCollapseEnd);
		range.InsertAfter(text);
	}
	private static void CreateCrossRefsSeed(object wordApp, string path)
		=> BuildSeed(wordApp, path, "Document with Cross-References", doc =>
		{
			dynamic headingPara = AddParagraph(doc, "Target Heading", WdStyleHeading1);
			doc.Bookmarks.Add("_RefTarget", headingPara.Range);
			headingPara.Range.InsertParagraphAfter();

			AddParagraph(doc, "Body text near the bookmarked heading.").Range.InsertParagraphAfter();
			AddParagraph(doc, "See page ").Range.InsertParagraphAfter();

			dynamic pageRefRange = doc.Paragraphs[doc.Paragraphs.Count - 1].Range;
			pageRefRange.Collapse(WdCollapseStart);
			pageRefRange.Fields.Add(pageRefRange, WdFieldPageRef, "_RefTarget");

			doc.Fields.Update();
		});

}
