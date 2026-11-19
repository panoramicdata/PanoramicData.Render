namespace PanoramicData.Render.ReferenceGenerator;

using PDFtoImage;
using System.Runtime.InteropServices;

internal static class WordInterop
{
	internal const int Dpi = 150;
	internal const int WdAlertsNone = 0;
	internal const int WdDoNotSaveChanges = 0;

	private const int WdExportFormatPdf = 17;
	private const int WdExportOptimizeForPrint = 0;
	private const int WdExportAllDocument = 0;
	private const int WdExportCreateNoBookmarks = 0;

	internal static void ExportAsPdf(object document, string outputPdfPath)
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

	internal static int ConvertPdfToPngs(string pdfPath, string stem, string outputDir)
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

	internal static void ReleaseComObject(object comObject)
	{
		if (Marshal.IsComObject(comObject))
		{
			Marshal.ReleaseComObject(comObject);
		}
	}
}