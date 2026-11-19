namespace PanoramicData.Render.ReferenceGenerator;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Runtime.InteropServices;

/// <summary>
/// Generates field-update corpus documents whose fields are deliberately stale.
/// <para>
/// Workflow per document:
/// 1. Create a seed DOCX via Word COM (late-bound) with valid fields (so initial values are correct)
/// 2. Re-open via OpenXML SDK, inject extra content without updating fields → stale source DOCX
/// 3. Open the stale DOCX in Word, call Fields.Update(), export PDF → PNG → reference images
/// </para>
/// Uses late-bound COM (dynamic) to avoid typed interop assembly version mismatches.
/// </summary>
internal static class FieldUpdateCorpusGenerator
{
	// Word COM constants
	private const int WdFormatXMLDocument = 12;
	private const int WdCollapseStart = 1;
	private const int WdCollapseEnd = 0;
	private const int WdAlignParagraphCenter = 1;
	private const int WdStyleHeading1 = -2;
	private const int WdStyleCaption = -35;
	private const int WdFieldPage = 33;
	private const int WdFieldNumPages = 26;
	private const int WdFieldPageRef = 37;

	private static readonly (string Name, Action<dynamic, string> CreateSeed, Action<string> InjectStaleness)[] Generators =
	[
		("field-update-toc", CreateTocSeed, InjectTocStaleness),
		("field-update-tof", CreateTofSeed, InjectTofStaleness),
		("field-update-page-of", CreatePageOfSeed, InjectPageOfStaleness),
		("field-update-cross-refs", CreateCrossRefsSeed, InjectCrossRefsStaleness),
	];

	/// <summary>
	/// Generates all field-update corpus DOCX files and renders reference PNGs.
	/// Returns the number of documents created.
	/// </summary>
	public static int GenerateAll(string docxDir, string pngDir)
	{
		Directory.CreateDirectory(docxDir);
		Directory.CreateDirectory(pngDir);

		var wordAppType = Type.GetTypeFromProgID("Word.Application");
		if (wordAppType is null)
		{
			throw new COMException("Microsoft Word COM type was not found.", unchecked((int)0x80040154));
		}

		dynamic wordApp = Activator.CreateInstance(wordAppType)
			?? throw new COMException("Failed to create Word.Application COM instance.");

		try
		{
			wordApp.Visible = false;
			wordApp.DisplayAlerts = WordInterop.WdAlertsNone;

			foreach (var (name, createSeed, injectStaleness) in Generators)
			{
				GenerateOne(wordApp, name, createSeed, injectStaleness, docxDir, pngDir);
			}

			Console.WriteLine($"Done. Created {Generators.Length} field-update corpus document(s).");
			return Generators.Length;
		}
		catch (COMException ex) when (ex.HResult == unchecked((int)0x80040154))
		{
			Console.Error.WriteLine("Microsoft Word is not installed or not registered as a COM server.");
			Console.Error.WriteLine($"HRESULT: 0x{ex.HResult:X8}");
			return -1;
		}
		finally
		{
			try { wordApp.Quit(SaveChanges: false); } catch { /* best-effort */ }
			if (Marshal.IsComObject(wordApp))
			{
				Marshal.ReleaseComObject(wordApp);
			}
		}
	}

	private static void GenerateOne(dynamic wordApp, string name, Action<dynamic, string> createSeed, Action<string> injectStaleness, string docxDir, string pngDir)
	{
		var docxPath = Path.Combine(docxDir, $"{name}.docx");
		var seedPath = Path.Combine(Path.GetTempPath(), $"{name}_seed_{Guid.NewGuid():N}.docx");

		try
		{
			Console.Write($"  Creating {name}.docx ... ");

			// Step 1: Create seed document via Word COM
			createSeed(wordApp, seedPath);

			// Step 2: Copy seed to final location, then inject extra content via OpenXML SDK
			File.Copy(seedPath, docxPath, overwrite: true);
			injectStaleness(docxPath);

			// Step 3: Open stale doc in Word, update fields, export reference PNGs
			var pages = RenderWithFieldUpdate(wordApp, docxPath, name, pngDir);
			Console.WriteLine($"{pages} page(s)");
		}
		finally
		{
			try { File.Delete(seedPath); } catch { /* best-effort */ }
		}
	}
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

	// --- Staleness Injectors (OpenXML SDK) ---

	private static void ModifyBody(string docxPath, Action<Body> modify)
	{
		using var doc = WordprocessingDocument.Open(docxPath, isEditable: true);
		modify(doc.MainDocumentPart!.Document!.Body!);
	}

	private static Paragraph TextParagraph(string text, OpenXmlElement? properties = null)
		=> properties is null
			? new Paragraph(new Run(new Text(text)))
			: new Paragraph(properties, new Run(new Text(text)));

	private static Paragraph PageBreakParagraph(string text)
		=> TextParagraph(text, new ParagraphProperties(new PageBreakBefore()));

	private static Paragraph StyledParagraph(string styleId, string text)
		=> TextParagraph(text, new ParagraphProperties(new ParagraphStyleId { Val = styleId }));

	private static void InjectTocStaleness(string docxPath)
		=> ModifyBody(docxPath, body =>
		{
			// Add 10+ headings across multiple pages to make the TOC very stale
			for (var i = 2; i <= 12; i++)
			{
				body.AppendChild(new Paragraph(
					new ParagraphProperties(
						new ParagraphStyleId { Val = "Heading1" },
						new PageBreakBefore()),
					new Run(new Text($"Chapter {i}: Generated Heading"))));
				body.AppendChild(StyledParagraph("Heading2", $"{i}.1 Sub-section"));
				body.AppendChild(TextParagraph($"Body text for chapter {i}. This provides content to ensure the chapter occupies space on the page and affects page numbering."));
				body.AppendChild(StyledParagraph("Heading3", $"{i}.1.1 Detail"));
				body.AppendChild(TextParagraph("Detailed content under the tertiary heading."));
			}
		});

