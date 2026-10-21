using System;
namespace Modules.NotificationModule.Data.ValueObjects
{
    /// <summary>One notification to show: under a key, at a local time.</summary>
    public sealed class NotificationRequestVO
    {
        public string Key;
        public string Title;
        public string Text;
        public DateTime At;
    }
}
