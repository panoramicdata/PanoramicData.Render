namespace PanoramicData.Render.ReferenceGenerator;

using System.Runtime.InteropServices;

internal static partial class FieldUpdateCorpusGenerator
{
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
