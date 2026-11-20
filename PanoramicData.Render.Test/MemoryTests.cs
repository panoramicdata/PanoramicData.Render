namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

/// <summary>
/// Memory sanity tests to verify no obvious memory leaks in the rendering pipeline.
/// </summary>
[Collection("NonParallel")]
public sealed class MemoryTests
{
	/// <summary>
	/// Renders many documents sequentially and verifies GC-collected memory doesn't grow unboundedly.
	/// </summary>
	[Fact]
	public void RepeatedRenders_MemoryDoesNotGrowUnbounded()
	{
		var renderer = new DocxRenderer(new RenderOptions());

		using (var warmup = CreateTestDocx())
		{
			_ = renderer.Render(warmup);
		}

		var baselineMemory = MemoryAfterFullCollection();

		for (var i = 0; i < 200; i++)
		{
			using var stream = CreateTestDocx();
			_ = RenderWithSvg(renderer, stream).ToPdf();
		}

		var finalMemory = MemoryAfterFullCollection();

		// Memory should not grow by more than 50MB after GC for 200 simple documents
		var growth = finalMemory - baselineMemory;
		growth.Should().BeLessThan(50 * 1024 * 1024);
	}

	/// <summary>
	/// Verifies that DocxDocument instances are properly disposed after rendering.
	/// </summary>
	[Fact]
	public void Render_StreamDisposal_DoesNotHoldReferences()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		var weakRefs = new List<WeakReference>();

		for (var i = 0; i < 10; i++)
		{
			var stream = CreateTestDocx();
			weakRefs.Add(new WeakReference(stream));
			RenderWithSvg(renderer, stream);
			stream.Dispose();
		}

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		// At least some streams should be collected (not all held alive)
		var collectedCount = weakRefs.Count(wr => !wr.IsAlive);
		collectedCount.Should().BeGreaterThan(0);
	}

	private static RenderResult RenderWithSvg(DocxRenderer renderer, MemoryStream stream)
	{
		var result = renderer.Render(stream);
		_ = result.Pages[0].ToSvg();
		return result;
	}

	private static long MemoryAfterFullCollection()
	{
		ForceFullCollection();
		GC.WaitForPendingFinalizers();
		ForceFullCollection();
		return GC.GetTotalMemory(forceFullCollection: false);
	}

	private static void ForceFullCollection()
		=> GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

	private static MemoryStream CreateTestDocx()
		=> SimpleDocxBuilder.Create(5, i => $"Test paragraph {i + 1} with some content.");
}
