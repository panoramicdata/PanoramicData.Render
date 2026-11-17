using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PanoramicData.Render;

public sealed partial class DocxRenderer
{
	/// <summary>
	/// Builds a mapping from each concrete numId to the lowest numId that shares
	/// the same abstractNumId, so numbering instances that logically belong to the
	/// same multilevel definition share counter state.
	/// </summary>
	private void BuildNumberingIdNormalization(DocumentFormat.OpenXml.Packaging.NumberingDefinitionsPart numberingPart)
	{
		var numbering = numberingPart.Numbering;
		if (numbering is null)
		{
			return;
		}

		var instances = numbering.Elements<DocumentFormat.OpenXml.Wordprocessing.NumberingInstance>()
			.Select(TryGetInstanceIds)
			.OfType<(int NumId, int AbstractId)>()
			.ToList();

		// Group numIds by abstractNumId and pick the lowest numId as canonical.
		var abstractToCanonical = new Dictionary<int, int>();
		foreach (var (numId, abstractId) in instances)
		{
			if (!abstractToCanonical.TryGetValue(abstractId, out var canonical) || numId < canonical)
			{
				abstractToCanonical[abstractId] = numId;
			}
		}

		foreach (var (numId, abstractId) in instances)
		{
			var canonical = abstractToCanonical[abstractId];
			if (canonical != numId)
			{
				_options.NumberingIdNormalization[numId] = canonical;
			}
		}
	}

	private static (int NumId, int AbstractId)? TryGetInstanceIds(DocumentFormat.OpenXml.Wordprocessing.NumberingInstance instance)
	{
		var numId = instance.NumberID?.Value;
		var abstractId = instance.AbstractNumId?.Val?.Value;
		return numId is null || abstractId is null ? null : (numId.Value, abstractId.Value);
	}
}
