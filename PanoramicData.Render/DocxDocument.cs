namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Represents a loaded DOCX document, providing access to its constituent OpenXML parts.
/// </summary>
internal sealed class DocxDocument : IDisposable
{
	private readonly WordprocessingDocument _wordDocument;

	private DocxDocument(WordprocessingDocument wordDocument)
	{
		_wordDocument = wordDocument;

		var mainPart = wordDocument.MainDocumentPart
			?? throw new InvalidOperationException("The DOCX file has no main document part.");

		DocumentBody = mainPart.Document.Body
			?? throw new InvalidOperationException("The DOCX file has no document body.");

		StylesPart = mainPart.StyleDefinitionsPart;
		ThemePart = mainPart.ThemePart;
		NumberingPart = mainPart.NumberingDefinitionsPart;
		SettingsPart = mainPart.DocumentSettingsPart;
	}

	/// <summary>
	/// Gets the document body element.
	/// </summary>
	public Body DocumentBody { get; }

	/// <summary>
	/// Gets the style definitions part, or <see langword="null"/> if not present.
	/// </summary>
	public StyleDefinitionsPart? StylesPart { get; }

	/// <summary>
	/// Gets the theme part, or <see langword="null"/> if not present.
	/// </summary>
	public ThemePart? ThemePart { get; }

	/// <summary>
	/// Gets the numbering definitions part, or <see langword="null"/> if not present.
	/// </summary>
	public NumberingDefinitionsPart? NumberingPart { get; }

	/// <summary>
	/// Gets the document settings part, or <see langword="null"/> if not present.
	/// </summary>
	public DocumentSettingsPart? SettingsPart { get; }

	/// <summary>
	/// Loads a DOCX document from a stream.
	/// </summary>
	/// <param name="stream">A readable, seekable stream containing DOCX data.</param>
	/// <returns>A <see cref="DocxDocument"/> providing access to the document parts.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
	public static DocxDocument Load(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);
		var wordDoc = WordprocessingDocument.Open(stream, false);
		try
		{
			return new DocxDocument(wordDoc);
		}
		catch
		{
			wordDoc.Dispose();
			throw;
		}
	}

	/// <inheritdoc />
	public void Dispose() => _wordDocument.Dispose();
}
