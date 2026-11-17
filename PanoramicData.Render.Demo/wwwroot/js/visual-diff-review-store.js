// Part of the visual diff helper; composed by visual-diff.js.
window.visualDiffParts = window.visualDiffParts ?? {};
window.visualDiffParts.reviewStore = {
	reviewStore: {
		_prefix: "panoramic.render.review",
		_key(docKey, pageNumber) {
			return `${this._prefix}::${docKey}::page-${pageNumber}`;
		},
		getPage(docKey, pageNumber) {
			let page = null;
			try {
				const raw = localStorage.getItem(this._key(docKey, pageNumber));
				page = raw ? JSON.parse(raw) : null;
			} catch (error) {
				console.debug("Review store unavailable.", error);
			}

			return page;
		},
		setPage(docKey, pageNumber, payload) {
			try {
				localStorage.setItem(
					this._key(docKey, pageNumber),
					JSON.stringify(payload),
				);
			} catch (error) {
				console.debug("Review store unavailable.", error);
			}
		},
		removePage(docKey, pageNumber) {
			try {
				localStorage.removeItem(this._key(docKey, pageNumber));
			} catch (error) {
				console.debug("Review store unavailable.", error);
			}
		},
	},
};
