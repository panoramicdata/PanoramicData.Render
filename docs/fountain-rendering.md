# Rendering scripts from Fountain

```csharp
using PanoramicData.Render;

var source = File.ReadAllText("stage.fountain");
var renderer = new FountainRenderer();
var pdf = renderer.RenderPdf(source, FountainRenderProfile.BbcStageUs);
File.WriteAllBytes("stage.pdf", pdf);
```

Choose a profile when calling the renderer. The input is a decoded UTF-8
Fountain script. The renderer leaves it untouched. Use the same
[stage script](examples/stage.fountain) with both BBC stage profiles. You can
also render the [teleplay](examples/television.fountain) or
[feature screenplay](examples/feature.fountain).

| Profile | Reference | Layout |
| --- | --- | --- |
| `BbcStageUs` | [BBC US Stage](https://downloads.bbc.co.uk/writersroom/scripts/stageus.pdf) | US Letter, separate act and scene rows, bracketed directions in the source case, indented character names above dialogue, body numbering in the header |
| `BbcStageUk` | [BBC UK Stage](https://downloads.bbc.co.uk/writersroom/scripts/stage.pdf) | A4, act and scene on one row, uppercase directions, dialogue on the same row as the cue, body numbering in the footer |
| `BbcTelevision` | [BBC Television Screenplay](https://downloads.bbc.co.uk/writersroom/scripts/screenplaytv.pdf) | US Letter, visible act boundaries with new-page act starts, indented dialogue and parentheticals, body numbering from page one |
| `FeatureScreenplay` | [Academy Nicholl format guide](https://www.oscars.org/sites/oscars/files/scriptsample.pdf) | US Letter, 12-point monospaced type, indented character cues and dialogue, 1.5-inch left margin, top-right numbering from page two |

The BBC samples provide layout references. Submission rules for a particular
programme or opportunity may include other requirements.

## Source mapping

- Use [Fountain syntax](https://fountain.io/syntax/) for title metadata, scenes,
  dialogue, parentheticals and transitions. Title-page fields include
  `Title:`, `Credit:`, `Author:` or `Authors:`, `Source:`, `Contact:`
  and `Draft date:`.
- Leave a blank line between elements. A character cue is followed directly by
  dialogue or a parenthetical. Use an uppercase cue or Fountain's `@` marker.
- Use action paragraphs for stage directions. Use Fountain's `!` marker
  if the paragraph would otherwise be parsed as another element. The stage
  profile applies its own brackets, case, indentation and spacing.
- Use a forced heading such as `.SCENE 1` for plays, with a blank line before
  and after it. Use conventional `INT.` or `EXT.` headings for film and TV.
- For a visible act marker, use centered text such as `>ACT I<`,
  `>END OF ACT I<`, `>ACT ONE<` or `>END OF ACT ONE<`. Other Fountain
  readers can display these as centered text. The stage profiles position the
  act next to or above the scene heading. The TV profile also recognises
  teaser, cold open, and tag headings. The feature profile renders authored
  act labels as ordinary centered text. Fountain sections marked with `#` and
  synopses marked with `=` are outlines and are omitted from PDFs.
- Optional stage front matter uses standalone centered `>CHARACTERS<` or
  `>CAST<`, `>SETTING<` and `>TIME<` before the first act or scene. Follow each
  with action paragraphs. Use `NAME - Description` for a cast entry.
  US stage prints cast and setting/time on separate unnumbered pages. UK stage
  groups supplied front matter on an unnumbered page. The first scene starts
  body page one in either profile.
- A standard `===` remains an explicit page break. The renderer generates
  scene and act breaks, page numbers, and dialogue continuation cues as
  appropriate to the profile. US stage and television prefer dialogue breaks
  at complete sentences and keep parentheticals with the next spoken line.
  A sentence longer than a page continues across pages.
- Fountain notes and boneyards are omitted. Film and TV render dual dialogue
  in adjacent columns when both parts fit on a page. Longer pairs render as
  consecutive speeches. Inline emphasis currently displays as plain text.

Use Courier New or another installed fixed-width font for reference layout.
Provide fonts through `RenderOptions.FontDirectories` or the host system.
`RenderOptions.PageRange` selects already paginated pages and preserves their
original numbers.


## Interoperability check

Run the independent parser fixture check from the repository root after
installing `fountain-js@1.2.4` into a temporary directory:

```sh
npm install --prefix <temp-directory> fountain-js@1.2.4
NODE_PATH=<temp-directory>/node_modules node scripts/test-fountain-interop.cjs
```
