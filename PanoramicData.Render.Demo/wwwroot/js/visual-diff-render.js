// Part of the visual diff helper; composed by visual-diff.js.
window.visualDiffParts = window.visualDiffParts ?? {};
window.visualDiffParts.render = {
	/** Lazy-load pdf.js (ES module via dynamic import) */
	async _ensurePdfJs() {
		if (this._pdfjs) return this._pdfjs;
		const pdfjs = await import(
			"https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.9.155/pdf.min.mjs"
		);
		pdfjs.GlobalWorkerOptions.workerSrc =
			"https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.9.155/pdf.worker.min.mjs";
		this._pdfjs = pdfjs;
		return pdfjs;
	},
	/**
	 * Renders all pages of a PDF (given as Base64) to PNG data-URIs.
	 * @param {string} base64Pdf  Base64-encoded PDF bytes
	 * @param {number} scale      Render scale (1 = 72 dpi, 2 = 144 dpi, ...)
	 * @returns {Promise<string[]>} Array of PNG data-URIs, one per page
	 */
	async renderPdfPages(base64Pdf, scale) {
		const pdfjs = await this._ensurePdfJs();
		const raw = Uint8Array.from(atob(base64Pdf), (c) => c.charCodeAt(0));
		const doc = await pdfjs.getDocument({ data: raw }).promise;
		const results = [];

		for (let i = 1; i <= doc.numPages; i++) {
			const page = await doc.getPage(i);
			const vp = page.getViewport({ scale });
			const canvas = this._createCanvas(
				Math.ceil(vp.width),
				Math.ceil(vp.height),
			);
			const ctx = canvas.getContext("2d");
			await page.render({ canvasContext: ctx, viewport: vp }).promise;
			const blob = await this._canvasToBlob(canvas);
			results.push(await this._blobToDataUri(blob));
		}

		doc.destroy();
		return results;
	},
	/**
	 * Rasterises an inline SVG element to a PNG data-URI at the specified pixel size.
	 * @param {string} containerId  The id of the element whose first child is the <svg>
	 * @param {number} width        Target width in px
	 * @param {number} height       Target height in px
	 * @returns {Promise<string>}   PNG data-URI
	 */
	async rasteriseSvg(containerId, width, height) {
		const container = document.getElementById(containerId);
		if (!container) throw new Error(`Element #${containerId} not found`);
		const svg = container.querySelector("svg");
		if (!svg) throw new Error(`No <svg> inside #${containerId}`);

		const exportSvg = svg.cloneNode(true);
		exportSvg.setAttribute("width", String(width));
		exportSvg.setAttribute("height", String(height));
		if (!exportSvg.getAttribute("viewBox")) {
			exportSvg.setAttribute("viewBox", `0 0 ${width} ${height}`);
		}
		exportSvg.setAttribute(
			"style",
			`display:block;width:${width}px;height:${height}px`,
		);

		const serializer = new XMLSerializer();
		const svgString = serializer.serializeToString(exportSvg);
		const blob = new Blob([svgString], {
			type: "image/svg+xml;charset=utf-8",
		});
		const url = URL.createObjectURL(blob);

		try {
			const img = new Image();
			img.width = width;
			img.height = height;
			await new Promise((resolve, reject) => {
				img.onload = resolve;
				img.onerror = reject;
				img.src = url;
			});

			const canvas = this._createCanvas(width, height);
			const ctx = canvas.getContext("2d");
			ctx.fillStyle = "#fff";
			ctx.fillRect(0, 0, width, height);
			ctx.drawImage(img, 0, 0, width, height);
			const outBlob = await this._canvasToBlob(canvas);
			return await this._blobToDataUri(outBlob);
		} finally {
			URL.revokeObjectURL(url);
		}
	},
};
