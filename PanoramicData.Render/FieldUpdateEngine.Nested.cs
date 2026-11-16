namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private sealed class ActiveFieldUpdate
	{
		public StringBuilder InstructionBuilder { get; } = new();

		public bool IsResultSection { get; set; }

		public FieldUpdateKind Kind { get; set; } = FieldUpdateKind.Other;

		public bool HasWrittenValue { get; set; }
	}

	private enum FieldUpdateKind
	{
		Other,
		Page,
		NumPages,
		Toc,
		Title,
		Author,
		Subject,
		Keywords,
		Description,
		Filename
	}
}
