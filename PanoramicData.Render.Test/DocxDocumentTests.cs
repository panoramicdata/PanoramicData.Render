namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class DocxDocumentTests
{
	[Fact]
	public void Load_WithFullDocx_ExtractsDocumentBody()
	{
		using var stream = TestDocxBuilder.CreateFullDocx();
		using var doc = DocxDocument.Load(stream);

		doc.DocumentBody.Should().NotBeNull();
	}

	[Fact]
	public void Load_WithFullDocx_ExtractsStylesPart()
	{
		using var stream = TestDocxBuilder.CreateFullDocx();
		using var doc = DocxDocument.Load(stream);

		doc.StylesPart.Should().NotBeNull();
	}

	[Fact]
	public void Load_WithFullDocx_ExtractsThemePart()
	{
		using var stream = TestDocxBuilder.CreateFullDocx();
		using var doc = DocxDocument.Load(stream);

		doc.ThemePart.Should().NotBeNull();
	}

	[Fact]
	public void Load_WithFullDocx_ExtractsNumberingPart()
	{
		using var stream = TestDocxBuilder.CreateFullDocx();
		using var doc = DocxDocument.Load(stream);

		doc.NumberingPart.Should().NotBeNull();
	}

	[Fact]
	public void Load_WithFullDocx_ExtractsSettingsPart()
	{
		using var stream = TestDocxBuilder.CreateFullDocx();
		using var doc = DocxDocument.Load(stream);

		doc.SettingsPart.Should().NotBeNull();
	}

	[Fact]
	public void Load_WithFullDocx_BodyContainsParagraphs()
	{
		using var stream = TestDocxBuilder.CreateFullDocx();
		using var doc = DocxDocument.Load(stream);

		doc.DocumentBody.Elements<Paragraph>().Should().ContainSingle();
	}

	[Fact]
	public void Load_WithMinimalDocx_HasNullOptionalParts()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		doc.DocumentBody.Should().NotBeNull();
		doc.StylesPart.Should().BeNull();
		doc.ThemePart.Should().BeNull();
		doc.NumberingPart.Should().BeNull();
		doc.SettingsPart.Should().BeNull();
	}

	[Fact]
	public void Load_WithNullStream_ThrowsArgumentNullException()
	{
		Action act = () => DocxDocument.Load(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Load_WithDocxMissingMainPart_ThrowsInvalidOperationException()
	{
		using var stream = TestDocxBuilder.CreateDocxWithoutMainPart();

		Action act = () => DocxDocument.Load(stream);

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*main document part*");
	}

	[Fact]
	public void Load_WithDocxMissingBody_ThrowsInvalidOperationException()
	{
		using var stream = TestDocxBuilder.CreateDocxWithoutBody();

		Action act = () => DocxDocument.Load(stream);

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*document body*");
	}
}
