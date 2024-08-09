using UnityEngine;

namespace Volorf.VRNotifications
{
    [CreateAssetMenu(fileName = "Notification Settings", menuName = "Create Notification Settings")]
    public class NotificationSettings : ScriptableObject
    {
        public float defaultDuration;
        public float checkingFrequency;
        
        [Header("Positioning")]
        public float distanceFromHead;
        public bool IsOffsetRelative;
        public float downOffset;

        [Header("Animation")]
        public float toShowDuration;
        public AnimationCurve showCurve;
        public float toHideDuration;
        public AnimationCurve hideCurve;
        
        [Header("Following Head")]
        public bool FollowHead;
        public float followHeadSmoothKoef;
        public float lookAtHeadSmoothKoef;

        [Header("Styles")]
        public NotificationStyle defaultNotificationStyle;
        public NotificationStyle warningNotificationStyle;
        public NotificationStyle errorNotificationStyle;

        [Space(10)] 
        public bool printDebugMessages;
    }
}
