#import <UIKit/UIKit.h>

// Bumped to stop a running PlayFor: its pending taps see a newer generation and do nothing.
static int jellyHapticGeneration = 0;

static void JellyHapticImpact(int preset, float intensity)
{
    if (@available(iOS 13.0, *)) {
        // Selection plays as a light impact: UISelectionFeedbackGenerator is too faint to feel in play.
        UIImpactFeedbackStyle style = preset <= 1 ? UIImpactFeedbackStyleLight : preset == 2 ? UIImpactFeedbackStyleMedium : UIImpactFeedbackStyleHeavy;
        UIImpactFeedbackGenerator *feedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
        [feedback prepare];
        [feedback impactOccurredWithIntensity:(CGFloat)fmaxf(0.05f, fminf(1.0f, intensity))];
#if !__has_feature(objc_arc)
        [feedback release];
#endif
    }
}

extern "C" void JellyHapticPlay(int preset, float intensity)
{
    // Unity calls this synchronously on its main thread; no pending callback survives disabling.
    if (@available(iOS 13.0, *)) {
        if (preset == 4 || preset == 5) {
            UINotificationFeedbackGenerator *feedback = [[UINotificationFeedbackGenerator alloc] init];
            [feedback prepare];
            [feedback notificationOccurred:(preset == 4 ? UINotificationFeedbackTypeSuccess : UINotificationFeedbackTypeError)];
#if !__has_feature(objc_arc)
            [feedback release];
#endif
        } else {
            JellyHapticImpact(preset, intensity);
        }
    }
}

// A train of medium taps, intervalMs apart, for seconds - a tremble while something lasts on screen.
extern "C" void JellyHapticPlayFor(float intensity, int intervalMs, float seconds)
{
    int generation = ++jellyHapticGeneration;
    int interval = MAX(30, intervalMs);
    int taps = (int)ceilf(seconds * 1000.0f / (float)interval);
    for (int i = 0; i < taps; i++) {
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)i * interval * (int64_t)NSEC_PER_MSEC), dispatch_get_main_queue(), ^{
            if (generation != jellyHapticGeneration) return;
            JellyHapticImpact(2, intensity);
        });
    }
}

extern "C" void JellyHapticCancel()
{
    jellyHapticGeneration++;
}
