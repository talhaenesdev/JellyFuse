using FlowIoC.BaseModule.Contexts;
using Modules.ReviewModule.Services;

namespace Modules.ReviewModule.RootsContexts
{
    public class ReviewServiceContext : Context
    {
        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinderCrossContext.Bind<IReviewService, ReviewService>();
        }
    }
}
