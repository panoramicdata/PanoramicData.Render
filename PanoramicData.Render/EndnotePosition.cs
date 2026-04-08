namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Specifies where endnotes are collected and rendered.
/// </summary>
internal enum EndnotePlacement
{
	/// <summary>
	/// Endnotes are collected and rendered at the end of the document (Word default).
	/// </summary>
	DocumentEnd = 0,

	/// <summary>
	/// Endnotes are collected and rendered at the end of each section.
	/// </summary>
	SectionEnd = 1,
}
