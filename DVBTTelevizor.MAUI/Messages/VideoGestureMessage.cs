using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DVBTTelevizor.MAUI.Messages
{
    public enum VideoGestureType
    {
        LeftSwipe,
        RightSwipe,
        Click,
        DoubleClick
    }

    public class VideoGestureMessage : ValueChangedMessage<VideoGestureType>
    {
        public VideoGestureMessage(VideoGestureType value) : base(value)
        {
        }
    }
}
