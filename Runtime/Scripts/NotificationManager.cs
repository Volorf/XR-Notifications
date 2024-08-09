using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Volorf.VRNotifications
{
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance
        {
            get
            {
                return _notificationManager;
            }
        }

        private static NotificationManager _notificationManager;

        [FormerlySerializedAs("NotificationSettings")]
        [Header("Settings")]
        [SerializeField] private NotificationSettings _settings;

        [FormerlySerializedAs("UpdateElementsSizeInstance")]
        [Header("Elements")]
        [SerializeField] private UpdateElementsSize _updateElementsSize;
        [SerializeField] private Canvas UICanvas;
        [SerializeField] private Image BackgroundImage;
        [SerializeField] private TextMeshProUGUI MessageLabel;
        
        private Transform _camera;
        private Vector3 _smoothPositionVelocity = Vector3.zero;
        private Vector3 _smoothForwardVelocity = Vector3.zero;
        private Queue<Notification> _notificationQueue = new Queue<Notification>();
        private bool _isNotificationExecutorRunning = false;
        private bool _isMessageShowing = false;
        private int _messageCounter = 0;
        private bool _canUpdateSizeOfElements;
        
        private void Awake()
        {
            if (_notificationManager != null && _notificationManager != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _notificationManager = this;
            }
        }
        
        private async void Start()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Debug.Log("Time: " + stopWatch.ElapsedMilliseconds);
            await Task.Delay(10000);
            stopWatch.Stop();
            Debug.Log("Time: " + stopWatch.ElapsedMilliseconds);

            if (Camera.main != null)
            {
                _camera = Camera.main.transform;
            } 
            else
            {
                _camera = Camera.current.transform;
            }
            
            UICanvas.transform.localScale = Vector3.zero;
            
            if (_settings.showWelcomeMessageAtStart)
            {
                SendMessage("Welcome to VR Notification System!", NotificationType.Info);
            }
        }

        [ContextMenu("Send Debug Message")]
        public void SendDebugMessage()
        {
            _messageCounter += 1;
            string message = "Message #" + _messageCounter;
            Notification not = new Notification(message, NotificationType.Info);
            SendMessage(not); 
        }

        public void SendMessage(string message, NotificationType type)
        {
            if (_settings.printDebugMessages)
            {
                Debug.Log(message);
            }
            Notification notification = new Notification(message, type);
            AddMessageToQueue(notification);
        }
        
        public void SendMessage(string message, string subMessage, NotificationType type = NotificationType.Info)
        {
            string finalMessage = $"{message} \n<alpha=#33>{subMessage}";
            
            if (_settings.printDebugMessages)
            {
                Debug.Log(finalMessage);
            }
            
            Notification notification = new Notification(finalMessage, type);
            AddMessageToQueue(notification);
        }

        public void SendMessage(string message)
        {
            Notification notification = new Notification(message, NotificationType.Info);
            AddMessageToQueue(notification);
        }

        public void SendMessage(Notification not)
        {
            AddMessageToQueue(not);
        }

        private void AddMessageToQueue(Notification not)
        {
            _notificationQueue.Enqueue(not);

            if (!_isNotificationExecutorRunning)
            {
                StartCoroutine(ExecuteNotifications());
                
                if (_settings.FollowHead)
                {
                    StartCoroutine(FollowHead());
                }
                
            }
        }
        
        private void ShowMessage(Notification notification)
        {
            //Prepare elements
            switch (notification.Type)
            {
                case NotificationType.Info:
                    BackgroundImage.color = _settings.defaultNotificationStyle.backgroundColor;
                    MessageLabel.color = _settings.defaultNotificationStyle.messageColor;
                    break;
                case NotificationType.Warning:
                    BackgroundImage.color = _settings.warningNotificationStyle.backgroundColor;
                    MessageLabel.color = _settings.warningNotificationStyle.messageColor;
                    break;
                case NotificationType.Error:
                    BackgroundImage.color = _settings.errorNotificationStyle.backgroundColor;
                    MessageLabel.color = _settings.errorNotificationStyle.messageColor;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            transform.position = CalculateSnackBarPosition();
            transform.forward = (_camera.position - transform.position).normalized;
            
            TurnOnElements();
            
            UICanvas.transform.localScale = Vector3.zero;
            MessageLabel.text = notification.Message;

            StartCoroutine(CallNextFrame());
            
            Action callback = delegate { HideMessage(notification); };
            StartCoroutine(MessageAnimation(
                Vector3.one,
                _settings.toShowDuration, 
                0f,
                _settings.showCurve, 
                callback));
        }

        private void HideMessage(Notification notification)
        {
            Action callback = delegate { TurnOffElements(); };
            StartCoroutine(MessageAnimation(
                Vector3.zero, 
                _settings.toHideDuration,
                _settings.defaultDuration, 
                _settings.hideCurve, 
                callback));
        }

        private void TurnOnElements()
        {
            _isMessageShowing = true;
            SetElementsState(true);
        }

        private void TurnOffElements()
        {
            _isMessageShowing = false;
            SetElementsState(false);
        }

        private void SetElementsState(bool b)
        {
            UICanvas.enabled = b;
            // messageLabel.enabled = b;
        }
        
        private Vector3 CalculateSnackBarPosition()
        {
            Vector3 forwardDir;

            if (_settings.IsOffsetRelative)
            {
                forwardDir = _camera.forward;
            }
            else
            {
                forwardDir =  new Vector3(_camera.forward.x, 0f, _camera.forward.z).normalized;
            }
            
            return _camera.position + forwardDir * _settings.distanceFromHead + _camera.up * -1f * _settings.downOffset;
        }

        private IEnumerator ExecuteNotifications()
        {
            _isNotificationExecutorRunning = true;
            
            while (_notificationQueue.Count > 0)
            {
                if (!_isMessageShowing)
                {
                    Notification messageToShow = _notificationQueue.Dequeue();
                    ShowMessage(messageToShow);
                }
                
                yield return new WaitForSeconds(_settings.checkingFrequency);
            }
            
            _isNotificationExecutorRunning = false;
        }

        private IEnumerator CallNextFrame()
        {
            yield return null;
            yield return null;
            _updateElementsSize.UpdateSizeOfElements();
        }

        private IEnumerator FollowHead()
        {
            while (_isNotificationExecutorRunning || _isMessageShowing)
            {
                Vector3 newPos = CalculateSnackBarPosition();
                transform.position = Vector3.SmoothDamp(transform.position, newPos,
                    ref _smoothPositionVelocity, _settings.followHeadSmoothKoef);

                Vector3 newForward = (_camera.position - transform.position).normalized;
                transform.forward = Vector3.SmoothDamp(transform.forward, newForward, ref _smoothForwardVelocity, _settings.lookAtHeadSmoothKoef);
                
                yield return null;
            }
        }

        private IEnumerator MessageAnimation(Vector3 targetScale, float duration, float delay, AnimationCurve curve, Action callback)
        {
            float timer = 0f;
            Vector3 curScale = UICanvas.transform.localScale;

            while (timer <= delay)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            timer = 0f;
            
            while (timer <= duration)
            {
                float val = timer / duration;
                Vector3 newScale = Vector3.LerpUnclamped(curScale, targetScale, curve.Evaluate(val));
                UICanvas.transform.localScale = newScale;
                timer += Time.deltaTime;
                yield return null;
            }
            
            callback();
        }
    }
}