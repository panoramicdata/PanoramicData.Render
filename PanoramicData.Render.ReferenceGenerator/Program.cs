namespace PanoramicData.Render.ReferenceGenerator;

using Microsoft.Office.Interop.Word;
using PDFtoImage;
using System.Runtime.InteropServices;

/// <summary>
/// Generates reference PNG images from DOCX files using Word Interop (DOCX → PDF)
/// and PDFtoImage/PDFium (PDF → PNG at 150 DPI).
///
/// Requirements:
///   - Microsoft Word must be installed on the machine
///   - Windows only (COM Interop)
///
/// Usage:
///   PanoramicData.Render.ReferenceGenerator &lt;input-dir&gt; [output-dir]
///
///   input-dir   Directory containing .docx files to convert
///   output-dir  Directory for output PNGs (default: test-assets/reference/ relative to repo root)
///
/// Output naming convention:
///   {docx-stem}_page-{N}.png  (1-indexed page numbers)
/// </summary>
internal static class Program
{
	private const int Dpi = 150;

	private static int Main(string[] args)
	{
		if (args.Length < 1)
		{
			Console.Error.WriteLine("Usage: PanoramicData.Render.ReferenceGenerator <input-dir> [output-dir]");
			Console.Error.WriteLine();
			Console.Error.WriteLine("  input-dir   Directory containing .docx files to convert");
			Console.Error.WriteLine("  output-dir  Directory for output PNGs (default: test-assets/reference/)");
			return 1;
		}

		var inputDir = Path.GetFullPath(args[0]);
		if (!Directory.Exists(inputDir))
		{
			Console.Error.WriteLine($"Input directory not found: {inputDir}");
			return 1;
		}

		var outputDir = args.Length >= 2
			? Path.GetFullPath(args[1])
			: Path.GetFullPath(Path.Combine(inputDir, "..", "..", "test-assets", "reference"));

		Directory.CreateDirectory(outputDir);

		var docxFiles = Directory.GetFiles(inputDir, "*.docx", SearchOption.TopDirectoryOnly);
		if (docxFiles.Length == 0)
		{
			Console.Error.WriteLine($"No .docx files found in: {inputDir}");
			return 1;
		}

		Console.WriteLine($"Found {docxFiles.Length} DOCX file(s) in {inputDir}");
		Console.WriteLine($"Output directory: {outputDir}");
		Console.WriteLine($"DPI: {Dpi}");
		Console.WriteLine();

		Application? wordApp = null;
		try
		{
			Console.WriteLine("Starting Microsoft Word...");
			wordApp = new Application
			{
				Visible = false,
				DisplayAlerts = WdAlertLevel.wdAlertsNone
			};

			var totalPages = 0;
			foreach (var docxPath in docxFiles)
			{
				var stem = Path.GetFileNameWithoutExtension(docxPath);
				Console.Write($"  Processing {stem}.docx ... ");

				try
				{
					var pages = ProcessDocument(wordApp, docxPath, stem, outputDir);
					totalPages += pages;
					Console.WriteLine($"{pages} page(s)");
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"FAILED: {ex.Message}");
				}
			}

			Console.WriteLine();
			Console.WriteLine($"Done. Generated {totalPages} PNG(s) in {outputDir}");
			return 0;
		}
		catch (COMException ex) when (ex.HResult == unchecked((int)0x80040154))
		{
			Console.Error.WriteLine("Microsoft Word is not installed or not registered as a COM server.");
			Console.Error.WriteLine($"HRESULT: 0x{ex.HResult:X8}");
			return 2;
		}
		finally
		{
			if (wordApp is not null)
			{
				try
				{
					wordApp.Quit(SaveChanges: false);
				}
				catch
				{
					// Best-effort cleanup
				}

				Marshal.ReleaseComObject(wordApp);
			}
		}
	}

	private static int ProcessDocument(Application wordApp, string docxPath, string stem, string outputDir)
	{
		// Use a temporary PDF file for the intermediate conversion
		var tempPdf = Path.Combine(Path.GetTempPath(), $"{stem}_{Guid.NewGuid():N}.pdf");

		Document? doc = null;
		try
		{
			// Open the DOCX in Word
			doc = wordApp.Documents.Open(
				FileName: docxPath,
				ReadOnly: true,
				AddToRecentFiles: false,
				Visible: false);

			// Export to PDF using Word's built-in PDF export
			doc.ExportAsFixedFormat(
				OutputFileName: tempPdf,
				ExportFormat: WdExportFormat.wdExportFormatPDF,
				OpenAfterExport: false,
				OptimizeFor: WdExportOptimizeFor.wdExportOptimizeForPrint,
				Range: WdExportRange.wdExportAllDocument,
				IncludeDocProps: false,
				CreateBookmarks: WdExportCreateBookmarks.wdExportCreateNoBookmarks);

			// Close the document without saving
			doc.Close(SaveChanges: false);
			Marshal.ReleaseComObject(doc);
			doc = null;

			// Convert the PDF to PNGs
			return ConvertPdfToPngs(tempPdf, stem, outputDir);
		}
		finally
		{
			if (doc is not null)
			{
				try
				{
					doc.Close(SaveChanges: false);
				}
				catch
				{
					// Best-effort cleanup
				}

				Marshal.ReleaseComObject(doc);
			}

			// Clean up the temporary PDF
			try
			{
				if (File.Exists(tempPdf))
				{
					File.Delete(tempPdf);
				}
			}
			catch
			{
				// Best-effort cleanup
			}
		}
	}

	private static int ConvertPdfToPngs(string pdfPath, string stem, string outputDir)
	{
		using var pdfStream = File.OpenRead(pdfPath);
		var options = new PDFtoImage.RenderOptions(Dpi: Dpi);
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
