namespace Modules.ReviewModule.Services
{
    /// <summary>Google Play's in-app review sheet.</summary>
    public interface IReviewService
    {
        /// <summary>Asks Play to show its rating sheet. Play may decline silently; nothing comes back.</summary>
        void Request();
    }
}
