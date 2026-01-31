/*

    This is used for switching TinyMCE from Darkmode to Lightmode based on the toggle in _LoginPartial.cshtml

*/

const EDITOR_ID = 'TinyMCE-Editor';

function currentTheme() {
    return document.documentElement.getAttribute('data-bs-theme') || 'light';
}

function tinyOptions(theme) {
    const isDark = theme === 'dark';
    return {
        selector: `#${EDITOR_ID}`,
        menubar: false,
        plugins: 'lists link charmap preview anchor emoticons wordcount', //autoresize
        toolbar: 'undo redo | bold italic underline strikethrough | alignleft aligncenter alignright alignjustify | bullist numlist | blockquote | link charmap emoticons footnotes | preview',
        branding: true,
        license_key: 'gpl',

        //autoresize_bottom_margin: 16,
        //autoresize_min_height: 400,
        //autoresize_max_height: 2000,

        valid_elements: 'p,strong,em,ul,ol,li,blockquote,a[href|title],span[style],br,hr,strike,sub,sup,code',
        valid_styles: { '*': 'text-align' },

        cleanup: true,
        verify_html: true,
        extended_valid_elements: 'p[style]',
        invalid_attributes: 'data-*',

        skin_url: isDark ? '/lib/tinymce/skins/ui/oxide-dark' : '/lib/tinymce/skins/ui/oxide',
        content_css: isDark
            ? '/lib/tinymce/skins/content/dark/content.min.css'
            : '/lib/tinymce/skins/content/default/content.min.css',

        // Make the content area match Bootstrap dark surfaces too
        content_style: isDark
            ? 'body { background-color: #212529; color: #f8f9fa; } a { color: #8bb9fe; }'
            : '',

        // Force stylesheet URL to change
        cache_suffix: `?theme=${theme}`,
    };
}

function initTiny(theme) {
    if (!document.getElementById(EDITOR_ID)) return;
    tinymce.init(tinyOptions(theme));
}

function switchTiny(theme) {
    const ed = tinymce.get(EDITOR_ID);
    if (!ed) {
        initTiny(theme);
        return;
    }

    const content = ed.getContent();
    ed.remove();

    tinymce.init({
        ...tinyOptions(theme),
        setup: (editor) => editor.on('init', () => editor.setContent(content))
    });
}

// Init once
document.addEventListener('DOMContentLoaded', () => initTiny(currentTheme()));

// React to your existing theme toggle event
window.addEventListener('myficdb:theme-changed', (e) => switchTiny(e.detail.theme));