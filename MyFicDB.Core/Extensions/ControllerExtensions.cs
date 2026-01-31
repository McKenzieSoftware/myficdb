using Microsoft.AspNetCore.Mvc;

namespace MyFicDB.Core.Extensions
{
    /// <summary>
    /// Helper to display flash errors on Views; stops repetetion in controllers and typo's (since i can't spell lmao)
    /// </summary>
    public static class ControllerExtensions
    {
        public const string SuccessKey = "FlashSuccess";
        public const string ErrorKey = "FlashError";

        public static void FlashSuccess(this Controller controller, string message)
        {
            controller.TempData[SuccessKey] = message;
        }

        public static void FlashError(this Controller controller, string message)
        {
            controller.TempData[ErrorKey] = message;
        }
    }
}
