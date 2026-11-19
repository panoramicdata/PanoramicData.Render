namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SkiaSharp;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class MediaStoreTests
{
	private static readonly byte[] TestPngBytes = CreateMinimalPng();

	private sealed class StoreFixture : IDisposable
	{
		private readonly MemoryStream _stream;
		private readonly DocxDocument _document;

		private StoreFixture(MemoryStream stream, string relationshipId, string contentType)
		{
			_stream = stream;
			_document = DocxDocument.Load(stream);
			Store = new MediaStore(_document);
			RelationshipId = relationshipId;
			ExpectedContentType = contentType;
		}

		public MediaStore Store { get; }

		public string RelationshipId { get; }

		public string ExpectedContentType { get; }

		public static StoreFixture Empty() => new(TestDocxBuilder.CreateMinimalDocx(), string.Empty, string.Empty);

		public static StoreFixture WithImage(PartTypeInfo? partType = null, byte[]? bytes = null)
		{
			var stream = CreateDocxWithEmbeddedImage(partType ?? ImagePartType.Png, bytes ?? TestPngBytes, out var relationshipId, out var contentType);
			return new StoreFixture(stream, relationshipId, contentType);
		}

		public ImageData? Get()
		{
			Store.TryGetImage(RelationshipId, out var imageData).Should().BeTrue();
			return imageData;
		}

		public void Dispose()
		{
			_document.Dispose();
			_stream.Dispose();
		}
	}

	[Fact]
	public void TryGetImage_WithValidRelationshipId_ReturnsImageData()
	{
		using var fixture = StoreFixture.WithImage();

		var result = fixture.Store.TryGetImage(fixture.RelationshipId, out var imageData);

		result.Should().BeTrue();
		imageData.Should().NotBeNull();
		imageData!.Data.Should().NotBeEmpty();
		imageData.ContentType.Should().Be("image/png");
	}

	[Theory]
	[InlineData("rIdNonExistent")]
	[InlineData("")]
	[InlineData(null)]
	public void TryGetImage_WithUnusableRelationshipId_ReturnsFalse(string? relationshipId)
	{
		using var fixture = StoreFixture.Empty();

		var result = fixture.Store.TryGetImage(relationshipId!, out var imageData);

		result.Should().BeFalse();
		imageData.Should().BeNull();
	}

	[Fact]
	public void TryGetImage_CachesResultOnSecondCall()
	{
		using var fixture = StoreFixture.WithImage();

		var first = fixture.Get();
		var second = fixture.Get();

		first.Should().BeSameAs(second);
	}

	[Fact]
	public void GetAllRelationshipIds_WithImages_ReturnsIds()
	{
		using var fixture = StoreFixture.WithImage();

		fixture.Store.GetImagePartRelationshipIds().Should().Contain(fixture.RelationshipId);
	}

	[Fact]
	public void GetAllRelationshipIds_WithoutImages_ReturnsEmpty()
	{
		using var fixture = StoreFixture.Empty();

		fixture.Store.GetImagePartRelationshipIds().Should().BeEmpty();
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
		using var fixture = StoreFixture.WithImage();

		fixture.Get()!.Data.Length.Should().Be(TestPngBytes.Length);
	}

	[Theory]
	[MemberData(nameof(SupportedImagePartTypes))]
	public void TryGetImage_SupportedImageFormats_ReturnsImageData(PartTypeInfo imagePartType)
	{
		var payload = new byte[] { 0x01, 0x02, 0x03, 0x04 };
		using var fixture = StoreFixture.WithImage(imagePartType, payload);

		var imageData = fixture.Get();

		imageData.Should().NotBeNull();
		imageData!.Data.Should().Equal(payload);
		imageData.ContentType.Should().Be(fixture.ExpectedContentType);
	}

	[Fact]
	public void TryGetImage_WmfWithDecodableBytes_RasterizesToPng()
	{
		using var fixture = StoreFixture.WithImage(ImagePartType.Wmf, CreateDecodablePng());

		var imageData = fixture.Get();

		imageData.Should().NotBeNull();
		imageData!.ContentType.Should().Be("image/png");
		imageData.Data.Should().NotBeEmpty();
	}

	[Fact]
	public void TryGetImage_WmfWithInvalidBytes_LeavesOriginalContentType()
	{
		var invalidBytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
		using var fixture = StoreFixture.WithImage(ImagePartType.Wmf, invalidBytes);

		var imageData = fixture.Get();

		imageData.Should().NotBeNull();
		imageData!.ContentType.Should().Be(fixture.ExpectedContentType);
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

	private static MemoryStream CreateDocxWithEmbeddedImage(
		PartTypeInfo imagePartType,
		byte[] imageBytes,
		out string relationshipId,
		out string expectedContentType)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();

			var imagePart = mainPart.AddImagePart(imagePartType);
			using (var imgStream = new MemoryStream(imageBytes))
			{
				imagePart.FeedData(imgStream);
			}

			relationshipId = mainPart.GetIdOfPart(imagePart);
			expectedContentType = imagePart.ContentType;

			var pic = new A.Pictures.Picture(
				new A.Pictures.NonVisualPictureProperties(
					new A.Pictures.NonVisualDrawingProperties { Id = 1, Name = "test-image" },
					new A.Pictures.NonVisualPictureDrawingProperties()),
				new A.Pictures.BlipFill(new A.Blip { Embed = relationshipId }),
				new A.Pictures.ShapeProperties());
			var graphic = new A.Graphic(new A.GraphicData(pic)
			{
				Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
			});
			var drawing = ShapeDrawingBuilder.InlineOf(graphic, 914400, 914400);

			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(drawing))));
		}

		stream.Position = 0;
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
