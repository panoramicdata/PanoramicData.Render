namespace PanoramicData.Render;

internal enum FountainBlockKind
{
	Action,
	Scene,
	Dialogue,
	Centered,
	Transition,
	PageBreak,
	Lyric
}

internal sealed record FountainSpeechLine(string Text, bool Parenthetical);

internal sealed record FountainBlock(
	FountainBlockKind Kind,
	string Text,
	IReadOnlyList<FountainSpeechLine>? Speech = null,
	bool DualDialogue = false);

internal sealed record FountainScript(
	IReadOnlyDictionary<string, string> TitlePage,
	IReadOnlyList<FountainBlock> Blocks);
