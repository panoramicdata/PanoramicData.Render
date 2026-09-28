# Test font

`LiberationSans-Regular.ttf` is **Liberation Sans Regular, version 2.1.5**, taken unmodified from the
official upstream release: `liberation-fonts-ttf-2.1.5.tar.gz` on
<https://github.com/liberationfonts/liberation-fonts/releases/tag/2.1.5>
(archive SHA-256 `7191c669bf38899f73a2094ed00f7b800553364f90e2637010a69c0e268f25d0`,
font SHA-256 `76d04c18ea243f426b7de1f3ad208e927008f961dc5945e5aad352d0dfde8ee8`).

It is licensed under the **SIL Open Font License, Version 1.1**, which permits the font to be bundled
and redistributed with software, including in a public repository, provided the licence travels with
it. [`LICENSE`](LICENSE) is the upstream licence file, verbatim.

## Why it is here

The measurement, shaping, line-breaking and text-box tests need a real typeface. They used to take
one from the machine: the first `.ttf` found under the system font directories, or Arial by family
name, skipping themselves when Arial was absent. That made the results depend on the machine, and on
the CI runner - whose image has no fonts and no fontconfig at all - about 130 of them were either
excluded (`RequiresSystemFonts`) or skipped. They now all load this file through `TestFonts.cs`, so
every machine measures with the same font and they all run in CI.

Liberation Sans is metrically compatible with Arial (identical advance widths), which is why it was
chosen: the tests that were written against Arial keep their meaning. Tests that ask for "Arial" by
family name get it through a `FontResolver` substitution, `Arial` -> `Liberation Sans`.

Only the Regular face is included, because no test needs a bold or italic font file.
