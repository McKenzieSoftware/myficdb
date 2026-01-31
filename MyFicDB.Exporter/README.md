# How to add an additional export option

Modify StoryExportType (Enums/StoryExportType.cs) to have your additional export type.

Modify StoryExportService (Services/StoryExportService.cs), in the constructor modify _renders to add in your additional export type. 

Create the method for exporting the data to that specific type, for instance PDF, EPUB etc. See RenderHtml/RenderMarkdown for examples.

Exporting is then available the same way as HTML or Markdown.  To add it to the story view, go to Views/Story/View and modify the dropdown and add in your new export type

Copy and paste an existing one (html) and replace the asp-route-type="html" to whatever your type is, asp-route-type="pdf" for example.