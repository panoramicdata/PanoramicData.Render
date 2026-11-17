// Visual diff helper: PDF->PNG, SVG->PNG rasterisation, and pixel-level comparison.
// All heavy work stays on an offscreen canvas so the UI thread is not blocked by large pages.
// Composed from the visual-diff-*.js parts, which must be loaded first.

const visualDiffParts = window.visualDiffParts;

window.visualDiff = Object.assign({ _pdfjs: null }, visualDiffParts.canvas, visualDiffParts.render, visualDiffParts.compare, visualDiffParts.reviewStore);

/** Focus a Blazor ElementReference so the browser gives it keyboard focus. */
window.focusElement = function (element) {
	if (element) {
		element.focus();
	}
};
