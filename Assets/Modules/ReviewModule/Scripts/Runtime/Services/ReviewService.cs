using FlowIoC.ConsoleModule;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.Review;
#endif

namespace Modules.ReviewModule.Services
{
    public class ReviewService : IReviewService
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private ReviewManager _manager;

        public void Request()
        {
            _manager ??= new ReviewManager();
            _manager.RequestReviewFlow().Completed += request =>
            {
                if (!request.IsSuccessful)
                {
                    FlowLogger.LogWarning($"Review sheet unavailable: {request.Error}");
                    return;
                }

                _manager.LaunchReviewFlow(request.GetResult());
            };
        }
#else
        public void Request() => FlowLogger.Log("Review sheet requested - shown on a Play install only");
#endif
    }
}
