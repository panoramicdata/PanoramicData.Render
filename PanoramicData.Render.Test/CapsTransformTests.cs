using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public class CapsModeTests
{
	[Fact]
	public void None_HasValue_Zero()
	{
		((int)CapsMode.None).Should().Be(0);
	}

	[Fact]
	public void EnumCount_Is3()
	{
		Enum.GetValues<CapsMode>().Should().HaveCount(3);
	}

	[Theory]
	[InlineData((int)CapsMode.None)]
	[InlineData((int)CapsMode.AllCaps)]
	[InlineData((int)CapsMode.SmallCaps)]
	public void AllValues_AreDefined(int value)
	{
		Enum.IsDefined((CapsMode)value).Should().BeTrue();
	}
}

public class CapsTransformTests
{
	// --- DefaultSmallCapsScale ---

	[Fact]
	public void DefaultSmallCapsScale_Is80Percent()
	{
		CapsTransform.DefaultSmallCapsScale.Should().Be(0.8f);
	}

	// --- Resolve ---

	[Fact]
	public void Resolve_NeitherActive_ReturnsNone()
	{
		CapsTransform.Resolve(caps: false, smallCaps: false).Should().Be(CapsMode.None);
	}

	[Fact]
	public void Resolve_CapsOnly_ReturnsAllCaps()
	{
		CapsTransform.Resolve(caps: true, smallCaps: false).Should().Be(CapsMode.AllCaps);
	}

	[Fact]
	public void Resolve_SmallCapsOnly_ReturnsSmallCaps()
	{
		CapsTransform.Resolve(caps: false, smallCaps: true).Should().Be(CapsMode.SmallCaps);
	}

	[Fact]
	public void Resolve_BothActive_AllCapsTakesPrecedence()
	{
		CapsTransform.Resolve(caps: true, smallCaps: true).Should().Be(CapsMode.AllCaps);
	}

	// --- TransformText ---

	[Fact]
	public void TransformText_None_ReturnsOriginal()
	{
		CapsTransform.TransformText("Hello World", CapsMode.None).Should().Be("Hello World");
	}

	[Fact]
	public void TransformText_AllCaps_ReturnsUppercase()
	{
		CapsTransform.TransformText("Hello World", CapsMode.AllCaps).Should().Be("HELLO WORLD");
	}

	[Fact]
	public void TransformText_SmallCaps_ReturnsUppercase()
	{
		CapsTransform.TransformText("Hello World", CapsMode.SmallCaps).Should().Be("HELLO WORLD");
	}

	[Fact]
	public void TransformText_AlreadyUppercase_NoChange()
	{
		CapsTransform.TransformText("ABC", CapsMode.AllCaps).Should().Be("ABC");
	}

	[Fact]
	public void TransformText_EmptyString_ReturnsEmpty()
	{
		CapsTransform.TransformText(string.Empty, CapsMode.AllCaps).Should().BeEmpty();
	}

	[Fact]
	public void TransformText_NullText_ThrowsArgumentNullException()
	{
		var act = () => CapsTransform.TransformText(null!, CapsMode.None);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void TransformText_UnknownMode_ReturnsOriginal()
	{
		CapsTransform.TransformText("test", (CapsMode)999).Should().Be("test");
	}

	// --- ComputeCharacterFontSize ---

	[Fact]
	public void ComputeCharSize_None_ReturnsParentSize()
	{
		CapsTransform.ComputeCharacterFontSize('a', 12f, CapsMode.None).Should().Be(12f);
	}

	[Fact]
	public void ComputeCharSize_AllCaps_ReturnsParentSize()
	{
		CapsTransform.ComputeCharacterFontSize('a', 12f, CapsMode.AllCaps).Should().Be(12f);
	}

	[Fact]
	public void ComputeCharSize_SmallCaps_LowercaseChar_ReturnsScaledSize()
	{
		var result = CapsTransform.ComputeCharacterFontSize('a', 12f, CapsMode.SmallCaps);

		result.Should().BeApproximately(9.6f, 0.01f);
	}

	[Fact]
	public void ComputeCharSize_SmallCaps_UppercaseChar_ReturnsParentSize()
	{
		CapsTransform.ComputeCharacterFontSize('A', 12f, CapsMode.SmallCaps).Should().Be(12f);
	}

	[Fact]
	public void ComputeCharSize_SmallCaps_Digit_ReturnsParentSize()
	{
		CapsTransform.ComputeCharacterFontSize('5', 12f, CapsMode.SmallCaps).Should().Be(12f);
	}

	[Fact]
	public void ComputeCharSize_SmallCaps_Space_ReturnsParentSize()
	{
		CapsTransform.ComputeCharacterFontSize(' ', 12f, CapsMode.SmallCaps).Should().Be(12f);
	}

	[Fact]
	public void ComputeCharSize_SmallCaps_CustomScale()
	{
		var result = CapsTransform.ComputeCharacterFontSize('a', 12f, CapsMode.SmallCaps, smallCapsScale: 0.7f);

		result.Should().BeApproximately(8.4f, 0.01f);
	}

	[Fact]
	public void ComputeCharSize_AllCaps_UppercaseChar_ReturnsParentSize()
	{
		CapsTransform.ComputeCharacterFontSize('Z', 12f, CapsMode.AllCaps).Should().Be(12f);
	}
}
