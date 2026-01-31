namespace MyFicDB.Web.ViewModels._Shared
{
    public sealed class ConfirmDeleteModalViewModel
    {
        public required string ModalId { get; init; }           // deleteTagModal etc
        public required string LabelId { get; init; }           // deleteTagModalLabel etc
        public required string FormId { get; init; }            // deleteTagForm etc

        // UI text
        public string Title { get; init; } = "Confirm Deletion";
        public string? ItemLabel { get; init; }                // tag, series, actor, story etc.
        public string BodyText { get; init; } = "Are you sure you want to delete this item?";
        public string DangerText { get; init; } = "This action cannot be undone.";

        // Buttons
        public string CancelText { get; init; } = "Cancel";
        public string ConfirmText { get; init; } = "Delete";
    }
}
