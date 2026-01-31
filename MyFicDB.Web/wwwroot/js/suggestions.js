/*

    used when creating/editing a story to provide suggestions for tags, series and actors

*/
(function () {
    function defaultGetToken(value) {
        const parts = value.split(',');
        return (parts[parts.length - 1] || '').trim();
    }

    function defaultReplaceToken(value, replacement) {
        const parts = value.split(',');
        parts[parts.length - 1] = ' ' + replacement;
        return parts.map(p => p.trim()).filter(Boolean).join(', ') + ', ';
    }

    function singleGetToken(value) {
        return (value || '').trim();
    }

    function singleReplaceToken(_value, replacement) {
        return replacement;
    }

    async function fetchSuggest(endpoint, q) {
        const res = await fetch(`${endpoint}?query=${encodeURIComponent(q)}`, {
            headers: { "Accept": "application/json" }
        });
        if (!res.ok) return [];
        return await res.json();
    }

    /**
     * Initializes a suggestion dropdown for an input.
     *
     * Options:
     * - inputId: element id for the input
     * - suggestId: element id for the suggestion container
     * - endpoint: suggestion endpoint (expects ?query=)
     * - minChars: minimum chars before fetching
     * - debounceMs: debounce delay
     * - mode: "csv" (default) or "single"
     * - itemLabel: property name to show (default "name")
     */
    window.initSuggestBox = function initSuggestBox(options) {
        const {
            inputId,
            suggestId,
            endpoint,
            minChars = 2,
            debounceMs = 200,
            mode = "csv",
            itemLabel = "name"
        } = options || {};

        const input = document.getElementById(inputId);
        const box = document.getElementById(suggestId);

        if (!input || !box || !endpoint) return;

        let lastQuery = "";
        let debounceTimer = null;

        const getToken = mode === "single" ? singleGetToken : defaultGetToken;
        const replaceToken = mode === "single" ? singleReplaceToken : defaultReplaceToken;

        function hide() {
            box.style.display = "none";
            box.innerHTML = "";
        }

        function show(items) {
            if (!items || items.length === 0) return hide();

            box.innerHTML = "";
            items.forEach(x => {
                const label = (x && x[itemLabel]) ? String(x[itemLabel]) : "";
                if (!label) return;

                const btn = document.createElement("button");
                btn.type = "button";
                btn.className = "list-group-item list-group-item-action";
                btn.textContent = label;

                btn.addEventListener("click", () => {
                    input.value = replaceToken(input.value, label);
                    hide();
                    input.focus();
                });

                box.appendChild(btn);
            });

            box.style.display = "block";
        }

        input.addEventListener("input", () => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(async () => {
                const token = getToken(input.value);

                if (token.length < minChars) return hide();
                if (token === lastQuery) return;

                lastQuery = token;
                const items = await fetchSuggest(endpoint, token);
                show(items);
            }, debounceMs);
        });

        document.addEventListener("click", (e) => {
            if (e.target !== input && !box.contains(e.target)) hide();
        });

        input.addEventListener("keydown", (e) => {
            if (e.key === "Escape") hide();
        });
    };
})();