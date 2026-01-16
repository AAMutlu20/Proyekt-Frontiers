using IrminStaticUtilities.Tools;
using UnityEngine;
using UnityEngine.Events;

public class YRotationSystem : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private bool _faceTarget = false;

    [SerializeField] private float _targetGroundRotationAngleDegrees;
    [SerializeField] private float _minRot;
    [SerializeField] private float _maxRot;

    [SerializeField] private float _rotationSpeedPerSecond;

    [SerializeField] protected float _rotationAngleDeadZone = 5.0f;

    [SerializeField] private bool _inRotationDeadzone = false;

    public UnityEvent OnEnteredRotationDeadzone;
    public UnityEvent OnExitedRotationDeadzone;

    public Transform Target { get  { return _target; } set { _target = value; } }

    private void Update()
    {
        if (_faceTarget && _target != null)
        {

            SetTargetRotationMinMaxRot(_target);
            UpdateMovementRotation();
        }
    }

    public void SetTargetRotationMinMaxRot(Transform pTransform)
    {
        _targetGroundRotationAngleDegrees = GetLookAtRotation(transform, pTransform).eulerAngles.y;
        UpdateMinMaxRot();
    }

    public void ClearTarget()
    {
        _target = null;
    }

    private void UpdateMinMaxRot()
    {
        _minRot = EulerRotationUtility.ConvertTo360DegreeAngle(_targetGroundRotationAngleDegrees - (_rotationAngleDeadZone / 2));
        _maxRot = EulerRotationUtility.ConvertTo360DegreeAngle(_targetGroundRotationAngleDegrees + (_rotationAngleDeadZone / 2));
    }

    private void UpdateMovementRotation()
    {
        Debug.Log("Updating rotation");
        if (EulerRotationUtility.CheckIfBetweenAngles(transform.rotation.eulerAngles.y, _minRot, _maxRot))
        {
            if (!_inRotationDeadzone)
            {
                _inRotationDeadzone = true;
                OnEnteredRotationDeadzone?.Invoke();
            }
            //Debug.Log("Player Character within deadzone");
            return;
        }
        else
        {
            if (_inRotationDeadzone)
            {
                _inRotationDeadzone = false;
                OnExitedRotationDeadzone?.Invoke();
            }
        }

        if (EulerRotationUtility.RotateBackOrForward(transform.rotation.eulerAngles.y, _targetGroundRotationAngleDegrees))
        {
            transform.Rotate(0, _rotationSpeedPerSecond * Time.deltaTime, 0);
        }
        else
        {
            transform.Rotate(0, -_rotationSpeedPerSecond * Time.deltaTime, 0);
        }
    }

    Quaternion GetLookAtRotation(Transform from, Transform to)
    {
        Vector3 direction = to.position - from.position;
        return Quaternion.LookRotation(direction, Vector3.up);
    }
}