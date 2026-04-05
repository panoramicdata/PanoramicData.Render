namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;

public sealed class SectionInfoTests
{
	[Fact]
	public void Parse_WithDefaultSectionProperties_ReturnsWordDefaults()
	{
		var sectPr = new OoxmlSectionProperties();

		var result = SectionInfoParser.Parse(sectPr);

		result.PageWidth.Should().Be(12240);
		result.PageHeight.Should().Be(15840);
		result.Orientation.Should().Be(PageOrientation.Portrait);
		result.MarginTop.Should().Be(1440);
		result.MarginRight.Should().Be(1440);
		result.MarginBottom.Should().Be(1440);
		result.MarginLeft.Should().Be(1440);
		result.MarginHeader.Should().Be(720);
		result.MarginFooter.Should().Be(720);
		result.MarginGutter.Should().Be(0);
	}

	[Fact]
	public void Parse_WithExplicitPageSize_ExtractsWidthAndHeight()
	{
		var sectPr = new OoxmlSectionProperties(
			new PageSize { Width = 16838, Height = 11906 });

		var result = SectionInfoParser.Parse(sectPr);

		result.PageWidth.Should().Be(16838);
		result.PageHeight.Should().Be(11906);
	}

	[Fact]
	public void Parse_WithLandscapeOrientation_SetsLandscape()
	{
		var sectPr = new OoxmlSectionProperties(
			new PageSize
			{
				Width = 15840,
				Height = 12240,
				Orient = PageOrientationValues.Landscape
			});

		var result = SectionInfoParser.Parse(sectPr);

		result.Orientation.Should().Be(PageOrientation.Landscape);
	}

	[Fact]
	public void Parse_WithPortraitOrientation_SetsPortrait()
	{
		var sectPr = new OoxmlSectionProperties(
			new PageSize
			{
				Width = 12240,
				Height = 15840,
				Orient = PageOrientationValues.Portrait
			});

		var result = SectionInfoParser.Parse(sectPr);

		result.Orientation.Should().Be(PageOrientation.Portrait);
	}

	[Fact]
	public void Parse_WithNoOrientAttribute_DefaultsToPortrait()
	{
		var sectPr = new OoxmlSectionProperties(
			new PageSize { Width = 12240, Height = 15840 });

		var result = SectionInfoParser.Parse(sectPr);

		result.Orientation.Should().Be(PageOrientation.Portrait);
	}

	[Fact]
	public void Parse_WithExplicitMargins_ExtractsAllMargins()
	{
		var sectPr = new OoxmlSectionProperties(
			new PageMargin
			{
				Top = 1000,
				Right = 1100,
				Bottom = 1200,
				Left = 1300,
				Header = 500,
				Footer = 600,
				Gutter = 200
			});

		var result = SectionInfoParser.Parse(sectPr);

		result.MarginTop.Should().Be(1000);
		result.MarginRight.Should().Be(1100);
		result.MarginBottom.Should().Be(1200);
		result.MarginLeft.Should().Be(1300);
		result.MarginHeader.Should().Be(500);
		result.MarginFooter.Should().Be(600);
		result.MarginGutter.Should().Be(200);
	}

	[Fact]
	public void Parse_WithSectionBreakType_ExtractsBreakType()
	{
		var sectPr = new OoxmlSectionProperties(
			new SectionType { Val = SectionMarkValues.Continuous });

		var result = SectionInfoParser.Parse(sectPr);

		result.BreakType.Should().Be(SectionBreakType.Continuous);
	}

	[Fact]
	public void Parse_WithEvenPageBreakType_ExtractsEvenPage()
	{
		var sectPr = new OoxmlSectionProperties(
			new SectionType { Val = SectionMarkValues.EvenPage });

		var result = SectionInfoParser.Parse(sectPr);

		result.BreakType.Should().Be(SectionBreakType.EvenPage);
	}

	[Fact]
	public void Parse_WithOddPageBreakType_ExtractsOddPage()
	{
		var sectPr = new OoxmlSectionProperties(
			new SectionType { Val = SectionMarkValues.OddPage });

		var result = SectionInfoParser.Parse(sectPr);

		result.BreakType.Should().Be(SectionBreakType.OddPage);
	}

	[Fact]
	public void Parse_WithNextPageBreakType_ExtractsNextPage()
	{
		var sectPr = new OoxmlSectionProperties(
			new SectionType { Val = SectionMarkValues.NextPage });

		var result = SectionInfoParser.Parse(sectPr);

		result.BreakType.Should().Be(SectionBreakType.NextPage);
	}

	[Fact]
	public void Parse_WithNextColumnBreakType_ExtractsNextColumn()
	{
		var sectPr = new OoxmlSectionProperties(
			new SectionType { Val = SectionMarkValues.NextColumn });

		var result = SectionInfoParser.Parse(sectPr);

		result.BreakType.Should().Be(SectionBreakType.NextColumn);
	}

