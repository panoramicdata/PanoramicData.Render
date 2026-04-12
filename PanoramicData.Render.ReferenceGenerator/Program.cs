namespace PanoramicData.Render.ReferenceGenerator;

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
///   PanoramicData.Render.ReferenceGenerator generate-corpus &lt;output-dir&gt;
///     Creates the test DOCX corpus programmatically using OpenXML SDK.
///
///   PanoramicData.Render.ReferenceGenerator generate-field-update-corpus &lt;docx-dir&gt; &lt;png-dir&gt;
///     Creates field-update corpus DOCX files (with stale fields) and renders
///     field-updated reference PNGs via Word Interop.
///
///   PanoramicData.Render.ReferenceGenerator render &lt;input-dir&gt; [output-dir]
///     Renders DOCX files to reference PNGs via Word → PDF → PNG pipeline.
///
///   PanoramicData.Render.ReferenceGenerator all &lt;docx-dir&gt; &lt;png-dir&gt;
///     Creates corpus then renders all in one step.
///
/// Output naming convention:
///   {docx-stem}_page-{N}.png  (1-indexed page numbers)
/// </summary>
internal static class Program
{
	private const int Dpi = 150;
	private const int WdAlertsNone = 0;
	private const int WdExportFormatPdf = 17;
	private const int WdExportOptimizeForPrint = 0;
	private const int WdExportAllDocument = 0;
	private const int WdExportCreateNoBookmarks = 0;

	private static int Main(string[] args)
	{
		if (args.Length < 1)
		{
			PrintUsage();
			return 1;
		}

		return args[0].ToLowerInvariant() switch
		{
			"generate-corpus" => GenerateCorpus(args),
			"generate-field-update-corpus" => GenerateFieldUpdateCorpus(args),
			"render" => Render(args),
			"all" => All(args),
			_ => PrintUsage()
		};
	}

	private static int PrintUsage()
	{
		Console.Error.WriteLine("Usage:");
		Console.Error.WriteLine("  PanoramicData.Render.ReferenceGenerator generate-corpus <output-dir>");
		Console.Error.WriteLine("  PanoramicData.Render.ReferenceGenerator generate-field-update-corpus <docx-dir> <png-dir>");
		Console.Error.WriteLine("  PanoramicData.Render.ReferenceGenerator render <input-dir> [output-dir]");
		Console.Error.WriteLine("  PanoramicData.Render.ReferenceGenerator all <docx-dir> <png-dir>");
		return 1;
	}

	private static int GenerateCorpus(string[] args)
	{
		if (args.Length < 2)
		{
			Console.Error.WriteLine("Usage: generate-corpus <output-dir>");
			return 1;
		}

		var outputDir = Path.GetFullPath(args[1]);
		Directory.CreateDirectory(outputDir);

		Console.WriteLine($"Generating test DOCX corpus in {outputDir}");
		var count = TestCorpusGenerator.GenerateAll(outputDir);
		Console.WriteLine($"Done. Created {count} DOCX file(s).");
		return 0;
	}

	private static int GenerateFieldUpdateCorpus(string[] args)
	{
		if (args.Length < 3)
		{
			Console.Error.WriteLine("Usage: generate-field-update-corpus <docx-dir> <png-dir>");
			return 1;
		}

		var docxDir = Path.GetFullPath(args[1]);
		var pngDir = Path.GetFullPath(args[2]);

		Console.WriteLine($"Generating field-update corpus DOCX files in {docxDir}");
		Console.WriteLine($"Reference PNGs in {pngDir}");
		Console.WriteLine();

		var result = FieldUpdateCorpusGenerator.GenerateAll(docxDir, pngDir);
		return result < 0 ? 2 : 0;
	}

	private static int Render(string[] args)
	{
		if (args.Length < 2)
		{
			Console.Error.WriteLine("Usage: render <input-dir> [output-dir]");
			return 1;
		}

		var inputDir = Path.GetFullPath(args[1]);
		if (!Directory.Exists(inputDir))
		{
			Console.Error.WriteLine($"Input directory not found: {inputDir}");
			return 1;
		}

		var outputDir = args.Length >= 3
			? Path.GetFullPath(args[2])
			: Path.Combine(Path.GetDirectoryName(inputDir)!, "reference");

		return RenderDocxFiles(inputDir, outputDir);
	}

