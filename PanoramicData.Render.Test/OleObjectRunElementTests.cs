namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class OleObjectRunElementTests
{
	// -------------------------------------------------------------------------
	// 5.7.1 — Detect OLE embedded objects
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_RunWithOleObject_ReturnsOleObjectRunElement()
	{
		var oleObj = CreateOleObject("rId10", previewRelId: "");
		var run = new Run(oleObj);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<OleObjectRunElement>();
	}

	[Fact]
	public void Parse_OleObject_PreservesRelationshipId()
	{
		var oleObj = CreateOleObject("rIdOle1", previewRelId: "");
		var run = new Run(oleObj);

		var oleElement = (OleObjectRunElement)RunElementParser.Parse(run)[0];

		oleElement.RelationshipId.Should().Be("rIdOle1");
	}

	// -------------------------------------------------------------------------
	// 5.7.2 — Preview image (EMF/WMF) available
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_OleObjectWithPreviewImage_HasPreviewImageTrue()
	{
		var oleObj = CreateOleObject("rId5", previewRelId: "rIdPreview");
		var run = new Run(oleObj);

		var oleElement = (OleObjectRunElement)RunElementParser.Parse(run)[0];

		oleElement.HasPreviewImage.Should().BeTrue();
		oleElement.PreviewImageRelationshipId.Should().Be("rIdPreview");
	}

	// -------------------------------------------------------------------------
	// 5.7.3 — No preview: placeholder model
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_OleObjectWithoutPreview_HasPreviewImageFalse()
	{
		var oleObj = CreateOleObject("rId5", previewRelId: "");
		var run = new Run(oleObj);

		var oleElement = (OleObjectRunElement)RunElementParser.Parse(run)[0];

		oleElement.HasPreviewImage.Should().BeFalse();
		oleElement.PreviewImageRelationshipId.Should().BeEmpty();
	}

	// -------------------------------------------------------------------------
	// Size attributes
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_OleObjectWithDxaDyaOrig_ConvertsToEmu()
	{
		// 1440 twips = 1 inch = 914400 EMU
		var oleObj = CreateOleObjectWithSize("rId1", dxaOrig: 1440, dyaOrig: 720, previewRelId: "");
		var run = new Run(oleObj);

		var oleElement = (OleObjectRunElement)RunElementParser.Parse(run)[0];

		oleElement.WidthEmu.Should().Be(914400L);   // 1440 × 635 = 914400
		oleElement.HeightEmu.Should().Be(457200L);  // 720 × 635 = 457200
	}

	[Fact]
	public void Parse_OleObjectWithNoSizeAttributes_UsesDefaultSize()
	{
		var oleObj = CreateOleObject("rId1", previewRelId: "");
		var run = new Run(oleObj);

		var oleElement = (OleObjectRunElement)RunElementParser.Parse(run)[0];

		oleElement.WidthEmu.Should().Be(1905000L);  // default ~2 inches
		oleElement.HeightEmu.Should().Be(1905000L);
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	private static EmbeddedObject CreateOleObject(string oleRelId, string previewRelId)
	{
		var oleObjElement = new OpenXmlUnknownElement("o", "OLEObject", "urn:schemas-microsoft-com:office:office");
		oleObjElement.SetAttribute(new OpenXmlAttribute("r", "id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", oleRelId));

		var embeddedObject = new EmbeddedObject(oleObjElement);

		if (!string.IsNullOrEmpty(previewRelId))
		{
			var imageData = new OpenXmlUnknownElement("v", "imagedata", "urn:schemas-microsoft-com:vml");
			imageData.SetAttribute(new OpenXmlAttribute("r", "id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", previewRelId));
			embeddedObject.PrependChild(imageData);
		}

		return embeddedObject;
	}

	private static EmbeddedObject CreateOleObjectWithSize(string oleRelId, int dxaOrig, int dyaOrig, string previewRelId)
	{
		var oleObjElement = new OpenXmlUnknownElement("o", "OLEObject", "urn:schemas-microsoft-com:office:office");
		oleObjElement.SetAttribute(new OpenXmlAttribute("r", "id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", oleRelId));

		var embeddedObject = new EmbeddedObject(oleObjElement);
		embeddedObject.SetAttribute(new OpenXmlAttribute("w", "dxaOrig", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", dxaOrig.ToString()));
		embeddedObject.SetAttribute(new OpenXmlAttribute("w", "dyaOrig", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", dyaOrig.ToString()));

		if (!string.IsNullOrEmpty(previewRelId))
		{
			var imageData = new OpenXmlUnknownElement("v", "imagedata", "urn:schemas-microsoft-com:vml");
			imageData.SetAttribute(new OpenXmlAttribute("r", "id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", previewRelId));
			embeddedObject.PrependChild(imageData);
		}

		return embeddedObject;
	}
}
