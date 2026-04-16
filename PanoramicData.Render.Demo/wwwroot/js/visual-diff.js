// Visual diff helper: PDF->PNG, SVG->PNG rasterisation, and pixel-level comparison.
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
     * @param {number} scale      Render scale (1 = 72 dpi, 2 = 144 dpi, ...)
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

        const exportSvg = svg.cloneNode(true);
        exportSvg.setAttribute("width", String(width));
        exportSvg.setAttribute("height", String(height));
        if (!exportSvg.getAttribute("viewBox")) {
            exportSvg.setAttribute("viewBox", `0 0 ${width} ${height}`);
        }
        exportSvg.setAttribute("style", `display:block;width:${width}px;height:${height}px`);

        const serializer = new XMLSerializer();
        const svgString = serializer.serializeToString(exportSvg);
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
     * Returns diff image and detailed pixel percentages + footer baseline drift.
     * @param {string} pngA data-URI of reference image
     * @param {string} pngB data-URI of rendered SVG image
     */
    async computeDiff(pngA, pngB) {
        const [imgA, imgB] = await Promise.all([this._loadImage(pngA), this._loadImage(pngB)]);
        const w = Math.max(imgA.width, imgB.width);
        const h = Math.max(imgA.height, imgB.height);

        const canvasA = new OffscreenCanvas(w, h);
        const ctxA = canvasA.getContext("2d", { willReadFrequently: true });
        ctxA.drawImage(imgA, 0, 0);
        const dataA = ctxA.getImageData(0, 0, w, h);

        const canvasB = new OffscreenCanvas(w, h);
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

        const outCanvas = new OffscreenCanvas(w, h);
        const outCtx = outCanvas.getContext("2d");
        outCtx.fillStyle = "#fff";
        outCtx.fillRect(0, 0, w, h);
        outCtx.putImageData(diff, 0, 0);
        const blob = await outCanvas.convertToBlob({ type: "image/png" });
        const diffUri = await this._blobToDataUri(blob);

        const referenceBaselineY = this._estimateFooterBaselineY(dataA, w, h);
        const renderedBaselineY = this._estimateFooterBaselineY(dataB, w, h);

        let baselineDelta = null;
        if (referenceBaselineY !== null && renderedBaselineY !== null) {
            baselineDelta = renderedBaselineY - referenceBaselineY;
        }

        return {
            diffImageDataUri: diffUri,
            matchPercent: total > 0 ? ((total - mismatch) / total) * 100 : 100,
            mismatchCount: mismatch,
            totalPixels: total,
            whitePercent: total > 0 ? (sameCount / total) * 100 : 100,
            missingFromSvgPercent: total > 0 ? (missingCount / total) * 100 : 0,
            extraInSvgPercent: total > 0 ? (extraCount / total) * 100 : 0,
            recolorPercent: total > 0 ? (recolorCount / total) * 100 : 0,
            footerBaselineDeltaPx: baselineDelta
        };
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
                const i = rowOffset + (x * 4);
                const r = data[i];
                const g = data[i + 1];
                const b = data[i + 2];
                const a = data[i + 3];
                if (a < 16) {
                    continue;
                }

                const af = a / 255;
                const rw = (r * af) + 255 * (1 - af);
                const gw = (g * af) + 255 * (1 - af);
                const bw = (b * af) + 255 * (1 - af);

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

    /**
     * Returns intrinsic pixel dimensions for a data-URI image.
     * @param {string} dataUri
     * @returns {Promise<{width:number,height:number}>}
     */
    async getImageSize(dataUri) {
        const img = await this._loadImage(dataUri);
        return { width: img.width, height: img.height };
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

    reviewStore: {
        _prefix: "panoramic.render.review",

        _key(docKey, pageNumber) {
            return `${this._prefix}::${docKey}::page-${pageNumber}`;
        },

        getPage(docKey, pageNumber) {
            try {
                const raw = localStorage.getItem(this._key(docKey, pageNumber));
                if (!raw) {
                    return null;
                }
                return JSON.parse(raw);
            } catch {
                return null;
            }
        },

        setPage(docKey, pageNumber, payload) {
            try {
                localStorage.setItem(this._key(docKey, pageNumber), JSON.stringify(payload));
            } catch {
                // Best-effort persistence only.
            }
        }
    }
};

/** Focus a Blazor ElementReference so the browser gives it keyboard focus. */
window.focusElement = function (element) {
    if (element) {
        element.focus();
    }
};
