namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;

public sealed class HeaderFooterResolverTests
{
	private static readonly HeaderFooterReference DefaultHeader = new(HeaderFooterKind.Default, "rId1");
	private static readonly HeaderFooterReference FirstHeader = new(HeaderFooterKind.First, "rId2");
	private static readonly HeaderFooterReference EvenHeader = new(HeaderFooterKind.Even, "rId3");

	// --- ResolveHeader tests ---

	[Fact]
	public void ResolveHeader_NullSection_ThrowsArgumentNullException()
	{
		var act = () => HeaderFooterResolver.ResolveHeader(null!, true, 1, false);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("section");
	}

	[Fact]
	public void ResolveHeader_NoReferences_ReturnsNull()
	{
		var section = new SectionInfo();

		var result = HeaderFooterResolver.ResolveHeader(section, true, 1, false);

		result.Should().BeNull();
	}

	[Fact]
	public void ResolveHeader_DefaultOnly_ReturnsDefault()
	{
		var section = new SectionInfo
		{
			HeaderReferences = [DefaultHeader]
		};

		var result = HeaderFooterResolver.ResolveHeader(section, false, 1, false);

		result.Should().BeSameAs(DefaultHeader);
	}

	[Fact]
	public void ResolveHeader_FirstPage_WithTitlePage_ReturnsFirst()
	{
		var section = new SectionInfo
		{
			TitlePage = true,
			HeaderReferences = [DefaultHeader, FirstHeader]
		};

		var result = HeaderFooterResolver.ResolveHeader(section, true, 1, false);

		result.Should().BeSameAs(FirstHeader);
	}

	[Fact]
	public void ResolveHeader_FirstPage_WithoutTitlePage_ReturnsDefault()
	{
		var section = new SectionInfo
		{
			TitlePage = false,
			HeaderReferences = [DefaultHeader, FirstHeader]
		};

		var result = HeaderFooterResolver.ResolveHeader(section, true, 1, false);

		result.Should().BeSameAs(DefaultHeader);
	}

	[Fact]
	public void ResolveHeader_EvenPage_WithEvenAndOdd_ReturnsEven()
	{
		var section = new SectionInfo
		{
			HeaderReferences = [DefaultHeader, EvenHeader]
		};

		var result = HeaderFooterResolver.ResolveHeader(section, false, 2, true);

		result.Should().BeSameAs(EvenHeader);
	}

	[Fact]
	public void ResolveHeader_OddPage_WithEvenAndOdd_ReturnsDefault()
	{
		var section = new SectionInfo
		{
			HeaderReferences = [DefaultHeader, EvenHeader]
		};

		var result = HeaderFooterResolver.ResolveHeader(section, false, 3, true);

		result.Should().BeSameAs(DefaultHeader);
	}

	[Fact]
	public void ResolveHeader_EvenPage_WithoutEvenAndOdd_ReturnsDefault()
	{
		var section = new SectionInfo
		{
			HeaderReferences = [DefaultHeader, EvenHeader]
		};

		var result = HeaderFooterResolver.ResolveHeader(section, false, 2, false);

		result.Should().BeSameAs(DefaultHeader);
	}

	[Fact]
	public void ResolveHeader_FirstPage_TitlePage_EvenAndOdd_PrefersFirst()
	{
		var section = new SectionInfo
		{
			TitlePage = true,
			HeaderReferences = [DefaultHeader, FirstHeader, EvenHeader]
		};

		// Page 1 is odd, but isFirstPageOfSection with titlePage → First wins over Even.
		var result = HeaderFooterResolver.ResolveHeader(section, true, 1, true);

		result.Should().BeSameAs(FirstHeader);
	}

	[Fact]
	public void ResolveHeader_FirstPage_TitlePage_NoFirstRef_ReturnsNull()
	{
		var section = new SectionInfo
		{
			TitlePage = true,
			HeaderReferences = [DefaultHeader]
		};

		var result = HeaderFooterResolver.ResolveHeader(section, true, 1, false);

		result.Should().BeNull();
	}

	[Fact]
	public void ResolveHeader_EvenPage_NoEvenRef_FallsToDefault()
	{
		var section = new SectionInfo
		{
			HeaderReferences = [DefaultHeader]
		};

		var result = HeaderFooterResolver.ResolveHeader(section, false, 2, true);

		result.Should().BeSameAs(DefaultHeader);
	}

	// --- ResolveFooter tests ---

	[Fact]
	public void ResolveFooter_NullSection_ThrowsArgumentNullException()
	{
		var act = () => HeaderFooterResolver.ResolveFooter(null!, true, 1, false);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("section");
	}

	[Fact]
	public void ResolveFooter_DefaultOnly_ReturnsDefault()
	{
		var defaultFooter = new HeaderFooterReference(HeaderFooterKind.Default, "rFoot1");
		var section = new SectionInfo
		{
			FooterReferences = [defaultFooter]
		};

		var result = HeaderFooterResolver.ResolveFooter(section, false, 1, false);

		result.Should().BeSameAs(defaultFooter);
	}

	[Fact]
	public void ResolveFooter_FirstPage_WithTitlePage_ReturnsFirst()
	{
		var defaultFooter = new HeaderFooterReference(HeaderFooterKind.Default, "rFoot1");
		var firstFooter = new HeaderFooterReference(HeaderFooterKind.First, "rFoot2");
		var section = new SectionInfo
		{
			TitlePage = true,
			FooterReferences = [defaultFooter, firstFooter]
		};

		var result = HeaderFooterResolver.ResolveFooter(section, true, 1, false);

		result.Should().BeSameAs(firstFooter);
	}

	// --- SectionInfoParser TitlePage parsing ---

	[Fact]
	public void Parse_WithTitlePage_SetsTrue()
	{
		var sectPr = new OoxmlSectionProperties(new TitlePage());

		var result = SectionInfoParser.Parse(sectPr);

		result.TitlePage.Should().BeTrue();
	}

	[Fact]
	public void Parse_WithTitlePageValFalse_SetsFalse()
	{
		var sectPr = new OoxmlSectionProperties(
			new TitlePage { Val = false });

		var result = SectionInfoParser.Parse(sectPr);

		result.TitlePage.Should().BeFalse();
	}

	[Fact]
	public void Parse_WithNoTitlePage_SetsFalse()
	{
		var sectPr = new OoxmlSectionProperties();

		var result = SectionInfoParser.Parse(sectPr);

		result.TitlePage.Should().BeFalse();
	}
}
