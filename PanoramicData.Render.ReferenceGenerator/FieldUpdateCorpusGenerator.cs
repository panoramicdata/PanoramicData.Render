namespace PanoramicData.Render.ReferenceGenerator;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PDFtoImage;
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
	private const int Dpi = 150;

	// Word COM constants
	private const int WdAlertsNone = 0;
	private const int WdFormatXMLDocument = 12;
	private const int WdExportFormatPdf = 17;
	private const int WdExportOptimizeForPrint = 0;
	private const int WdExportAllDocument = 0;
	private const int WdExportCreateNoBookmarks = 0;
	private const int WdDoNotSaveChanges = 0;
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
			wordApp.DisplayAlerts = WdAlertsNone;

			foreach (var (name, createSeed, injectStaleness) in Generators)
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

	// --- Seed Creators (late-bound Word COM) ---

	private static void CreateTocSeed(dynamic wordApp, string path)
	{
		dynamic doc = wordApp.Documents.Add();
		try
		{
			// Add a title
			dynamic titlePara = doc.Paragraphs.Add();
			titlePara.Range.Text = "Document with Table of Contents";
			titlePara.Range.Font.Size = 16;
			titlePara.Range.Font.Bold = 1;
			titlePara.Range.InsertParagraphAfter();

			// Add a single heading so the initial TOC is valid
			dynamic headingPara = doc.Paragraphs.Add();
			headingPara.Range.Text = "Initial Chapter";
			headingPara.Style = WdStyleHeading1;
			headingPara.Range.InsertParagraphAfter();

			// Add some body text
			dynamic bodyPara = doc.Paragraphs.Add();
			bodyPara.Range.Text = "This is the body text of the initial chapter.";
			bodyPara.Range.InsertParagraphAfter();

			// Insert a TOC at the beginning (after the title)
			dynamic tocRange = doc.Paragraphs[2].Range;
			tocRange.Collapse(WdCollapseStart);
			doc.TablesOfContents.Add(
				Range: tocRange,
				UseHeadingStyles: true,
				UpperHeadingLevel: 1,
				LowerHeadingLevel: 3);

			// Update fields so the TOC is initially correct
			doc.Fields.Update();

			doc.SaveAs2(FileName: path, FileFormat: WdFormatXMLDocument);
		}
		finally
		{
			doc.Close(SaveChanges: WdDoNotSaveChanges);
			if (Marshal.IsComObject(doc)) Marshal.ReleaseComObject(doc);
		}
	}

	private static void CreateTofSeed(dynamic wordApp, string path)
	{
		dynamic doc = wordApp.Documents.Add();
		try
		{
			// Add a title
			dynamic titlePara = doc.Paragraphs.Add();
			titlePara.Range.Text = "Document with Table of Figures";
			titlePara.Range.Font.Size = 16;
			titlePara.Range.Font.Bold = 1;
			titlePara.Range.InsertParagraphAfter();

			// Add a figure caption using Word's built-in InsertCaption (creates proper SEQ field)
			dynamic captionRange = doc.Paragraphs.Add().Range;
			captionRange.InsertCaption(Label: "Figure", Title: ". Initial Diagram");

			// Add a body paragraph
			dynamic bodyPara = doc.Paragraphs.Add();
			bodyPara.Range.Text = "Description of the initial diagram.";
			bodyPara.Range.InsertParagraphAfter();

			// Insert a Table of Figures at paragraph 2
			dynamic tofRange = doc.Paragraphs[2].Range;
			tofRange.Collapse(WdCollapseStart);
			doc.TablesOfFigures.Add(
				Range: tofRange,
				Caption: "Figure");

			// Update fields
			doc.Fields.Update();
			foreach (dynamic tof in doc.TablesOfFigures)
			{
				tof.Update();
			}

			doc.SaveAs2(FileName: path, FileFormat: WdFormatXMLDocument);
		}
		finally
		{
			doc.Close(SaveChanges: WdDoNotSaveChanges);
			if (Marshal.IsComObject(doc)) Marshal.ReleaseComObject(doc);
		}
	}

	private static void CreatePageOfSeed(dynamic wordApp, string path)
	{
		dynamic doc = wordApp.Documents.Add();
		try
		{
			// Add a title
			dynamic titlePara = doc.Paragraphs.Add();
			titlePara.Range.Text = "Document with Page X of Y Footer";
			titlePara.Range.Font.Size = 16;
			titlePara.Range.Font.Bold = 1;
			titlePara.Range.InsertParagraphAfter();

			// Add a body paragraph
			dynamic bodyPara = doc.Paragraphs.Add();
			bodyPara.Range.Text = "This document tests PAGE and NUMPAGES field updates in the footer.";
			bodyPara.Range.InsertParagraphAfter();

			// Add "Page X of Y" footer using fields
			dynamic section = doc.Sections[1];
			dynamic footer = section.Footers[1]; // wdHeaderFooterPrimary = 1
			footer.Range.ParagraphFormat.Alignment = WdAlignParagraphCenter;
			footer.Range.Text = "Page ";
			dynamic footerRange = footer.Range;
			footerRange.Collapse(WdCollapseEnd);
			footerRange.Fields.Add(footerRange, WdFieldPage);
			footerRange = footer.Range;
			footerRange.Collapse(WdCollapseEnd);
			footerRange.InsertAfter(" of ");
			footerRange = footer.Range;
			footerRange.Collapse(WdCollapseEnd);
			footerRange.Fields.Add(footerRange, WdFieldNumPages);

			// Update fields
			doc.Fields.Update();
			foreach (dynamic sect in doc.Sections)
			{
				foreach (dynamic f in sect.Footers)
				{
					f.Range.Fields.Update();
				}
			}

			doc.SaveAs2(FileName: path, FileFormat: WdFormatXMLDocument);
		}
		finally
		{
			doc.Close(SaveChanges: WdDoNotSaveChanges);
			if (Marshal.IsComObject(doc)) Marshal.ReleaseComObject(doc);
		}
	}

	private static void CreateCrossRefsSeed(dynamic wordApp, string path)
	{
		dynamic doc = wordApp.Documents.Add();
		try
		{
			// Add a title
			dynamic titlePara = doc.Paragraphs.Add();
			titlePara.Range.Text = "Document with Cross-References";
			titlePara.Range.Font.Size = 16;
			titlePara.Range.Font.Bold = 1;
			titlePara.Range.InsertParagraphAfter();

			// Add a bookmarked heading
			dynamic headingPara = doc.Paragraphs.Add();
			headingPara.Range.Text = "Target Heading";
			headingPara.Style = WdStyleHeading1;
			doc.Bookmarks.Add("_RefTarget", headingPara.Range);
			headingPara.Range.InsertParagraphAfter();

			// Add body text
			dynamic bodyPara = doc.Paragraphs.Add();
			bodyPara.Range.Text = "Body text near the bookmarked heading.";
			bodyPara.Range.InsertParagraphAfter();

			// Add cross-reference paragraph at the end
			dynamic refPara = doc.Paragraphs.Add();
			refPara.Range.Text = "See page ";
			refPara.Range.InsertParagraphAfter();

			// Insert PAGEREF field
			dynamic pageRefRange = doc.Paragraphs[doc.Paragraphs.Count - 1].Range;
			pageRefRange.Collapse(WdCollapseStart);
			pageRefRange.Fields.Add(pageRefRange, WdFieldPageRef, "_RefTarget");

			// Update fields
			doc.Fields.Update();

			doc.SaveAs2(FileName: path, FileFormat: WdFormatXMLDocument);
		}
		finally
		{
			doc.Close(SaveChanges: WdDoNotSaveChanges);
			if (Marshal.IsComObject(doc)) Marshal.ReleaseComObject(doc);
		}
	}

	// --- Staleness Injectors (OpenXML SDK) ---

	private static void InjectTocStaleness(string docxPath)
	{
		using var doc = WordprocessingDocument.Open(docxPath, isEditable: true);
		var body = doc.MainDocumentPart!.Document!.Body!;

		// Add 10+ headings across multiple pages to make the TOC very stale
		for (var i = 2; i <= 12; i++)
		{
			// Page break before each chapter
			body.AppendChild(new Paragraph(
				new ParagraphProperties(
					new ParagraphStyleId { Val = "Heading1" },
					new PageBreakBefore()),
				new Run(new Text($"Chapter {i}: Generated Heading"))));

			// Sub-heading
			body.AppendChild(new Paragraph(
				new ParagraphProperties(
					new ParagraphStyleId { Val = "Heading2" }),
				new Run(new Text($"{i}.1 Sub-section"))));

			// Body text to fill the page
			body.AppendChild(new Paragraph(
				new Run(new Text($"Body text for chapter {i}. This provides content to ensure the chapter occupies space on the page and affects page numbering."))));

			// Another sub-heading
			body.AppendChild(new Paragraph(
				new ParagraphProperties(
					new ParagraphStyleId { Val = "Heading3" }),
				new Run(new Text($"{i}.1.1 Detail"))));

			body.AppendChild(new Paragraph(
				new Run(new Text("Detailed content under the tertiary heading."))));
		}
	}

	private static void InjectTofStaleness(string docxPath)
	{
		using var doc = WordprocessingDocument.Open(docxPath, isEditable: true);
		var body = doc.MainDocumentPart!.Document!.Body!;

		// Add 5+ caption paragraphs with proper SEQ fields after existing content.
		// Word's TOF requires SEQ fields in caption paragraphs, not just Caption-styled text.
		for (var i = 2; i <= 7; i++)
		{
			// Page break
			body.AppendChild(new Paragraph(
				new ParagraphProperties(new PageBreakBefore()),
				new Run(new Text($"Content before figure {i}."))));

			// Caption-style paragraph with SEQ field (matching Word's caption format)
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

			body.AppendChild(new Paragraph(
				new Run(new Text($"Description of generated diagram {i}."))));
		}
	}

	private static void InjectPageOfStaleness(string docxPath)
	{
		using var doc = WordprocessingDocument.Open(docxPath, isEditable: true);
		var body = doc.MainDocumentPart!.Document!.Body!;

		// Add enough content to span 5+ pages — the footer still says "Page 1 of 1"
		for (var i = 1; i <= 6; i++)
		{
			body.AppendChild(new Paragraph(
				new ParagraphProperties(new PageBreakBefore()),
				new Run(new Text($"Page {i + 1} content. This is additional material that pushes the document well beyond the original single page."))));

			// Add several paragraphs of filler content per page
			for (var j = 1; j <= 5; j++)
			{
				body.AppendChild(new Paragraph(
					new Run(new Text($"Additional paragraph {j} on page {i + 1}. The quick brown fox jumps over the lazy dog. Lorem ipsum dolor sit amet, consectetur adipiscing elit."))));
			}
		}
	}

	private static void InjectCrossRefsStaleness(string docxPath)
	{
		using var doc = WordprocessingDocument.Open(docxPath, isEditable: true);
		var body = doc.MainDocumentPart!.Document!.Body!;

		// Find the bookmarked heading and push it far down by inserting content before it.
		// We'll insert many page-break paragraphs before the last few paragraphs.
		// Strategy: add lots of content at the top (after title, before heading) to push
		// the bookmarked heading to page 5+, making the PAGEREF field value very stale.

		// Find the paragraph that contains the bookmark
		var bookmarkPara = body.Descendants<BookmarkStart>()
			.FirstOrDefault(b => b.Name?.Value == "_RefTarget")?.Parent;

		if (bookmarkPara is not null)
		{
			// Insert 5 pages of filler BEFORE the bookmarked paragraph
			for (var i = 1; i <= 5; i++)
			{
				var filler = new Paragraph(
					new ParagraphProperties(new PageBreakBefore()),
					new Run(new Text($"Filler content page {i}. This text is injected to push the bookmarked heading to a later page, making the PAGEREF field stale.")));
				body.InsertBefore(filler, bookmarkPara);

				for (var j = 1; j <= 4; j++)
				{
					var extra = new Paragraph(
						new Run(new Text($"Extra paragraph {j} on filler page {i}. The PAGEREF should resolve to the actual page number of the bookmark target.")));
					body.InsertBefore(extra, bookmarkPara);
				}
			}
		}
	}

	// --- Reference Renderer (late-bound Word COM with Fields.Update) ---

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

			// Force Word to repaginate before updating fields
			doc.Repaginate();

			// Explicitly update TOC and TOF objects — doc.Fields.Update() alone
			// does NOT rebuild TOC/TOF entries; we need the dedicated Update() calls.
			foreach (dynamic toc in doc.TablesOfContents)
			{
				toc.Update();
			}

			foreach (dynamic tof in doc.TablesOfFigures)
			{
				tof.Update();
			}

			// Update all other fields in the main body
			doc.Fields.Update();

			// Also update fields in headers/footers
			foreach (dynamic section in doc.Sections)
			{
				foreach (dynamic header in section.Headers)
				{
					header.Range.Fields.Update();
				}

				foreach (dynamic footer in section.Footers)
				{
					footer.Range.Fields.Update();
				}
			}

			// Second repaginate + field update pass to ensure page numbers are correct
			// (TOC expansion may have shifted content to new pages)
			doc.Repaginate();
			foreach (dynamic toc in doc.TablesOfContents)
			{
				toc.Update();
			}

			foreach (dynamic tof in doc.TablesOfFigures)
			{
				tof.Update();
			}

			doc.Fields.Update();
			foreach (dynamic section in doc.Sections)
			{
				foreach (dynamic footer in section.Footers)
				{
					footer.Range.Fields.Update();
				}
			}

			// Export to PDF
			doc.ExportAsFixedFormat(
				OutputFileName: tempPdf,
				ExportFormat: WdExportFormatPdf,
				OpenAfterExport: false,
				OptimizeFor: WdExportOptimizeForPrint,
				Range: WdExportAllDocument,
				IncludeDocProps: false,
				CreateBookmarks: WdExportCreateNoBookmarks);

			// Close WITHOUT saving — we want to keep the stale DOCX intact
			doc.Close(SaveChanges: WdDoNotSaveChanges);
			if (Marshal.IsComObject(doc)) Marshal.ReleaseComObject(doc);
			doc = null;

			// Convert PDF → PNGs
			return ConvertPdfToPngs(tempPdf, stem, pngDir);
		}
		finally
		{
			if (doc is not null)
			{
				try { doc.Close(SaveChanges: WdDoNotSaveChanges); } catch { /* best-effort */ }
				if (Marshal.IsComObject(doc)) Marshal.ReleaseComObject(doc);
			}

			try { if (File.Exists(tempPdf)) File.Delete(tempPdf); } catch { /* best-effort */ }
		}
	}

	private static int ConvertPdfToPngs(string pdfPath, string stem, string outputDir)
	{
		using var pdfStream = File.OpenRead(pdfPath);
		var options = new RenderOptions(Dpi: Dpi);
		var pageIndex = 0;

		foreach (var bitmap in Conversion.ToImages(pdfStream, options: options))
		{
			using (bitmap)
			{
				pageIndex++;
				var outputPath = Path.Combine(outputDir, $"{stem}_page-{pageIndex}.png");
				using var outputStream = File.Create(outputPath);
				bitmap.Encode(outputStream, SkiaSharp.SKEncodedImageFormat.Png, quality: 100);
			}
		}

		return pageIndex;
	}
}
