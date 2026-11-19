namespace PanoramicData.Render;

internal sealed partial class SvgRenderTarget
{
	private void AppendTextStyle(RenderFont font, (string ColorHex, float? Opacity) fill)
	{
		_content.Append($"font-family=\"{Escape(font.Family)}\" font-size=\"{Format(font.SizePoints)}pt\"");
		if (font.IsBold)
		{
			_content.Append(" font-weight=\"bold\"");
		}

		if (font.IsItalic)
		{
			_content.Append(" font-style=\"italic\"");
		}

		_content.Append($" fill=\"{fill.ColorHex}\"");
		AppendOpacity("fill", fill.Opacity);
	}

	private void AppendOpacity(string attribute, float? opacity)
	{
		if (opacity is not null)
		{
			_content.Append($" {attribute}-opacity=\"{Format(opacity.Value)}\"");
		}
	}
}