	private static int All(string[] args)
	{
		if (args.Length < 3)
		{
			Console.Error.WriteLine("Usage: all <docx-dir> <png-dir>");
			return 1;
		}

		var docxDir = Path.GetFullPath(args[1]);
		var pngDir = Path.GetFullPath(args[2]);

		Directory.CreateDirectory(docxDir);
		Console.WriteLine($"Generating test DOCX corpus in {docxDir}");
		var count = TestCorpusGenerator.GenerateAll(docxDir);
		Console.WriteLine($"Created {count} DOCX file(s).");
		Console.WriteLine();

		return RenderDocxFiles(docxDir, pngDir);
	}

	private static int RenderDocxFiles(string inputDir, string outputDir)
	{
		Directory.CreateDirectory(outputDir);

		var inputFiles = Directory.GetFiles(inputDir, "*.docx", SearchOption.TopDirectoryOnly)
			.Concat(Directory.GetFiles(inputDir, "*.dotx", SearchOption.TopDirectoryOnly))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		if (inputFiles.Length == 0)
		{
			Console.Error.WriteLine($"No .docx or .dotx files found in: {inputDir}");
			return 1;
		}

		Console.WriteLine($"Found {inputFiles.Length} DOCX/DOTX file(s) in {inputDir}");
		Console.WriteLine($"Output directory: {outputDir}");
		Console.WriteLine($"DPI: {Dpi}");
		Console.WriteLine();

		object? wordApp = null;
		try
		{
			Console.WriteLine("Starting Microsoft Word...");
			wordApp = CreateWordApplication();

			var totalPages = 0;
			foreach (var docxPath in inputFiles.OrderBy(f => f))
			{
				var stem = Path.GetFileNameWithoutExtension(docxPath);
				var extension = Path.GetExtension(docxPath);
				Console.Write($"  Processing {stem}{extension} ... ");

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
				QuitWordApplication(wordApp);
			}
		}
	}

	private static int ProcessDocument(object wordApp, string docxPath, string stem, string outputDir)
	{
		// Use a temporary PDF file for the intermediate conversion
		var tempPdf = Path.Combine(Path.GetTempPath(), $"{stem}_{Guid.NewGuid():N}.pdf");

		object? doc = null;
		try
		{
			doc = OpenDocument(wordApp, docxPath);
			ExportDocumentAsPdf(doc, tempPdf);
			CloseDocument(doc);
			doc = null;

			// Convert the PDF to PNGs
			return ConvertPdfToPngs(tempPdf, stem, outputDir);
		}
		finally
		{
			if (doc is not null)
			{
				CloseDocument(doc);
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

	private static object CreateWordApplication()
	{
		var wordApplicationType = Type.GetTypeFromProgID("Word.Application");
		if (wordApplicationType is null)
		{
			throw new COMException("Microsoft Word COM type was not found.", unchecked((int)0x80040154));
		}

		dynamic wordApp = Activator.CreateInstance(wordApplicationType)
			?? throw new COMException("Failed to create Word.Application COM instance.");
		wordApp.Visible = false;
		wordApp.DisplayAlerts = WdAlertsNone;
		return wordApp;
	}

	private static object OpenDocument(object wordApp, string docxPath)
	{
		dynamic app = wordApp;
		return app.Documents.Open(
			FileName: docxPath,
			ReadOnly: true,
			AddToRecentFiles: false,
			Visible: false);
	}

	private static void ExportDocumentAsPdf(object document, string outputPdfPath)
	{
		dynamic doc = document;
		doc.ExportAsFixedFormat(
			OutputFileName: outputPdfPath,
			ExportFormat: WdExportFormatPdf,
			OpenAfterExport: false,
			OptimizeFor: WdExportOptimizeForPrint,
			Range: WdExportAllDocument,
			IncludeDocProps: false,
			CreateBookmarks: WdExportCreateNoBookmarks);
	}

	private static void CloseDocument(object document)
	{
		try
		{
			dynamic doc = document;
			doc.Close(SaveChanges: false);
		}
		catch
		{
			// Best-effort cleanup
		}

		ReleaseComObject(document);
	}

	private static void QuitWordApplication(object wordApp)
	{
		try
		{
			dynamic app = wordApp;
			app.Quit(SaveChanges: false);
		}
		catch
		{
			// Best-effort cleanup
		}

		ReleaseComObject(wordApp);
	}

	private static void ReleaseComObject(object comObject)
	{
		if (Marshal.IsComObject(comObject))
		{
			Marshal.ReleaseComObject(comObject);
		}
	}
}
