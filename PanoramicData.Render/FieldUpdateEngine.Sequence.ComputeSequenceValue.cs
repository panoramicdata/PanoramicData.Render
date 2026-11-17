namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static int ComputeSequenceValue(
		Dictionary<string, int> counters,
		string identifier,
		int? resetValue)
	{
		if (resetValue.HasValue)
		{
			counters[identifier] = resetValue.Value;
			return resetValue.Value;
		}

		if (!counters.TryGetValue(identifier, out var current))
		{
			current = 0;
		}

		current++;
		counters[identifier] = current;
		return current;
	}
}
