namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Resolves numbering styles by applying numbering instance links and level overrides.
/// </summary>
internal static partial class NumberingStyleResolver
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

		var numberingInstance = FindInstance(numbering, numberingId);
		var abstractNum = FindAbstractNum(numbering, numberingInstance);
		if (numberingInstance is null || abstractNum is null)
		{
			return null;
		}

		var levelOverride = FindLevelOverride(numberingInstance, levelIndex);
		var effectiveLevel = FindEffectiveLevel(abstractNum, levelOverride, levelIndex);

		return effectiveLevel is null ? null : BuildLevelStyle(effectiveLevel, levelOverride, levelIndex);
	}

	private static NumberingInstance? FindInstance(Numbering numbering, int numberingId)
		=> numbering.Elements<NumberingInstance>().FirstOrDefault(n => n.NumberID?.Value == numberingId);

	private static LevelOverride? FindLevelOverride(NumberingInstance numberingInstance, int levelIndex)
		=> numberingInstance.Elements<LevelOverride>().FirstOrDefault(o => o.LevelIndex?.Value == levelIndex);

	private static Level? FindEffectiveLevel(AbstractNum abstractNum, LevelOverride? levelOverride, int levelIndex)
		=> levelOverride?.GetFirstChild<Level>()
			?? abstractNum.Elements<Level>().FirstOrDefault(l => l.LevelIndex?.Value == levelIndex);
	private static AbstractNum? FindAbstractNum(Numbering numbering, NumberingInstance? numberingInstance)
	{
		var abstractNumId = numberingInstance?.AbstractNumId?.Val?.Value;
		return abstractNumId is null
			? null
			: numbering.Elements<AbstractNum>().FirstOrDefault(a => a.AbstractNumberId?.Value == abstractNumId.Value);
	}
}