	[Fact]
	public void Parse_WithNoBreakType_DefaultsToNextPage()
	{
		var sectPr = new OoxmlSectionProperties();

		var result = SectionInfoParser.Parse(sectPr);

		result.BreakType.Should().Be(SectionBreakType.NextPage);
	}

	[Fact]
	public void Parse_WithHeaderReferences_ExtractsAll()
	{
		var sectPr = new OoxmlSectionProperties(
			new HeaderReference
			{
				Type = HeaderFooterValues.Default,
				Id = "rId1"
			},
			new HeaderReference
			{
				Type = HeaderFooterValues.First,
				Id = "rId2"
			},
			new HeaderReference
			{
				Type = HeaderFooterValues.Even,
				Id = "rId3"
			});

		var result = SectionInfoParser.Parse(sectPr);

		result.HeaderReferences.Should().HaveCount(3);
		result.HeaderReferences[0].Type.Should().Be(HeaderFooterKind.Default);
		result.HeaderReferences[0].RelationshipId.Should().Be("rId1");
		result.HeaderReferences[1].Type.Should().Be(HeaderFooterKind.First);
		result.HeaderReferences[1].RelationshipId.Should().Be("rId2");
		result.HeaderReferences[2].Type.Should().Be(HeaderFooterKind.Even);
		result.HeaderReferences[2].RelationshipId.Should().Be("rId3");
	}

	[Fact]
	public void Parse_WithFooterReferences_ExtractsAll()
	{
		var sectPr = new OoxmlSectionProperties(
			new FooterReference
			{
				Type = HeaderFooterValues.Default,
				Id = "rId4"
			},
			new FooterReference
			{
				Type = HeaderFooterValues.First,
				Id = "rId5"
			});

		var result = SectionInfoParser.Parse(sectPr);

		result.FooterReferences.Should().HaveCount(2);
		result.FooterReferences[0].Type.Should().Be(HeaderFooterKind.Default);
		result.FooterReferences[0].RelationshipId.Should().Be("rId4");
		result.FooterReferences[1].Type.Should().Be(HeaderFooterKind.First);
		result.FooterReferences[1].RelationshipId.Should().Be("rId5");
	}

	[Fact]
	public void Parse_WithNoHeadersOrFooters_ReturnsEmptyLists()
	{
		var sectPr = new OoxmlSectionProperties();

		var result = SectionInfoParser.Parse(sectPr);

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
		var sectPr = new OoxmlSectionProperties(new PageSize());

		var result = SectionInfoParser.Parse(sectPr);

		result.PageWidth.Should().Be(12240);
		result.PageHeight.Should().Be(15840);
		result.Orientation.Should().Be(PageOrientation.Portrait);
	}

	[Fact]
	public void Parse_WithPageMarginNoAttributes_ReturnsDefaults()
	{
		// PageMargin element exists but has no attribute values
		var sectPr = new OoxmlSectionProperties(new PageMargin());

		var result = SectionInfoParser.Parse(sectPr);

		result.MarginTop.Should().Be(1440);
		result.MarginRight.Should().Be(1440);
		result.MarginBottom.Should().Be(1440);
		result.MarginLeft.Should().Be(1440);
		result.MarginHeader.Should().Be(720);
		result.MarginFooter.Should().Be(720);
		result.MarginGutter.Should().Be(0);
	}

	[Fact]
	public void Parse_WithHeaderReferenceNoType_DefaultsToDefault()
	{
		// Header reference element with Id but no Type attribute
		var headerRef = new HeaderReference { Id = "rId1" };
		var sectPr = new OoxmlSectionProperties(headerRef);

		var result = SectionInfoParser.Parse(sectPr);

		result.HeaderReferences.Should().ContainSingle();
		result.HeaderReferences[0].Type.Should().Be(HeaderFooterKind.Default);
		result.HeaderReferences[0].RelationshipId.Should().Be("rId1");
	}

	[Fact]
	public void Parse_WithHeaderReferenceNoId_DefaultsToEmpty()
	{
		// Header reference element with Type but no Id attribute
		var headerRef = new HeaderReference
		{
			Type = HeaderFooterValues.Default
		};
		var sectPr = new OoxmlSectionProperties(headerRef);

		var result = SectionInfoParser.Parse(sectPr);

		result.HeaderReferences.Should().ContainSingle();
		result.HeaderReferences[0].RelationshipId.Should().BeEmpty();
	}

	[Fact]
	public void Parse_WithFooterReferenceNoType_DefaultsToDefault()
	{
		var footerRef = new FooterReference { Id = "rId1" };
		var sectPr = new OoxmlSectionProperties(footerRef);

		var result = SectionInfoParser.Parse(sectPr);

		result.FooterReferences.Should().ContainSingle();
		result.FooterReferences[0].Type.Should().Be(HeaderFooterKind.Default);
	}

	[Fact]
	public void Parse_WithFooterReferenceNoId_DefaultsToEmpty()
	{
		var footerRef = new FooterReference
		{
			Type = HeaderFooterValues.First
		};
		var sectPr = new OoxmlSectionProperties(footerRef);

		var result = SectionInfoParser.Parse(sectPr);

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
}
