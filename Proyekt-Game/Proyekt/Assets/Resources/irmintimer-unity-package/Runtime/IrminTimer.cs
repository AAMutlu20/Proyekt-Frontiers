using System;
using UnityEngine;


namespace IrminTimerPackage.Tools
{
    [Serializable]
    public class IrminTimer
    {
        /// <summary>
        /// Current time in seconds.
        /// </summary>
        [SerializeField] private float _currentTime = 0;
        [SerializeField] private float _time;
        [SerializeField] private bool _timerActive = false;

        public event Action OnTimeElapsed;
        public event Action<IrminTimer> OnTimeElapsedWithRef;
        public event Action OnTimeElapsedReverse;
        public event Action<IrminTimer> OnTimeElapsedReverseWithRef;
        [SerializeField] private bool _reverse = false;

        public float Percentage { get { return _currentTime / (_time / 100); } }

        /// <summary>
        /// First paramenter = deltatime.
        /// Second paramenter = current time.
        /// Third parameter = time percentage until completion.
        /// </summary>
        public event Action<float, float, float> OnTimerTick;
        

        public float Time { get { return _time; } set { _time = value; } }
        public bool TimerActive { get { return _timerActive; } }

        public IrminTimer(float pTimerTime)
        {
            _time = pTimerTime;
        }

        public IrminTimer()
        {
            // Don't add time
        }

        public void StartTimer(bool pReverse = false, bool pStartAtRandomPoint = false)
        {
            if(!pStartAtRandomPoint)
            {
                _currentTime = pReverse ? Time : 0;
            }
            else
            {
                _currentTime = UnityEngine.Random.Range(0, _time);
            }
            _reverse = pReverse;
            _timerActive = true;
        }

        public void StartTimer(float pTime, bool pReverse = false, bool pStartAtRandomPoint = false)
        {
            _time = pTime;
            if (!pStartAtRandomPoint)
            {
                _currentTime = pReverse ? Time : 0;
            }
            else
            {
                _currentTime = UnityEngine.Random.Range(0, _time);
            }
                _reverse = pReverse;
            _timerActive = true;
        }

        public void PauseTimer()
        {
            _timerActive = false;
        }

        public void ResumeTimer()
        {
            _timerActive = true;
        }

        /// <summary>
        /// Needs to be called to update this timer.
        /// </summary>
        /// <param name="pDeltaTime">DeltaTime needs to be given as input.</param>
        public void UpdateTimer(float pDeltaTime)
        {
            if (!_timerActive) return;

            if (!_reverse)
            {
                _currentTime += pDeltaTime;
                OnTimerTick?.Invoke(pDeltaTime, _currentTime, _currentTime / (_time / 100));
                if (_currentTime >= _time)
                {
                    _timerActive = false;
                    OnTimeElapsed?.Invoke();
                    OnTimeElapsedWithRef?.Invoke(this);
                }
            }
            else
            {
                _currentTime -= pDeltaTime;
                OnTimerTick?.Invoke(pDeltaTime, _currentTime, _currentTime / (_time / 100));
                if (_currentTime <= 0)
                {
                    _timerActive = false;
                    OnTimeElapsedReverse?.Invoke();
                    OnTimeElapsedReverseWithRef?.Invoke(this);
                }
            }
        }

        public void Reverse()
        {
            _reverse = !_reverse;
        }

        public void SetReverse(bool pReverse)
        {
            _reverse = pReverse;
        }

        public void OnDestroy()
        {
            ClearAllEvents();
        }

        public void ClearAllEvents()
        {
            OnTimeElapsed = null;
            OnTimeElapsedReverse = null;
            OnTimeElapsedWithRef = null;
            OnTimeElapsedReverseWithRef = null;
            OnTimerTick = null;
        }

        public string GetRemainingTimeString()
        {
            float remainingSeconds = _reverse == true ? remainingSeconds = _currentTime : remainingSeconds = Time - _currentTime;

            int hourNumber = (int)MathF.Truncate(remainingSeconds / 3600);
            remainingSeconds -= hourNumber * 3600;
            int minuteNumber = (int)MathF.Truncate(remainingSeconds / 60);
            remainingSeconds -= minuteNumber * 60;

            // Now we format the string:
            string formattedTime = string.Empty;
            if (hourNumber > 0) 
            { 
                formattedTime += $"{hourNumber}:"; 
            }
            if (minuteNumber > 0)
            {
                string possibleExtraZero = string.Empty;
                //if (minuteNumber < 10) { possibleExtraZero = "0"; } Maybe not needed
                formattedTime += $"{possibleExtraZero}{minuteNumber}:";
            }
            formattedTime += remainingSeconds;
            return formattedTime;
        }

        public void ResetCurrentTime(bool pReverse = false)
        {
            if(pReverse)
            {
                _currentTime = Time;
            }
            else
            {
                _currentTime = 0;
            }
        }
    }
}