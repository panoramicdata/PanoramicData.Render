namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static bool TryGetTocInstruction(Paragraph paragraph, out string instruction)
	{
		var found = FindSimpleTocInstruction(paragraph) ?? FindComplexTocInstruction(paragraph);
		instruction = found ?? string.Empty;
		return found is not null;
	}

	private static string? FindSimpleTocInstruction(Paragraph paragraph)
	{
		foreach (var simpleField in paragraph.Descendants<SimpleField>())
		{
			var candidateInstruction = simpleField.Instruction?.Value;
			if (ParseFieldKind(candidateInstruction) is FieldUpdateKind.Toc)
			{
				return candidateInstruction ?? string.Empty;
			}
		}

		return null;
	}

	private static string? FindComplexTocInstruction(Paragraph paragraph)
	{
		var activeInstructions = new Stack<StringBuilder>();

		foreach (var run in paragraph.Descendants<Run>())
		{
			foreach (var child in run.ChildElements)
			{
				var completed = AdvanceTocInstruction(child, activeInstructions);
				if (completed is not null && ParseFieldKind(completed) is FieldUpdateKind.Toc)
				{
					return completed;
				}
			}
		}

		return null;
	}

	private static string? AdvanceTocInstruction(OpenXmlElement child, Stack<StringBuilder> activeInstructions)
	{
		if (child is FieldChar fieldChar)
		{
			return AdvanceTocInstruction(fieldChar.FieldCharType?.Value, activeInstructions);
		}

		if (child is FieldCode fieldCode && activeInstructions.Count > 0)
		{
			activeInstructions.Peek().Append(fieldCode.Text);
		}

		return null;
	}

	private static string? AdvanceTocInstruction(FieldCharValues? type, Stack<StringBuilder> activeInstructions)
	{
		if (type == FieldCharValues.Begin)
		{
			activeInstructions.Push(new StringBuilder());
			return null;
		}

		if (activeInstructions.Count == 0)
		{
			return null;
		}

		if (type == FieldCharValues.Separate)
		{
			return activeInstructions.Peek().ToString();
		}

		return type == FieldCharValues.End ? activeInstructions.Pop().ToString() : null;
	}
}
