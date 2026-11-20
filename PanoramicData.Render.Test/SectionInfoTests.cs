namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;

public sealed partial class SectionInfoTests
{
	[Fact]
	public void Parse_WithDefaultSectionProperties_ReturnsWordDefaults()
	{
		var result = ParseSection();

		result.PageWidth.Should().Be(12240);
		result.PageHeight.Should().Be(15840);
		result.Orientation.Should().Be(PageOrientation.Portrait);
		AssertMargins(result, 1440, 1440, 1440, 1440, 720, 720, 0);
	}

	[Fact]
	public void Parse_WithExplicitPageSize_ExtractsWidthAndHeight()
	{
		var result = ParseSection(new PageSize { Width = 16838, Height = 11906 });

		result.PageWidth.Should().Be(16838);
		result.PageHeight.Should().Be(11906);
	}

	[Fact]
	public void Parse_WithLandscapeOrientation_SetsLandscape()
	{
		var result = ParseSection(new PageSize { Width = 15840, Height = 12240, Orient = PageOrientationValues.Landscape });

		result.Orientation.Should().Be(PageOrientation.Landscape);
	}

	[Fact]
	public void Parse_WithPortraitOrientation_SetsPortrait()
	{
		var result = ParseSection(new PageSize { Width = 12240, Height = 15840, Orient = PageOrientationValues.Portrait });

		result.Orientation.Should().Be(PageOrientation.Portrait);
	}

	[Fact]
	public void Parse_WithNoOrientAttribute_DefaultsToPortrait()
	{
		var result = ParseSection(new PageSize { Width = 12240, Height = 15840 });

		result.Orientation.Should().Be(PageOrientation.Portrait);
	}

	[Fact]
	public void Parse_WithExplicitMargins_ExtractsAllMargins()
	{
		var result = ParseSection(new PageMargin
		{
			Top = 1000,
			Right = 1100,
			Bottom = 1200,
			Left = 1300,
			Header = 500,
			Footer = 600,
			Gutter = 200
		});

		AssertMargins(result, 1000, 1100, 1200, 1300, 500, 600, 200);
	}

	[Fact]
	public void Parse_WithSectionBreakType_ExtractsBreakType()
	{
		ParseBreakType(SectionMarkValues.Continuous).Should().Be(SectionBreakType.Continuous);
	}

	[Fact]
	public void Parse_WithEvenPageBreakType_ExtractsEvenPage()
	{
		ParseBreakType(SectionMarkValues.EvenPage).Should().Be(SectionBreakType.EvenPage);
	}

	[Fact]
	public void Parse_WithOddPageBreakType_ExtractsOddPage()
	{
		ParseBreakType(SectionMarkValues.OddPage).Should().Be(SectionBreakType.OddPage);
	}

	[Fact]
	public void Parse_WithNextPageBreakType_ExtractsNextPage()
	{
		ParseBreakType(SectionMarkValues.NextPage).Should().Be(SectionBreakType.NextPage);
	}

	[Fact]
	public void Parse_WithNextColumnBreakType_ExtractsNextColumn()
	{
		ParseBreakType(SectionMarkValues.NextColumn).Should().Be(SectionBreakType.NextColumn);
	}

	[Fact]
	public void Parse_WithNoBreakType_DefaultsToNextPage()
	{
		ParseSection().BreakType.Should().Be(SectionBreakType.NextPage);
	}

	[Fact]
	public void Parse_WithHeaderReferences_ExtractsAll()
	{
		var result = ParseSection(
			new HeaderReference { Type = HeaderFooterValues.Default, Id = "rId1" },
			new HeaderReference { Type = HeaderFooterValues.First, Id = "rId2" },
			new HeaderReference { Type = HeaderFooterValues.Even, Id = "rId3" });

		AssertReferences(result.HeaderReferences, (HeaderFooterKind.Default, "rId1"), (HeaderFooterKind.First, "rId2"), (HeaderFooterKind.Even, "rId3"));
	}

	[Fact]
	public void Parse_WithFooterReferences_ExtractsAll()
	{
		var result = ParseSection(
			new FooterReference { Type = HeaderFooterValues.Default, Id = "rId4" },
			new FooterReference { Type = HeaderFooterValues.First, Id = "rId5" });

		AssertReferences(result.FooterReferences, (HeaderFooterKind.Default, "rId4"), (HeaderFooterKind.First, "rId5"));
	}

	[Fact]
	public void Parse_WithNoHeadersOrFooters_ReturnsEmptyLists()
	{
		var result = ParseSection();

		result.HeaderReferences.Should().BeEmpty();
		result.FooterReferences.Should().BeEmpty();
	}

