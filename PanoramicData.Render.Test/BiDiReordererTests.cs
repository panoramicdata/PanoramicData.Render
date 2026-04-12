namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class BiDiReordererTests
{
	private readonly record struct TestElement(string Text, bool IsRtl);

	private static readonly Func<TestElement, bool> IsRtlPredicate = e => e.IsRtl;

	[Fact]
	public void Reorder_AllLtrInLtrParagraph_ReturnsSameOrder()
	{
		var elements = new TestElement[]
		{
			new("Hello", false),
			new("World", false)
		};

		var result = BiDiReorderer.Reorder<TestElement>(elements, IsRtlPredicate, false);

		result.Should().HaveCount(2);
		result[0].Text.Should().Be("Hello");
		result[1].Text.Should().Be("World");
	}

	[Fact]
	public void Reorder_SingleElement_ReturnsSameElement()
	{
		var elements = new TestElement[] { new("Only", false) };

		var result = BiDiReorderer.Reorder<TestElement>(elements, IsRtlPredicate, false);

		result.Should().ContainSingle().Which.Text.Should().Be("Only");
	}

	[Fact]
	public void Reorder_EmptyList_ReturnsEmpty()
	{
		var elements = Array.Empty<TestElement>();

		var result = BiDiReorderer.Reorder<TestElement>(elements, IsRtlPredicate, false);

		result.Should().BeEmpty();
	}

	[Fact]
	public void Reorder_MixedLtrRtlInLtrParagraph_ReversesRtlGroup()
	{
		// LTR paragraph: "Hello [RTL-A RTL-B] World" → "Hello [RTL-B RTL-A] World"
		var elements = new TestElement[]
		{
			new("Hello", false),
			new("RTL-A", true),
			new("RTL-B", true),
			new("World", false)
		};

		var result = BiDiReorderer.Reorder<TestElement>(elements, IsRtlPredicate, false);

		result.Should().HaveCount(4);
		result[0].Text.Should().Be("Hello");
		result[1].Text.Should().Be("RTL-B"); // Reversed
		result[2].Text.Should().Be("RTL-A"); // Reversed
		result[3].Text.Should().Be("World");
	}

	[Fact]
	public void Reorder_TrailingRtlInLtrParagraph_ReversesTrailingGroup()
	{
		var elements = new TestElement[]
		{
			new("Start", false),
			new("RTL-1", true),
			new("RTL-2", true),
			new("RTL-3", true)
		};

		var result = BiDiReorderer.Reorder<TestElement>(elements, IsRtlPredicate, false);

		result.Should().HaveCount(4);
		result[0].Text.Should().Be("Start");
		result[1].Text.Should().Be("RTL-3");
		result[2].Text.Should().Be("RTL-2");
		result[3].Text.Should().Be("RTL-1");
	}

	[Fact]
	public void Reorder_AllRtlInRtlParagraph_ReversesEntireList()
	{
		// RTL paragraph with all RTL elements: entire list reversed
		var elements = new TestElement[]
		{
			new("A", true),
			new("B", true),
			new("C", true)
		};

		var result = BiDiReorderer.Reorder<TestElement>(elements, IsRtlPredicate, true);

		result.Should().HaveCount(3);
		result[0].Text.Should().Be("C");
		result[1].Text.Should().Be("B");
		result[2].Text.Should().Be("A");
	}

	[Fact]
	public void Reorder_MixedInRtlParagraph_ReversesOverallAndLtrGroups()
	{
		// RTL paragraph: logical [RTL-A, LTR-B, LTR-C, RTL-D]
		// In RTL paragraph, LTR runs are "opposite" → reversed within their group
		// Then entire result reversed for RTL base direction
		var elements = new TestElement[]
		{
			new("RTL-A", true),
			new("LTR-B", false),
			new("LTR-C", false),
			new("RTL-D", true)
		};

		var result = BiDiReorderer.Reorder<TestElement>(elements, IsRtlPredicate, true);

		result.Should().HaveCount(4);
		// Step 1: LTR group [LTR-B, LTR-C] are "opposite" → reversed to [LTR-C, LTR-B]
		// Intermediate: [RTL-A, LTR-C, LTR-B, RTL-D]
		// Step 2: Entire list reversed for RTL base: [RTL-D, LTR-B, LTR-C, RTL-A]
		result[0].Text.Should().Be("RTL-D");
		result[1].Text.Should().Be("LTR-B");
		result[2].Text.Should().Be("LTR-C");
		result[3].Text.Should().Be("RTL-A");
	}

	[Fact]
	public void Reorder_SingleRtlInLtrParagraph_PreservesOrder()
	{
		// A single RTL element among LTR — "reversed" group of one stays in place
		var elements = new TestElement[]
		{
			new("Before", false),
			new("RTL", true),
			new("After", false)
		};

		var result = BiDiReorderer.Reorder<TestElement>(elements, IsRtlPredicate, false);

		result.Should().HaveCount(3);
		result[0].Text.Should().Be("Before");
		result[1].Text.Should().Be("RTL");
		result[2].Text.Should().Be("After");
	}
}
