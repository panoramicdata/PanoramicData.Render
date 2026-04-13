namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Extracts and deobfuscates embedded fonts from a DOCX document's font table part.
/// </summary>
/// <remarks>
/// OOXML embedded fonts use a simple XOR-based obfuscation (ECMA-376, Part 2, §14.2.1).
/// The first 32 bytes of the font data are XOR'd with a key derived from the font's GUID.
/// </remarks>
internal static class DocxFontExtractor
{
	/// <summary>
	/// Extracts all embedded fonts from a DOCX document's main document part.
	/// Returns a dictionary mapping font family name and style suffix to deobfuscated font bytes.
	/// Keys are formatted as <c>"FamilyName"</c> for regular, <c>"FamilyName Bold"</c>, etc.
	/// </summary>
	/// <param name="mainDocumentPart">The main document part of the DOCX document.</param>
	/// <returns>A dictionary of font family+style keys to raw TTF/OTF font bytes.</returns>
	public static Dictionary<string, byte[]> Extract(MainDocumentPart mainDocumentPart)
	{
		ArgumentNullException.ThrowIfNull(mainDocumentPart);

		var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
		var fontTablePart = mainDocumentPart.FontTablePart;
		if (fontTablePart is null)
		{
			return result;
		}

		var fonts = fontTablePart.Fonts;
		if (fonts is null)
		{
			return result;
		}

		foreach (var font in fonts.Elements<Font>())
		{
			var familyName = font.Name?.Value;
			if (string.IsNullOrWhiteSpace(familyName))
			{
				continue;
			}

			TryExtractVariant(fontTablePart, font.EmbedRegularFont, familyName, result);
			TryExtractVariant(fontTablePart, font.EmbedBoldFont, $"{familyName} Bold", result);
			TryExtractVariant(fontTablePart, font.EmbedItalicFont, $"{familyName} Italic", result);
			TryExtractVariant(fontTablePart, font.EmbedBoldItalicFont, $"{familyName} BoldItalic", result);
		}

		return result;
	}

	private static void TryExtractVariant(
		FontTablePart fontTablePart,
		FontRelationshipType? embedElement,
		string key,
		Dictionary<string, byte[]> result)
	{
		if (embedElement is null)
		{
			return;
		}

		var fontKeyHex = embedElement.FontKey?.Value;
		var relationshipId = embedElement.Id?.Value;
		if (string.IsNullOrWhiteSpace(fontKeyHex) || string.IsNullOrWhiteSpace(relationshipId))
		{
			return;
		}

		try
		{
			var part = fontTablePart.GetPartById(relationshipId);
			using var stream = part.GetStream(FileMode.Open, FileAccess.Read);
			using var ms = new MemoryStream();
			stream.CopyTo(ms);
			var fontData = ms.ToArray();

			Deobfuscate(fontData, fontKeyHex);
			result[key] = fontData;
		}
		catch
		{
			// Best-effort: skip fonts we can't read
		}
	}

	/// <summary>
	/// Deobfuscates OOXML embedded font data in-place.
	/// Per ECMA-376 Part 2 §14.2.1: parse the fontKey GUID hex digits into 16 bytes,
	/// reverse the byte order, then XOR the first 32 bytes of the font data.
	/// </summary>
	private static void Deobfuscate(byte[] data, string fontKeyHex)
	{
		// Strip non-hex characters from the GUID string (braces, hyphens)
		Span<byte> key = stackalloc byte[16];
		var hexIndex = 0;
		for (var i = 0; i < fontKeyHex.Length && hexIndex < 32; i++)
		{
			var c = fontKeyHex[i];
			if (IsHexDigit(c))
			{
				var nibble = HexToNibble(c);
				if (hexIndex % 2 == 0)
				{
					key[hexIndex / 2] = (byte)(nibble << 4);
				}
				else
				{
					key[hexIndex / 2] |= nibble;
				}

				hexIndex++;
			}
		}

		if (hexIndex < 32)
		{
			return; // Invalid GUID, not enough hex digits
		}

		// Reverse the 16-byte key per OOXML spec
		key.Reverse();

		// XOR the first 32 bytes with the reversed key (cycling through 16 bytes)
		var limit = Math.Min(32, data.Length);
		for (var i = 0; i < limit; i++)
		{
			data[i] ^= key[i % 16];
		}
	}

	private static bool IsHexDigit(char c)
		=> c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');

	private static byte HexToNibble(char c) => c switch
	{
		>= '0' and <= '9' => (byte)(c - '0'),
		>= 'a' and <= 'f' => (byte)(c - 'a' + 10),
		>= 'A' and <= 'F' => (byte)(c - 'A' + 10),
		_ => 0
	};
}
