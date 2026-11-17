// Part of the visual diff helper; composed by visual-diff.js.
window.visualDiffParts = window.visualDiffParts ?? {};
window.visualDiffParts.canvas = {
	/** Creates an offscreen canvas where supported, else a detached canvas element */
	_createCanvas(width, height) {
		if (typeof OffscreenCanvas !== "undefined") {
			return new OffscreenCanvas(width, height);
		}
		const canvas = document.createElement("canvas");
		canvas.width = width;
		canvas.height = height;
		return canvas;
	},
	/** Encodes a canvas as a PNG blob */
	_canvasToBlob(canvas) {
		if (typeof canvas.convertToBlob === "function") {
			return canvas.convertToBlob({ type: "image/png" });
		}
		return new Promise((resolve, reject) => {
			canvas.toBlob(
				(blob) =>
					blob ? resolve(blob) : reject(new Error("toBlob failed")),
				"image/png",
			);
		});
	},
	/**
	 * Returns intrinsic pixel dimensions for a data-URI image.
	 * @param {string} dataUri
	 * @returns {Promise<{width:number,height:number}>}
	 */
	async getImageSize(dataUri) {
		const img = await this._loadImage(dataUri);
		const size = { width: img.width, height: img.height };
		return size;
	},
	/** @private */
	_loadImage(dataUri) {
		return new Promise((resolve, reject) => {
			const img = new Image();
			img.onload = () => resolve(img);
			img.onerror = reject;
			img.src = dataUri;
		});
	},
	/** @private */
	_blobToDataUri(blob) {
		return new Promise((resolve, reject) => {
			const reader = new FileReader();
			reader.onloadend = () => resolve(reader.result);
			reader.onerror = reject;
			reader.readAsDataURL(blob);
		});
	},
};
