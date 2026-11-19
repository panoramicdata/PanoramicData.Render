namespace PanoramicData.Render;

using SkiaSharp;

internal static class SkiaFontStyles
{
	internal static SKFontStyle FromRenderFont(RenderFont font)
	{
		return new SKFontStyle(
			font.IsBold ? (int)SKFontStyleWeight.Bold : (int)SKFontStyleWeight.Normal,
			(int)SKFontStyleWidth.Normal,
			font.IsItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
	}
}