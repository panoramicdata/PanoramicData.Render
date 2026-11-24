namespace PanoramicData.Render;

/// <summary>
/// Selects the subset of pages to render for a configured page range.
/// </summary>
internal static class PageRangeSelector
{
	public static IReadOnlyList<T> Apply<T>(IReadOnlyList<T> pages, Range? pageRange)
	{
		if (pageRange is null)
		{
			return pages;
		}

		var start = pageRange.Value.Start.GetOffset(pages.Count);
		var end = pageRange.Value.End.GetOffset(pages.Count);
		if (start < 0 || end < start || start > pages.Count)
		{
			return [];
		}

		end = Math.Min(end, pages.Count);
		var result = new List<T>(end - start);
		for (var index = start; index < end; index++)
		{
			result.Add(pages[index]);
		}

		return result;
	}
}