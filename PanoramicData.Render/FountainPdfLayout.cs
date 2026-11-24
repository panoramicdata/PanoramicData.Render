namespace PanoramicData.Render;

internal readonly record struct FountainPdfLayout(
	FountainRenderProfile Profile,
	float PageWidth,
	float PageHeight,
	float BodyLeft,
	float BodyRight,
	float BodyBottom,
	float CueLeft,
	float DialogueLeft,
	float DialogueRight,
	float ParentheticalLeft,
	float ParentheticalRight,
	float NumberX,
	float NumberY)
{
	public const float FirstBaseline = 81.5f;
	public const float LineHeight = 12;
	public bool IsUkStage => Profile == FountainRenderProfile.BbcStageUk;
	public bool IsUsStage => Profile == FountainRenderProfile.BbcStageUs;
	public bool IsStage => IsUsStage || IsUkStage;
	public bool IsTv => Profile == FountainRenderProfile.BbcTelevision;
	public bool HasMoreCue => Profile is FountainRenderProfile.BbcTelevision or FountainRenderProfile.FeatureScreenplay;

	public static FountainPdfLayout For(FountainRenderProfile profile) => profile switch
	{
		FountainRenderProfile.BbcStageUs => new(profile, 612, 792, 108, 540, 720, 288, 108, 540, 216, 540, 558, 44.8f),
		FountainRenderProfile.BbcStageUk => new(profile, 595, 842, 72, 565, 768, 72, 72, 565, 72, 565, 297.5f, 804),
		FountainRenderProfile.BbcTelevision => new(profile, 612, 792, 108, 540, 707, 252, 180, 432, 216, 396, 518.4f, 44.8f),
		FountainRenderProfile.FeatureScreenplay => new(profile, 612, 792, 108, 540, 720, 266.4f, 180, 432, 223.2f, 396, 540, 36),
		_ => throw new ArgumentOutOfRangeException(nameof(profile))
	};
}

internal sealed record FountainPdfLine(string Text, float X, float Baseline, bool Underline = false);

internal sealed record FountainPdfPage(List<FountainPdfLine> Lines, int? BodyPageNumber);
