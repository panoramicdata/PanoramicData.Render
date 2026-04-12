namespace PanoramicData.Render;

/// <summary>
/// Configuration for the optional field-update pass executed before final rendering.
/// </summary>
public sealed record FieldUpdateOptions
{
	private int _maxIterations = 3;

	/// <summary>
	/// Gets or sets a value indicating whether <c>PAGE</c> and <c>NUMPAGES</c> fields should be updated.
	/// </summary>
	public bool UpdatePageFields { get; init; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether document property fields should be updated.
	/// </summary>
	public bool UpdateDocumentProperties { get; init; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether table of contents fields should be updated.
	/// </summary>
	public bool UpdateTableOfContents { get; init; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether table of figures fields should be updated.
	/// </summary>
	public bool UpdateTableOfFigures { get; init; } = true;

	/// <summary>
	/// Gets or sets the maximum number of field-update/layout iterations to perform.
	/// </summary>
	/// <value>A positive integer greater than or equal to 1.</value>
	public int MaxIterations
	{
		get => _maxIterations;
		init
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
			_maxIterations = value;
		}
	}
}