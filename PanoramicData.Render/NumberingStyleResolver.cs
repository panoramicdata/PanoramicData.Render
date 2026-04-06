namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Resolves numbering styles by applying numbering instance links and level overrides.
/// </summary>
internal static class NumberingStyleResolver
{
	/// <summary>
	/// Resolves the effective numbering level style for a numbering instance and level index.
	/// </summary>
	/// <param name="numberingPart">The numbering definitions part.</param>
	/// <param name="numberingId">The numbering instance ID (<c>w:numId</c>).</param>
	/// <param name="levelIndex">The requested numbering level index (<c>w:ilvl</c>).</param>
	/// <returns>A resolved <see cref="NumberingLevelStyle"/>, or <see langword="null"/> when resolution fails.</returns>
	public static NumberingLevelStyle? ResolveLevel(
		NumberingDefinitionsPart? numberingPart,
		int numberingId,
		int levelIndex)
	{
		if (levelIndex < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(levelIndex));
		}

		var numbering = numberingPart?.Numbering;
		if (numbering is null)
		{
			return null;
		}

		var numberingInstance = numbering.Elements<NumberingInstance>()
			.FirstOrDefault(n => n.NumberID?.Value == numberingId);
		if (numberingInstance is null)
		{
			return null;
		}

		var abstractNumId = numberingInstance.AbstractNumId?.Val?.Value;
		if (abstractNumId is null)
		{
			return null;
		}

		var abstractNum = numbering.Elements<AbstractNum>()
			.FirstOrDefault(a => a.AbstractNumberId?.Value == abstractNumId.Value);
		if (abstractNum is null)
		{
			return null;
		}

		var levelOverride = numberingInstance.Elements<LevelOverride>()
			.FirstOrDefault(o => o.LevelIndex?.Value == levelIndex);

		var baseLevel = abstractNum.Elements<Level>()
			.FirstOrDefault(l => l.LevelIndex?.Value == levelIndex);

		var overrideLevel = levelOverride?.GetFirstChild<Level>();
		var effectiveLevel = overrideLevel ?? baseLevel;
		if (effectiveLevel is null)
		{
			return null;
		}

		var start = effectiveLevel.StartNumberingValue?.Val?.Value ?? 1;
		var startOverride = levelOverride?.StartOverrideNumberingValue?.Val?.Value;
		if (startOverride is not null)
		{
			start = startOverride.Value;
		}

		return new NumberingLevelStyle
		{
			LevelIndex = levelIndex,
			Start = start,
			NumberFormat = effectiveLevel.NumberingFormat?.Val?.InnerText,
			LevelText = effectiveLevel.LevelText?.Val?.Value
		};
	}
}
