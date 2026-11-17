// Part of the visual diff helper; composed by visual-diff.js.
window.visualDiffParts = window.visualDiffParts ?? {};
window.visualDiffParts.compare = {
	/**
	 * Computes a pixel difference between two PNG data-URIs.
	 * Returns diff image and detailed pixel percentages + footer baseline drift.
	 * @param {string} pngA data-URI of reference image
	 * @param {string} pngB data-URI of rendered SVG image
	 */
	async computeDiff(pngA, pngB) {
		const [imgA, imgB] = await Promise.all([
			this._loadImage(pngA),
			this._loadImage(pngB),
		]);
		const w = Math.max(imgA.width, imgB.width);
		const h = Math.max(imgA.height, imgB.height);

		const canvasA = this._createCanvas(w, h);
		const ctxA = canvasA.getContext("2d", { willReadFrequently: true });
		ctxA.drawImage(imgA, 0, 0);
		const dataA = ctxA.getImageData(0, 0, w, h);

		const canvasB = this._createCanvas(w, h);
		const ctxB = canvasB.getContext("2d", { willReadFrequently: true });
		ctxB.drawImage(imgB, 0, 0);
		const dataB = ctxB.getImageData(0, 0, w, h);

		const diff = ctxA.createImageData(w, h);
		const threshold = 32;
		const whiteThreshold = 248;

		let mismatch = 0;
		let sameCount = 0;
		let missingCount = 0;
		let extraCount = 0;
		let recolorCount = 0;

		const total = w * h;

		const blendWhite = (r, g, b, a) => {
			const af = a / 255;
			return [
				Math.round(r * af + 255 * (1 - af)),
				Math.round(g * af + 255 * (1 - af)),
				Math.round(b * af + 255 * (1 - af)),
			];
		};

		const isWhite = (r, g, b) =>
			r >= whiteThreshold && g >= whiteThreshold && b >= whiteThreshold;

		for (let i = 0; i < dataA.data.length; i += 4) {
			const [rA, gA, bA] = blendWhite(
				dataA.data[i],
				dataA.data[i + 1],
				dataA.data[i + 2],
				dataA.data[i + 3],
			);
			const [rB, gB, bB] = blendWhite(
				dataB.data[i],
				dataB.data[i + 1],
				dataB.data[i + 2],
				dataB.data[i + 3],
			);

			const dr = Math.abs(rA - rB);
			const dg = Math.abs(gA - gB);
			const db = Math.abs(bA - bB);

			if (dr > threshold || dg > threshold || db > threshold) {
				const referenceIsWhite = isWhite(rA, gA, bA);
				const svgIsWhite = isWhite(rB, gB, bB);

				if (!referenceIsWhite && svgIsWhite) {
					diff.data[i] = 255;
					diff.data[i + 1] = 0;
					diff.data[i + 2] = 0;
					missingCount++;
				} else if (referenceIsWhite && !svgIsWhite) {
					diff.data[i] = 0;
					diff.data[i + 1] = 255;
					diff.data[i + 2] = 255;
					extraCount++;
				} else {
					diff.data[i] = 160;
					diff.data[i + 1] = 160;
					diff.data[i + 2] = 160;
					recolorCount++;
				}
				diff.data[i + 3] = 255;
				mismatch++;
			} else {
				diff.data[i] = 255;
				diff.data[i + 1] = 255;
				diff.data[i + 2] = 255;
				diff.data[i + 3] = 255;
				sameCount++;
			}
		}

		const outCanvas = this._createCanvas(w, h);
		const outCtx = outCanvas.getContext("2d");
		outCtx.fillStyle = "#fff";
		outCtx.fillRect(0, 0, w, h);
		outCtx.putImageData(diff, 0, 0);
		const blob = await this._canvasToBlob(outCanvas);
		const diffUri = await this._blobToDataUri(blob);

		const referenceBaselineY = this._estimateFooterBaselineY(dataA, w, h);
		const renderedBaselineY = this._estimateFooterBaselineY(dataB, w, h);

		let baselineDelta = null;
		if (referenceBaselineY !== null && renderedBaselineY !== null) {
			baselineDelta = renderedBaselineY - referenceBaselineY;
		}

		const result = {
			diffImageDataUri: diffUri,
			matchPercent: total > 0 ? ((total - mismatch) / total) * 100 : 100,
			mismatchCount: mismatch,
			totalPixels: total,
			whitePercent: total > 0 ? (sameCount / total) * 100 : 100,
			missingFromSvgPercent: total > 0 ? (missingCount / total) * 100 : 0,
			extraInSvgPercent: total > 0 ? (extraCount / total) * 100 : 0,
			recolorPercent: total > 0 ? (recolorCount / total) * 100 : 0,
			footerBaselineDeltaPx: baselineDelta,
		};
		return result;
	},
	_estimateFooterBaselineY(imageData, width, height) {
		const startY = Math.floor(height * 0.86);
		const endY = height;
		const data = imageData.data;

		let bestY = null;
		let bestInk = 0;

		for (let y = startY; y < endY; y++) {
			let rowInk = 0;
			const rowOffset = y * width * 4;
			for (let x = 0; x < width; x++) {
				const i = rowOffset + x * 4;
				const r = data[i];
				const g = data[i + 1];
				const b = data[i + 2];
				const a = data[i + 3];
				if (a < 16) {
					continue;
				}

				const af = a / 255;
				const rw = r * af + 255 * (1 - af);
				const gw = g * af + 255 * (1 - af);
				const bw = b * af + 255 * (1 - af);

				if (rw < 90 || gw < 90 || bw < 90) {
					rowInk++;
				}
			}

			if (rowInk > bestInk) {
				bestInk = rowInk;
				bestY = y;
			}
		}

		return bestInk > 0 ? bestY : null;
	},
};