	[Fact]
	public void Parse_NullSectionProperties_ThrowsArgumentNullException()
	{
		Action act = () => SectionInfoParser.Parse(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Parse_WithPageSizeNoAttributes_ReturnsDefaults()
	{
		// PageSize element exists but has no Width/Height/Orient attributes
		var result = ParseSection(new PageSize());

		result.PageWidth.Should().Be(12240);
		result.PageHeight.Should().Be(15840);
		result.Orientation.Should().Be(PageOrientation.Portrait);
	}

	[Fact]
	public void Parse_WithPageMarginNoAttributes_ReturnsDefaults()
	{
		// PageMargin element exists but has no attribute values
		var result = ParseSection(new PageMargin());

		AssertMargins(result, 1440, 1440, 1440, 1440, 720, 720, 0);
	}

	[Fact]
	public void Parse_WithHeaderReferenceNoType_DefaultsToDefault()
	{
		var result = ParseSection(new HeaderReference { Id = "rId1" });

		AssertReferences(result.HeaderReferences, (HeaderFooterKind.Default, "rId1"));
	}

	[Fact]
	public void Parse_WithHeaderReferenceNoId_DefaultsToEmpty()
	{
		var result = ParseSection(new HeaderReference { Type = HeaderFooterValues.Default });

		result.HeaderReferences.Should().ContainSingle();
		result.HeaderReferences[0].RelationshipId.Should().BeEmpty();
	}

	[Fact]
	public void Parse_WithFooterReferenceNoType_DefaultsToDefault()
	{
		var result = ParseSection(new FooterReference { Id = "rId1" });

		result.FooterReferences.Should().ContainSingle();
		result.FooterReferences[0].Type.Should().Be(HeaderFooterKind.Default);
	}

	[Fact]
	public void Parse_WithFooterReferenceNoId_DefaultsToEmpty()
	{
		var result = ParseSection(new FooterReference { Type = HeaderFooterValues.First });

		result.FooterReferences.Should().ContainSingle();
		result.FooterReferences[0].RelationshipId.Should().BeEmpty();
	}

	[Fact]
	public void ParseAll_WithSingleSection_ReturnsOneSection()
	{
		using var stream = TestDocxBuilder.CreateDocxWithSectionProperties(
			new OoxmlSectionProperties(
				new PageSize { Width = 12240, Height = 15840 },
				new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 }));
		using var doc = DocxDocument.Load(stream);

		var sections = SectionInfoParser.ParseAll(doc.DocumentBody);

		sections.Should().ContainSingle();
		sections[0].PageWidth.Should().Be(12240);
	}

	[Fact]
	public void ParseAll_WithMultipleSections_ReturnsAllSections()
	{
		using var stream = TestDocxBuilder.CreateDocxWithMultipleSections();
		using var doc = DocxDocument.Load(stream);

		var sections = SectionInfoParser.ParseAll(doc.DocumentBody);

		sections.Should().HaveCount(2);
		// First section (from paragraph sectPr) is landscape A4
		sections[0].PageWidth.Should().Be(16838);
		sections[0].Orientation.Should().Be(PageOrientation.Landscape);
		// Second section (from body sectPr) is portrait US Letter
		sections[1].PageWidth.Should().Be(12240);
		sections[1].Orientation.Should().Be(PageOrientation.Portrait);
	}

	[Fact]
	public void ParseAll_WithNoSectionProperties_ReturnsDefaultSection()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var sections = SectionInfoParser.ParseAll(doc.DocumentBody);

		sections.Should().ContainSingle();
		sections[0].PageWidth.Should().Be(12240);
		sections[0].PageHeight.Should().Be(15840);
	}

	[Fact]
	public void ParseAll_NullBody_ThrowsArgumentNullException()
	{
		Action act = () => SectionInfoParser.ParseAll(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	private static SectionInfo ParseSection(params OpenXmlElement[] children)
		=> SectionInfoParser.Parse(new OoxmlSectionProperties(children));

	private static SectionBreakType ParseBreakType(SectionMarkValues value)
		=> ParseSection(new SectionType { Val = value }).BreakType;

	private static void AssertReferences(IReadOnlyList<HeaderFooterReference> references, params (HeaderFooterKind Type, string Id)[] expected)
	{
		references.Should().HaveCount(expected.Length);
		for (var index = 0; index < expected.Length; index++)
		{
			references[index].Type.Should().Be(expected[index].Type);
			references[index].RelationshipId.Should().Be(expected[index].Id);
		}
	}

	private static void AssertMargins(SectionInfo result, int top, int right, int bottom, int left, int header, int footer, int gutter)
	{
		result.MarginTop.Should().Be(top);
		result.MarginRight.Should().Be(right);
		result.MarginBottom.Should().Be(bottom);
		result.MarginLeft.Should().Be(left);
		result.MarginHeader.Should().Be(header);
		result.MarginFooter.Should().Be(footer);
		result.MarginGutter.Should().Be(gutter);
	}
}
