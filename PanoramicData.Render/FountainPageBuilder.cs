namespace PanoramicData.Render;

internal sealed partial class FountainPageBuilder(
	FountainPdfLayout layout,
	FountainLineFormatter formatter,
	CancellationToken cancellationToken)
{
	private const float _lineHeight = FountainPdfLayout.LineHeight;
	private readonly List<FountainPdfPage> _pages = [];
	private float _baseline = FountainPdfLayout.FirstBaseline;
	private int _pageNumber;
	private string? _act;
	private string? _previousSpeaker;
	private bool _atFirstStageAction;
	private bool _actionSinceSpeech;

	private FountainPdfPage Page => _pages[^1];

	public IReadOnlyList<FountainPdfPage> Build(FountainScript script)
	{
		AddTitlePage(script.TitlePage);
		var firstBodyBlock = layout.IsStage ? AddStageFrontMatter(script.Blocks) : 0;
		if (firstBodyBlock == script.Blocks.Count && _pages.Count > 0)
		{
			return _pages;
		}

		NewPage(numbered: true);
		if (layout.IsTv && script.TitlePage.GetValueOrDefault("Title") is { Length: > 0 } title && firstBodyBlock < script.Blocks.Count)
		{
			AddCentered(title.ToUpperInvariant(), underline: true, after: 2);
		}

		for (var index = firstBodyBlock; index < script.Blocks.Count; index++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			AddBlock(script.Blocks, ref index);
		}

		return _pages;
	}

	private void AddBlock(IReadOnlyList<FountainBlock> blocks, ref int index)
	{
		var block = blocks[index];
		var next = blocks.ElementAtOrDefault(index + 1);
		switch (block.Kind)
		{
			case FountainBlockKind.PageBreak:
				BreakBodyPage();
				break;
			case FountainBlockKind.Centered:
				AddCenteredBlock(block.Text, next, index == 0);
				break;
			case FountainBlockKind.Scene:
				AddScene(block.Text);
				break;
			case FountainBlockKind.Dialogue:
				index += AddDialogueBlock(block, next);
				break;
			case FountainBlockKind.Transition:
				AddTransition(block.Text);
				break;
			case FountainBlockKind.Lyric:
			case FountainBlockKind.Action:
				AddAction(block.Text);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(blocks));
		}
	}

	private int AddDialogueBlock(FountainBlock block, FountainBlock? next)
	{
		if (!layout.IsStage && next is { Kind: FountainBlockKind.Dialogue, DualDialogue: true })
		{
			AddDualDialogue(block, next);
			return 1;
		}

		AddDialogue(block);
		return 0;
	}
}
