namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SkiaSharp;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class MediaStoreTests
{
	private static readonly byte[] TestPngBytes = CreateMinimalPng();

	[Fact]
	public void TryGetImage_WithValidRelationshipId_ReturnsImageData()
	{
		using var stream = CreateDocxWithEmbeddedImage(out var relationshipId);
		using var doc = DocxDocument.Load(stream);
		var store = new MediaStore(doc);

		var result = store.TryGetImage(relationshipId, out var imageData);

		result.Should().BeTrue();
		imageData.Should().NotBeNull();
		imageData!.Data.Should().NotBeEmpty();
		imageData.ContentType.Should().Be("image/png");
	}

	[Fact]
	public void TryGetImage_WithInvalidRelationshipId_ReturnsFalse()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);
		var store = new MediaStore(doc);

		var result = store.TryGetImage("rIdNonExistent", out var imageData);

		result.Should().BeFalse();
		imageData.Should().BeNull();
	}

	[Fact]
	public void TryGetImage_CachesResultOnSecondCall()
	{
		using var stream = CreateDocxWithEmbeddedImage(out var relationshipId);
		using var doc = DocxDocument.Load(stream);
		var store = new MediaStore(doc);

		store.TryGetImage(relationshipId, out var first);
		store.TryGetImage(relationshipId, out var second);

		first.Should().BeSameAs(second);
	}

	[Fact]
	public void TryGetImage_WithEmptyRelationshipId_ReturnsFalse()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);
		var store = new MediaStore(doc);

		var result = store.TryGetImage(string.Empty, out var imageData);

		result.Should().BeFalse();
		imageData.Should().BeNull();
	}

	[Fact]
	public void TryGetImage_WithNullRelationshipId_ReturnsFalse()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);
		var store = new MediaStore(doc);

		var result = store.TryGetImage(null!, out var imageData);

		result.Should().BeFalse();
		imageData.Should().BeNull();
	}

	[Fact]
	public void GetAllRelationshipIds_WithImages_ReturnsIds()
	{
		using var stream = CreateDocxWithEmbeddedImage(out var relationshipId);
		using var doc = DocxDocument.Load(stream);
		var store = new MediaStore(doc);

		var ids = store.GetImagePartRelationshipIds();

		ids.Should().Contain(relationshipId);
	}

	[Fact]
	public void GetAllRelationshipIds_WithoutImages_ReturnsEmpty()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);
		var store = new MediaStore(doc);

		var ids = store.GetImagePartRelationshipIds();

		ids.Should().BeEmpty();
	}

	[Fact]
	public void Constructor_NullDocument_ThrowsArgumentNullException()
	{
		Action act = () => new MediaStore(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ImageData_HasCorrectDataLength()
	{
		using var stream = CreateDocxWithEmbeddedImage(out var relationshipId);
		using var doc = DocxDocument.Load(stream);
		var store = new MediaStore(doc);

		store.TryGetImage(relationshipId, out var imageData);

		imageData!.Data.Length.Should().Be(TestPngBytes.Length);
	}

	[Theory]
	[MemberData(nameof(SupportedImagePartTypes))]
	public void TryGetImage_SupportedImageFormats_ReturnsImageData(PartTypeInfo imagePartType)
	{
		var payload = new byte[] { 0x01, 0x02, 0x03, 0x04 };
		using var stream = CreateDocxWithEmbeddedImage(imagePartType, payload, out var relationshipId, out var expectedContentType);
		using var doc = DocxDocument.Load(stream);
		var store = new MediaStore(doc);

		var result = store.TryGetImage(relationshipId, out var imageData);

		result.Should().BeTrue();
		imageData.Should().NotBeNull();
		imageData!.Data.Should().Equal(payload);
		imageData.ContentType.Should().Be(expectedContentType);
	}

	[Fact]
	public void TryGetImage_WmfWithDecodableBytes_RasterizesToPng()
	{
		var decodableBytes = CreateDecodablePng();
		using var stream = CreateDocxWithEmbeddedImage(ImagePartType.Wmf, decodableBytes, out var relationshipId, out _);
		using var doc = DocxDocument.Load(stream);
		var store = new MediaStore(doc);

		var result = store.TryGetImage(relationshipId, out var imageData);

		result.Should().BeTrue();
		imageData.Should().NotBeNull();
		imageData!.ContentType.Should().Be("image/png");
		imageData.Data.Should().NotBeEmpty();
	}

	[Fact]
	public void TryGetImage_WmfWithInvalidBytes_LeavesOriginalContentType()
	{
		var invalidBytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
		using var stream = CreateDocxWithEmbeddedImage(ImagePartType.Wmf, invalidBytes, out var relationshipId, out var expectedContentType);
		using var doc = DocxDocument.Load(stream);
		var store = new MediaStore(doc);

		var result = store.TryGetImage(relationshipId, out var imageData);

		result.Should().BeTrue();
		imageData.Should().NotBeNull();
		imageData!.ContentType.Should().Be(expectedContentType);
		imageData.Data.Should().Equal(invalidBytes);
	}

	public static TheoryData<PartTypeInfo> SupportedImagePartTypes =>
	[
		ImagePartType.Jpeg,
		ImagePartType.Png,
		ImagePartType.Gif,
		ImagePartType.Bmp,
		ImagePartType.Tiff,
		ImagePartType.Wmf,
		ImagePartType.Emf
	];

	private static MemoryStream CreateDocxWithEmbeddedImage(out string relationshipId)
	{
		return CreateDocxWithEmbeddedImage(ImagePartType.Png, TestPngBytes, out relationshipId, out _);
	}

	private static MemoryStream CreateDocxWithEmbeddedImage(
		PartTypeInfo imagePartType,
		byte[] imageBytes,
		out string relationshipId,
		out string expectedContentType)
	{
		var stream = new MemoryStream();
		string relId;
		string contentType;
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();

			var imagePart = mainPart.AddImagePart(imagePartType);
			using (var imgStream = new MemoryStream(imageBytes))
			{
				imagePart.FeedData(imgStream);
			}

			relId = mainPart.GetIdOfPart(imagePart);
			contentType = imagePart.ContentType;

			var blip = new A.Blip { Embed = relId };
			var blipFill = new A.Pictures.BlipFill(blip);
			var pic = new A.Pictures.Picture(
				new A.Pictures.NonVisualPictureProperties(
					new A.Pictures.NonVisualDrawingProperties { Id = 1, Name = "test-image" },
					new A.Pictures.NonVisualPictureDrawingProperties()),
				blipFill,
				new A.Pictures.ShapeProperties());
			var graphicData = new A.GraphicData(pic)
			{
				Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
			};
			var graphic = new A.Graphic(graphicData);
			var inline = new DW.Inline(
				new DW.Extent { Cx = 914400, Cy = 914400 },
				graphic)
			{
				DistanceFromTop = 0,
				DistanceFromBottom = 0,
				DistanceFromLeft = 0,
				DistanceFromRight = 0
			};
			var drawing = new Drawing(inline);

			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(drawing))));
		}

		stream.Position = 0;
		relationshipId = relId;
		expectedContentType = contentType;
		return stream;
	}

	private static byte[] CreateMinimalPng()
	{
		// Minimal valid 1x1 transparent PNG
		return
		[
			0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
			0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
			0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1
			0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, // RGBA
			0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, // IDAT chunk
			0x54, 0x78, 0x9C, 0x62, 0x00, 0x00, 0x00, 0x02,
			0x00, 0x01, 0xE5, 0x27, 0xDE, 0xFC, 0x00, 0x00, // IEND chunk
			0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42,
			0x60, 0x82
		];
	}

	private static byte[] CreateDecodablePng()
	{
		using var bitmap = new SKBitmap(1, 1);
		bitmap.SetPixel(0, 0, SKColors.Transparent);
		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
		return data!.ToArray();
	}
}
