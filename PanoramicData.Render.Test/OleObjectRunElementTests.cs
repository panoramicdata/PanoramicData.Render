namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class OleObjectRunElementTests
{
	private const string RelationshipsNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

	private static OleObjectRunElement Parse(EmbeddedObject oleObject)
		=> RunElementParser.Parse(new Run(oleObject)).Should().ContainSingle()
			.Which.Should().BeOfType<OleObjectRunElement>()
			.Subject;

	private static EmbeddedObject CreateOleObject(string oleRelId, string previewRelId, (int Dxa, int Dya)? size = null)
	{
		var oleObjElement = new OpenXmlUnknownElement("o", "OLEObject", "urn:schemas-microsoft-com:office:office");
		oleObjElement.SetAttribute(new OpenXmlAttribute("r", "id", RelationshipsNs, oleRelId));

		var embeddedObject = new EmbeddedObject(oleObjElement);
		if (size is { } s)
		{
			embeddedObject.SetAttribute(new OpenXmlAttribute("w", "dxaOrig", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", s.Dxa.ToString()));
			embeddedObject.SetAttribute(new OpenXmlAttribute("w", "dyaOrig", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", s.Dya.ToString()));
		}

		if (!string.IsNullOrEmpty(previewRelId))
		{
			var imageData = new OpenXmlUnknownElement("v", "imagedata", "urn:schemas-microsoft-com:vml");
			imageData.SetAttribute(new OpenXmlAttribute("r", "id", RelationshipsNs, previewRelId));
			embeddedObject.PrependChild(imageData);
		}

		return embeddedObject;
	}

	[Fact]
	public void Parse_RunWithOleObject_ReturnsOleObjectRunElement()
	{
		var oleElement = Parse(CreateOleObject("rId10", previewRelId: ""));

		oleElement.Should().NotBeNull();
	}

	[Fact]
	public void Parse_OleObject_PreservesRelationshipId()
	{
		var oleElement = Parse(CreateOleObject("rIdOle1", previewRelId: ""));

		oleElement.RelationshipId.Should().Be("rIdOle1");
	}

	[Fact]
	public void Parse_OleObjectWithPreviewImage_HasPreviewImageTrue()
	{
		var oleElement = Parse(CreateOleObject("rId5", previewRelId: "rIdPreview"));

		oleElement.HasPreviewImage.Should().BeTrue();
		oleElement.PreviewImageRelationshipId.Should().Be("rIdPreview");
	}

	[Fact]
	public void Parse_OleObjectWithoutPreview_HasPreviewImageFalse()
	{
		var oleElement = Parse(CreateOleObject("rId5", previewRelId: ""));

		oleElement.HasPreviewImage.Should().BeFalse();
		oleElement.PreviewImageRelationshipId.Should().BeEmpty();
	}

	[Fact]
	public void Parse_OleObjectWithDxaDyaOrig_ConvertsToEmu()
	{
		// 1440 twips = 1 inch = 914400 EMU
		var oleElement = Parse(CreateOleObject("rId1", previewRelId: "", size: (1440, 720)));

		oleElement.WidthEmu.Should().Be(914400L);
		oleElement.HeightEmu.Should().Be(457200L);
	}

	[Fact]
	public void Parse_OleObjectWithNoSizeAttributes_UsesDefaultSize()
	{
		var oleElement = Parse(CreateOleObject("rId1", previewRelId: ""));

		oleElement.WidthEmu.Should().Be(1905000L);
		oleElement.HeightEmu.Should().Be(1905000L);
	}
}
