using System.ComponentModel.DataAnnotations;

namespace MyFicDB.Web.Areas.SystemManagement.ViewModels
{
    public sealed class ResetSystemViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;    
    }
}
