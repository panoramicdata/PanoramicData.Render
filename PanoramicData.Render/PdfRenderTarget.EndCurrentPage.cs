namespace PanoramicData.Render;

using SkiaSharp;

internal sealed partial class PdfRenderTarget : IRenderTarget, IDisposable
{
	private void EndCurrentPage()
	{
		if (!_hasOpenPage)
		{
			return;
		}

		while (_clipStack.Count > 0)
		{
			GetCanvas().Restore();
			_clipStack.Pop();
		}

		_document.EndPage();
		_canvas = null;
		_hasOpenPage = false;
	}

	private SKCanvas GetCanvas()
	{
		if (_canvas is null)
		{
			throw new InvalidOperationException("No active PDF page is open.");
		}

		return _canvas;
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}
}
