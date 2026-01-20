using IrminTimerPackage.Tools;
using System;
using System.Threading;
using UnityEngine;

public class IrminDestroyOverTimeComponent : MonoBehaviour
{
    [SerializeField] private IrminTimer _timer = new();
    [SerializeField] private bool _startTimerOnStart = true;

    private void Start()
    {
        _timer.OnTimeElapsed += TimeElapsed;
        if (_startTimerOnStart) _timer.StartTimer();
    }

    private void Update()
    {
        _timer.UpdateTimer(Time.deltaTime);
    }

    private void TimeElapsed()
    {
        Destroy(gameObject);
    }
}