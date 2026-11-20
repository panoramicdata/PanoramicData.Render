namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml.Wordprocessing;

internal sealed class StyleDocument : IDisposable
{
	private readonly MemoryStream _stream;
	private readonly DocxDocument _document;

	public StyleDocument(Styles styles)
	{
		_stream = TestDocxBuilder.CreateDocxWithStyles(styles);
		_document = DocxDocument.Load(_stream);
	}

	public Styles? Styles => _document.StylesPart?.Styles;

	public void Dispose()
	{
		_document.Dispose();
		_stream.Dispose();
	}
}