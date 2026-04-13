// Visual diff helper: PDF→PNG, SVG→PNG rasterisation, and pixel-level comparison.
// All heavy work stays on an offscreen canvas so the UI thread is not blocked by large pages.

window.visualDiff = {

    _pdfjs: null,

    /** Lazy-load pdf.js (ES module via dynamic import) */
    async _ensurePdfJs() {
        if (this._pdfjs) return this._pdfjs;
        const pdfjs = await import("https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.9.155/pdf.min.mjs");
        pdfjs.GlobalWorkerOptions.workerSrc =
            "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.9.155/pdf.worker.min.mjs";
        this._pdfjs = pdfjs;
        return pdfjs;
    },

    /**
     * Renders all pages of a PDF (given as Base64) to PNG data-URIs.
     * @param {string} base64Pdf  Base64-encoded PDF bytes
     * @param {number} scale      Render scale (1 = 72 dpi, 2 = 144 dpi, …)
     * @returns {Promise<string[]>} Array of PNG data-URIs, one per page
     */
    async renderPdfPages(base64Pdf, scale) {
        const pdfjs = await this._ensurePdfJs();
        const raw = Uint8Array.from(atob(base64Pdf), c => c.charCodeAt(0));
        const doc = await pdfjs.getDocument({ data: raw }).promise;
        const results = [];

        for (let i = 1; i <= doc.numPages; i++) {
            const page = await doc.getPage(i);
            const vp = page.getViewport({ scale });
            const canvas = new OffscreenCanvas(Math.ceil(vp.width), Math.ceil(vp.height));
            const ctx = canvas.getContext("2d");
            await page.render({ canvasContext: ctx, viewport: vp }).promise;
            const blob = await canvas.convertToBlob({ type: "image/png" });
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

        const serializer = new XMLSerializer();
        const svgString = serializer.serializeToString(svg);
        const blob = new Blob([svgString], { type: "image/svg+xml;charset=utf-8" });
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

            const canvas = new OffscreenCanvas(width, height);
            const ctx = canvas.getContext("2d");
            // White background so transparent SVG areas match PDF rendering
            ctx.fillStyle = "#fff";
            ctx.fillRect(0, 0, width, height);
            ctx.drawImage(img, 0, 0, width, height);
            const outBlob = await canvas.convertToBlob({ type: "image/png" });
            return await this._blobToDataUri(outBlob);
        } finally {
            URL.revokeObjectURL(url);
        }
    },

    /**
     * Computes a pixel difference between two PNG data-URIs.
     * Returns an object { diffImageDataUri, matchPercent, mismatchCount, totalPixels }.
     * Matching pixels are white. Missing SVG ink is red, extra SVG ink is cyan,
     * and color-only mismatches where both sides contain ink are grey.
     * @param {string} pngA  data-URI of image A
     * @param {string} pngB  data-URI of image B
     * @returns {Promise<{diffImageDataUri:string, matchPercent:number, mismatchCount:number, totalPixels:number}>}
     */
    async computeDiff(pngA, pngB) {
        const [imgA, imgB] = await Promise.all([this._loadImage(pngA), this._loadImage(pngB)]);
        const w = Math.max(imgA.width, imgB.width);
        const h = Math.max(imgA.height, imgB.height);

        const canvasA = new OffscreenCanvas(w, h);
        const ctxA = canvasA.getContext("2d");
        ctxA.drawImage(imgA, 0, 0);
        const dataA = ctxA.getImageData(0, 0, w, h);

        const canvasB = new OffscreenCanvas(w, h);
        const ctxB = canvasB.getContext("2d");
        ctxB.drawImage(imgB, 0, 0);
        const dataB = ctxB.getImageData(0, 0, w, h);

        const diff = ctxA.createImageData(w, h);
        const threshold = 32; // per-channel tolerance
        const whiteThreshold = 248;
        let mismatch = 0;
        const total = w * h;

        // Composite a pixel's RGBA against a white background, yielding opaque RGB.
        // This ensures transparent pixels are treated as white for comparison purposes.
        const blendWhite = (r, g, b, a) => {
            const af = a / 255;
            return [
                Math.round(r * af + 255 * (1 - af)),
                Math.round(g * af + 255 * (1 - af)),
                Math.round(b * af + 255 * (1 - af))
            ];
        };

        const isWhite = (r, g, b) => r >= whiteThreshold && g >= whiteThreshold && b >= whiteThreshold;

        for (let i = 0; i < dataA.data.length; i += 4) {
            const [rA, gA, bA] = blendWhite(dataA.data[i], dataA.data[i + 1], dataA.data[i + 2], dataA.data[i + 3]);
            const [rB, gB, bB] = blendWhite(dataB.data[i], dataB.data[i + 1], dataB.data[i + 2], dataB.data[i + 3]);

            const dr = Math.abs(rA - rB);
            const dg = Math.abs(gA - gB);
            const db = Math.abs(bA - bB);

            if (dr > threshold || dg > threshold || db > threshold) {
                const referenceIsWhite = isWhite(rA, gA, bA);
                const svgIsWhite = isWhite(rB, gB, bB);

                if (!referenceIsWhite && svgIsWhite) {
                    // White on SVG, non-white on PNG.
                    diff.data[i] = 255;
                    diff.data[i + 1] = 0;
                    diff.data[i + 2] = 0;
                } else if (referenceIsWhite && !svgIsWhite) {
                    // Non-white on SVG, white on PNG.
                    diff.data[i] = 0;
                    diff.data[i + 1] = 255;
                    diff.data[i + 2] = 255;
                } else {
                    // Non-white on both, but different colors.
                    diff.data[i] = 160;
                    diff.data[i + 1] = 160;
                    diff.data[i + 2] = 160;
                }
                diff.data[i + 3] = 255;
                mismatch++;
            } else {
                diff.data[i] = 255;
                diff.data[i + 1] = 255;
                diff.data[i + 2] = 255;
                diff.data[i + 3] = 255;
            }
        }

        const outCanvas = new OffscreenCanvas(w, h);
        const outCtx = outCanvas.getContext("2d");
        // White background so matching areas remain explicit in the output.
        outCtx.fillStyle = "#fff";
        outCtx.fillRect(0, 0, w, h);
        outCtx.putImageData(diff, 0, 0);
        const blob = await outCanvas.convertToBlob({ type: "image/png" });
        const diffUri = await this._blobToDataUri(blob);

        return {
            diffImageDataUri: diffUri,
            matchPercent: total > 0 ? ((total - mismatch) / total) * 100 : 100,
            mismatchCount: mismatch,
            totalPixels: total
        };
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
    }
};
