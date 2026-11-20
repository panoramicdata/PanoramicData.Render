namespace PanoramicData.Render.Test;

using A = DocumentFormat.OpenXml.Drawing;
using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Xunit;
using RenderTabStop = PanoramicData.Render.TabStop;

public sealed partial class RenderCommandEmitterTests
{
	private sealed class FakeRenderTarget : IRenderTarget
	{
		public List<DrawTextCall> DrawTextCalls { get; } = [];
		public List<DrawRectCall> DrawRectCalls { get; } = [];
		public List<HyperlinkCall> HyperlinkCalls { get; } = [];
		public List<NamedDestinationCall> NamedDestinationCalls { get; } = [];
		public List<RotatedTextCall> RotatedTextCalls { get; } = [];
		public List<RotatedImageCall> RotatedImageCalls { get; } = [];
		public List<DrawLineCall> DrawLineCalls { get; } = [];
		public List<string> CallOrder { get; } = [];

		public void DrawText(string text, float baselineXTwips, float baselineYTwips, RenderFont font, RenderBrush brush)
		{
			DrawTextCalls.Add(new DrawTextCall(text, baselineXTwips, baselineYTwips, font, brush));
			CallOrder.Add($"DrawText:{text}");
		}

		public void DrawLine(RenderPoint from, RenderPoint to, RenderStroke stroke)
		{
			DrawLineCalls.Add(new DrawLineCall(from, to, stroke));
		}

		public void DrawRect(RenderRect rect, RenderBrush? fill, RenderStroke? stroke)
		{
			DrawRectCalls.Add(new DrawRectCall(rect, fill, stroke));
		}

		public void DrawImage(ImageData image, RenderRect rect)
		{
			// Not needed by these tests.
		}

		public void DrawPath(string pathData, RenderBrush? fill, RenderStroke? stroke)
		{
			// Not needed by these tests.
		}

		public void PushClip(RenderRect clipRect)
		{
			// Not needed by these tests.
		}

		public void PopClip()
		{
			// Not needed by these tests.
		}

		public void SetHyperlink(RenderRect rect, string uri)
		{
			HyperlinkCalls.Add(new HyperlinkCall(rect, uri));
		}

		public void SetNamedDestination(string name, float xTwips, float yTwips)
		{
			NamedDestinationCalls.Add(new NamedDestinationCall(name, xTwips, yTwips));
		}

		public void DrawRotatedText(string text, float centerXTwips, float centerYTwips, float rotationDegrees, RenderFont font, RenderBrush brush)
		{
			RotatedTextCalls.Add(new RotatedTextCall(text, centerXTwips, centerYTwips, rotationDegrees, font, brush));
			CallOrder.Add($"DrawRotatedText:{text}");
		}

		public void DrawRotatedImage(ImageData image, float centerXTwips, float centerYTwips, float widthTwips, float heightTwips, float rotationDegrees, float opacity)
		{
			RotatedImageCalls.Add(new RotatedImageCall(image, centerXTwips, centerYTwips, widthTwips, heightTwips, rotationDegrees, opacity));
			CallOrder.Add("DrawRotatedImage");
		}
	}

	private readonly record struct DrawTextCall(
		string Text,
		float BaselineXTwips,
		float BaselineYTwips,
		RenderFont Font,
		RenderBrush Brush);

	private readonly record struct DrawRectCall(
		RenderRect Rect,
		RenderBrush? Fill,
		RenderStroke? Stroke);

	private readonly record struct HyperlinkCall(RenderRect Rect, string Uri);

	private readonly record struct NamedDestinationCall(string Name, float XTwips, float YTwips);

	private readonly record struct RotatedTextCall(string Text, float CenterXTwips, float CenterYTwips, float RotationDegrees, RenderFont Font, RenderBrush Brush);

	private readonly record struct RotatedImageCall(ImageData Image, float CenterXTwips, float CenterYTwips, float WidthTwips, float HeightTwips, float RotationDegrees, float Opacity);

	private readonly record struct DrawLineCall(RenderPoint From, RenderPoint To, RenderStroke Stroke);
}
