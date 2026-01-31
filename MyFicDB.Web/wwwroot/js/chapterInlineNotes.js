/*
    Allows the user to create inline notes on desktop or mobile devices (only tested on ios cos i dont have android)

    NOTE: I am terrible at javascript.  If anyone can refactor this and make it better, go ahead, but it works and it isn't slow lol
    
    docs: https://developer.mozilla.org/en-US/docs/Glossary/IIFE
    iIFE so it runs right away + doesnt leak vars everywhere (i think?)
*/

(function () {

    // get the chapter body div where the text is
    var body = document.getElementById("chapter-body");
    if (!body) {
        // no chapter found so bail out
        return;
    }

    // check if the device is touch (phones/tablets)
    // not 100% sure this is perfect but seems common
    function isTouchDevice() {
        return ('ontouchstart' in window || navigator.maxTouchPoints > 0);
    }

    // make the little popup menu for adding/removing highlights
    var menu = document.createElement("div");
    menu.className = "card shadow-sm mfdb-inline-note-menu";

    // im just doing this as html string because its easiest
    menu.innerHTML =
        '<div class="list-group list-group-flush">' +
        '  <button type="button" class="list-group-item list-group-item-action" data-action="add">Highlight</button>' +
        '  <button type="button" class="list-group-item list-group-item-action text-danger" data-action="remove">Remove highlight</button>' +
        '</div>';

    document.body.appendChild(menu);

    // buttons inside the menu
    var addBtn = menu.querySelector('[data-action="add"]');
    var removeBtn = menu.querySelector('[data-action="remove"]');

    // try pull data attrs first (if they exist)
    var storyId = body.getAttribute("data-story-id");
    var chapterNumber = body.getAttribute("data-chapter-number");

    // also support route parsing just in case attrs aren't there
    function getStoryAndChapterFromUrl() {
        // split path like /story/xxx/chapter/3 into bits
        var parts = window.location.pathname.split("/").filter(Boolean);
        var result = {
            storyId: null,
            chapterNumber: null
        };

        // loop through and find story + chapter
        for (var i = 0; i < parts.length; i++) {
            if (parts[i] === "story") {
                result.storyId = parts[i + 1];
            }
            if (parts[i] === "chapter") {
                result.chapterNumber = parts[i + 1];
            }
        }

        return result;
    }

    // fallback if the data attributes are missing
    var routeBits = getStoryAndChapterFromUrl();
    var sId = storyId || routeBits.storyId;
    var cNo = chapterNumber || routeBits.chapterNumber;

    // get anti-forgery token from a hidden form input
    function getAntiForgeryToken() {
        var t = document.querySelector('#inline-note-af input[name="__RequestVerificationToken"]');
        return t ? t.value : null;
    }

    // hide menu and reset random state
    function hideMenu() {
        menu.style.display = "none";

        // clearing these so we dont get weird stale stuff
        menu.dataset.mode = "";
        menu.dataset.noteId = "";
        menu.dataset.selectedText = "";

        // private-ish property storing range (kinda hacky)
        menu._savedRange = null;
    }

    // show menu at x/y and swap buttons depending on mode
    function showMenu(x, y, mode, noteId) {
        menu.style.display = "block";

        // set to 0 first so getBoundingClientRect works ok
        menu.style.left = "0px";
        menu.style.top = "0px";

        // "selection" = add highlight, "note" = remove highlight
        addBtn.style.display = (mode === "selection") ? "block" : "none";
        removeBtn.style.display = (mode === "note") ? "block" : "none";

        var rect = menu.getBoundingClientRect();
        var padding = 8;

        // keep it on screen horizontally
        if (x + rect.width > window.innerWidth) {
            x = window.innerWidth - rect.width - padding;
        }

        // dont go too far up
        if (y < padding) {
            y = padding;
        }

        menu.style.left = x + "px";
        menu.style.top = y + "px";

        menu.dataset.mode = mode;
        menu.dataset.noteId = noteId || "";
    }

    // make sure the selection is inside the chapter body
    function selectionIsInsideBody(sel) {
        if (!sel || sel.rangeCount === 0) {
            return false;
        }

        // selection common ancestor (might be a text node)
        var node = sel.getRangeAt(0).commonAncestorContainer;

        // text node -> parent element
        if (node.nodeType === 3) {
            node = node.parentNode;
        }

        return body.contains(node);
    }

    // store the selected text + the range so we can highlight it after hitting the button
    function saveSelectionStuff(sel) {
        try {
            // store the raw selected text
            menu.dataset.selectedText = sel.toString().trim();

            // clone range so we can use it later
            if (sel.rangeCount > 0) {
                menu._savedRange = sel.getRangeAt(0).cloneRange();
            }
        } catch (err) {
            // if something goes wrong just clear it
            // (probly ios does weird stuff sometimes?)
            menu._savedRange = null;
        }
    }

    // wrap the currently saved selection in a span highlight
    function wrapCurrentSelection(noteId) {
        var range = menu._savedRange;
        if (!range || range.collapsed) {
            return;
        }

        var span = document.createElement("span");
        span.className = "mfdb-inline-note-highlight";
        span.setAttribute("data-note-id", noteId);

        try {
            // this can throw if selection splits nodes etc
            range.surroundContents(span);
        } catch (err2) {
            // fallback: extract and reinsert
            var frag = range.extractContents();
            span.appendChild(frag);
            range.insertNode(span);
        }

        // clear saved range now we've used it
        menu._savedRange = null;
    }

    // POST create note (details = selected text)
    async function apiCreate(details) {
        var res = await fetch(`/story/${sId}/chapter/${cNo}/notes`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": getAntiForgeryToken()
            },
            // server expects { details: "..." }
            body: JSON.stringify({
                details: details
            })
        });

        // assume it returns json with id etc
        return await res.json();
    }

    // DELETE note by id
    async function apiDelete(noteId) {
        await fetch(`/story/${sId}/chapter/${cNo}/notes/${noteId}`, {
            method: "DELETE",
            headers: {
                "RequestVerificationToken": getAntiForgeryToken()
            }
        });
    }

    // GET all notes for chapter
    async function apiGetAllNotes() {
        var r = await fetch(`/story/${sId}/chapter/${cNo}/notes`);
        if (!r.ok) {
            return [];
        }
        return await r.json();
    }

    // on touch devices we can't reliably wrap exact selection (selection range is weird),
    // so we just find the first match of the text and wrap it
    function highlightFirstMatch(noteId, text) {
        var walker = document.createTreeWalker(body, NodeFilter.SHOW_TEXT);
        var node;

        // walk all text nodes
        while ((node = walker.nextNode())) {
            var idx = node.nodeValue.indexOf(text);
            if (idx === -1) {
                continue;
            }

            // create highlight span
            var span = document.createElement("span");
            span.className = "mfdb-inline-note-highlight";
            span.setAttribute("data-note-id", noteId);

            // NOTE: this sets the span text to the exact matched text
            span.textContent = text;

            // replace the text node with a fragment:
            // [before][span match][after]
            var frag = document.createDocumentFragment();
            frag.appendChild(document.createTextNode(node.nodeValue.slice(0, idx)));
            frag.appendChild(span);
            frag.appendChild(document.createTextNode(node.nodeValue.slice(idx + text.length)));

            node.parentNode.replaceChild(frag, node);
            return;
        }
    }

    // load existing notes and apply highlights
    async function rehydrate() {
        var notes = await apiGetAllNotes();

        // reverse so earlier replacements dont break later ones as much (i guess?)
        notes.slice().reverse().forEach(function (n) {
            highlightFirstMatch(n.id, n.details);
        });
    }

    // kick it off
    rehydrate();

    // desktop mouse selection + clicking highlight
    body.addEventListener("mouseup", function (e) {

        // if we clicked on an existing highlight, show remove option
        var highlight = e.target.closest(".mfdb-inline-note-highlight");
        if (highlight) {
            var r = highlight.getBoundingClientRect();
            showMenu(
                r.left + window.scrollX,
                r.bottom + window.scrollY + 6,
                "note",
                highlight.dataset.noteId
            );
            return;
        }

        // otherwise check if user selected some text
        var sel = window.getSelection();
        if (!selectionIsInsideBody(sel)) {
            return;
        }

        var txt = sel.toString().trim();
        if (txt.length < 2) {
            return;
        }

        // save range + selected text
        saveSelectionStuff(sel);

        // position menu under the selection rect
        var rect = sel.getRangeAt(0).getBoundingClientRect();
        showMenu(
            rect.left + window.scrollX,
            rect.bottom + window.scrollY + 6,
            "selection",
            ""
        );
    });

    // touch stuff (phones etc)
    var isTouching = false;
    var mobileTimer = null;

    document.addEventListener("touchstart", function () {
        isTouching = true;
    }, {
        passive: true
    });

    document.addEventListener("touchend", function () {
        isTouching = false;
    }, {
        passive: true
    });

    if (isTouchDevice()) {
        // on mobile, selection is different, use selectionchange + debounce
        document.addEventListener("selectionchange", function () {
            clearTimeout(mobileTimer);

            mobileTimer = setTimeout(function () {
                // if finger is still down then dont do menu yet
                if (isTouching) {
                    return;
                }

                var sel = window.getSelection();
                if (!selectionIsInsideBody(sel)) {
                    return;
                }

                var txt = sel.toString().trim();
                if (txt.length < 2) {
                    return;
                }

                saveSelectionStuff(sel);

                // show above-ish selection (because finger covers bottom)
                var r = sel.getRangeAt(0).getBoundingClientRect();
                showMenu(
                    r.left + window.scrollX,
                    r.top + window.scrollY - 10,
                    "selection",
                    ""
                );
            }, 260); // random delay that felt ok
        });
    }

    // handle click/tap on menu buttons
    function handleMenuAction(e) {
        var btn = e.target.closest("button[data-action]");
        if (!btn) {
            return;
        }

        // do async in a tiny self-calling function because i get confused otherwise
        (async function () {
            if (btn.dataset.action === "add") {
                var details = menu.dataset.selectedText;

                // create note on server
                var created = await apiCreate(details);

                // highlight it depending on device
                if (isTouchDevice()) {
                    highlightFirstMatch(created.id, details);
                } else {
                    wrapCurrentSelection(created.id);
                }

                hideMenu();
            }

            if (btn.dataset.action === "remove") {
                var id = menu.dataset.noteId;

                // delete on server
                await apiDelete(id);

                // unwrap any spans with this id
                document.querySelectorAll(`span[data-note-id="${id}"]`).forEach(function (s) {
                    while (s.firstChild) {
                        s.parentNode.insertBefore(s.firstChild, s);
                    }
                    s.remove();
                });

                hideMenu();
            }
        })();
    }

    // click for desktop
    menu.addEventListener("click", handleMenuAction);

    // touchend for mobile (prevent default to stop weird click delay etc)
    menu.addEventListener("touchend", function (e) {
        e.preventDefault();
        handleMenuAction(e);
    }, {
        passive: false
    });

    // click outside closes menu
    document.addEventListener("mousedown", function (e) {
        if (!menu.contains(e.target)) {
            hideMenu();
        }
    });

    // touch outside closes menu
    document.addEventListener("touchstart", function (e) {
        if (!menu.contains(e.target)) {
            hideMenu();
        }
    }, {
        passive: true
    });

    // scroll hides it too
    window.addEventListener("scroll", hideMenu);

    // start hidden
    hideMenu();

})();