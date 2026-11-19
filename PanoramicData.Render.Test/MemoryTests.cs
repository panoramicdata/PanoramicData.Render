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
			var result = renderer.Render(stream);
			_ = result.Pages[0].ToSvg();
			_ = result.ToPdf();
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
			var result = renderer.Render(stream);
			_ = result.Pages[0].ToSvg();
			stream.Dispose();
		}

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		// At least some streams should be collected (not all held alive)
		var collectedCount = weakRefs.Count(wr => !wr.IsAlive);
		collectedCount.Should().BeGreaterThan(0);
	}

	private static long MemoryAfterFullCollection()
	{
		GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
		GC.WaitForPendingFinalizers();
		GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
		return GC.GetTotalMemory(forceFullCollection: false);
	}

	private static MemoryStream CreateTestDocx()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			var body = new Body();
			for (var i = 0; i < 5; i++)
			{
				body.AppendChild(new Paragraph(
					new Run(new Text($"Test paragraph {i + 1} with some content."))));
			}

			mainPart.Document = new Document(body);
		}

		stream.Position = 0;
		return stream;
	}
}
