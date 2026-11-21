namespace PanoramicData.Render.ReferenceGenerator;

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
internal static partial class FieldUpdateCorpusGenerator
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
}