	private static void InjectTofStaleness(string docxPath)
		=> ModifyBody(docxPath, body =>
		{
			// Word's TOF requires SEQ fields in caption paragraphs, not just Caption-styled text.
			for (var i = 2; i <= 7; i++)
			{
				body.AppendChild(PageBreakParagraph($"Content before figure {i}."));
				body.AppendChild(new Paragraph(
					new ParagraphProperties(
						new ParagraphStyleId { Val = "Caption" }),
					new Run(new Text("Figure ") { Space = SpaceProcessingModeValues.Preserve }),
					new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
					new Run(new FieldCode { Space = SpaceProcessingModeValues.Preserve, Text = " SEQ Figure \\* ARABIC " }),
					new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
					new Run(new Text(i.ToString())),
					new Run(new FieldChar { FieldCharType = FieldCharValues.End }),
					new Run(new Text($". Generated Diagram {i}"))));
				body.AppendChild(TextParagraph($"Description of generated diagram {i}."));
			}
		});

	private static void InjectPageOfStaleness(string docxPath)
		=> ModifyBody(docxPath, body =>
		{
			// Enough content to span 5+ pages while the footer still says "Page 1 of 1"
			for (var i = 1; i <= 6; i++)
			{
				body.AppendChild(PageBreakParagraph($"Page {i + 1} content. This is additional material that pushes the document well beyond the original single page."));
				for (var j = 1; j <= 5; j++)
				{
					body.AppendChild(TextParagraph($"Additional paragraph {j} on page {i + 1}. The quick brown fox jumps over the lazy dog. Lorem ipsum dolor sit amet, consectetur adipiscing elit."));
				}
			}
		});

	private static void InjectCrossRefsStaleness(string docxPath)
		=> ModifyBody(docxPath, body =>
		{
			var bookmarkPara = body.Descendants<BookmarkStart>()
				.FirstOrDefault(b => b.Name?.Value == "_RefTarget")?.Parent;

			if (bookmarkPara is null)
			{
				return;
			}

			// Filler before the bookmarked paragraph pushes it to page 5+, making the PAGEREF value stale
			for (var i = 1; i <= 5; i++)
			{
				body.InsertBefore(PageBreakParagraph($"Filler content page {i}. This text is injected to push the bookmarked heading to a later page, making the PAGEREF field stale."), bookmarkPara);
				for (var j = 1; j <= 4; j++)
				{
					body.InsertBefore(TextParagraph($"Extra paragraph {j} on filler page {i}. The PAGEREF should resolve to the actual page number of the bookmark target."), bookmarkPara);
				}
			}
		});
	// --- Reference Renderer (late-bound Word COM with Fields.Update) ---

	private static void UpdateAllFields(dynamic doc)
	{
		// Force Word to repaginate before updating fields
		doc.Repaginate();
		UpdateTocAndTof(doc);

		// Update all other fields in the main body, then in headers/footers
		doc.Fields.Update();
		UpdateHeaderFooterFields(doc, includeHeaders: true);

		// Second repaginate + field update pass to ensure page numbers are correct
		// (TOC expansion may have shifted content to new pages)
		doc.Repaginate();
		UpdateTocAndTof(doc);
		doc.Fields.Update();
		UpdateHeaderFooterFields(doc, includeHeaders: false);
	}

	// doc.Fields.Update() alone does NOT rebuild TOC/TOF entries; the dedicated Update() calls are needed.
	private static void UpdateTocAndTof(dynamic doc)
	{
		foreach (dynamic toc in doc.TablesOfContents)
		{
			toc.Update();
		}

		foreach (dynamic tof in doc.TablesOfFigures)
		{
			tof.Update();
		}
	}

	private static void UpdateHeaderFooterFields(dynamic doc, bool includeHeaders)
	{
		foreach (dynamic section in doc.Sections)
		{
			if (includeHeaders)
			{
				foreach (dynamic header in section.Headers)
				{
					header.Range.Fields.Update();
				}
			}

			foreach (dynamic footer in section.Footers)
			{
				footer.Range.Fields.Update();
			}
		}
	}

	private static int RenderWithFieldUpdate(dynamic wordApp, string docxPath, string stem, string pngDir)
	{
		var tempPdf = Path.Combine(Path.GetTempPath(), $"{stem}_{Guid.NewGuid():N}.pdf");
		dynamic? doc = null;

		try
		{
			// Open the stale document (read-write so we can update fields)
			doc = wordApp.Documents.Open(
				FileName: docxPath,
				ReadOnly: false,
				AddToRecentFiles: false,
				Visible: false);

			UpdateAllFields(doc);
			WordInterop.ExportAsPdf((object)doc, tempPdf);

			// Close WITHOUT saving — we want to keep the stale DOCX intact
			doc.Close(SaveChanges: WordInterop.WdDoNotSaveChanges);
			if (Marshal.IsComObject(doc))
			{
				Marshal.ReleaseComObject(doc);
			}
			doc = null;

			// Convert PDF → PNGs
			return WordInterop.ConvertPdfToPngs(tempPdf, stem, pngDir);
		}
		finally
		{
			if (doc is not null)
			{
				try { doc.Close(SaveChanges: WordInterop.WdDoNotSaveChanges); } catch { /* best-effort */ }
				if (Marshal.IsComObject(doc))
				{
					Marshal.ReleaseComObject(doc);
				}
			}

			try
			{
				if (File.Exists(tempPdf))
				{
					File.Delete(tempPdf);
				}
			}
			catch
			{
				// Best-effort cleanup.
			}
		}
	}
}
