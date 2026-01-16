using System;
using UnityEngine;

namespace IrminStaticUtilities.Tools
{
    public static class EulerRotationUtility
    {
        public static float ConvertTo360DegreeAngle(float pDegrees)
        {

            float leftOverDegrees = pDegrees % 360;
            if (leftOverDegrees < 0)
            {
                // Plus because leftOverDegrees is a minus number. - - results in plus.
                leftOverDegrees = 360 + leftOverDegrees;
            }
            return leftOverDegrees;
        }

        public static bool CheckIfBetweenAngles(float pAngle, float pMinAngle, float pMaxAngle)
        {
            if (pMinAngle > pMaxAngle)
            {
                if (pAngle > pMinAngle || pAngle < pMaxAngle) return true;
            }
            else
            {
                if (pAngle > pMinAngle && pAngle < pMaxAngle) return true;
            }
            return false;
        }


        public static bool RotateBackOrForward(float pCurrentRotationAngle, float pTargetRotationAngle)
        {
            float currentThreeSixtyAngle = ConvertTo360DegreeAngle(pCurrentRotationAngle);
            float backwardDistance;
            float forwardDistance;

            // Caculate rotation distances.
            if (pTargetRotationAngle < currentThreeSixtyAngle)
            {
                backwardDistance = currentThreeSixtyAngle - pTargetRotationAngle;
            }
            else
            {
                backwardDistance = currentThreeSixtyAngle + 380 - pTargetRotationAngle;
            }

            if (pTargetRotationAngle > currentThreeSixtyAngle)
            {
                forwardDistance = pTargetRotationAngle - currentThreeSixtyAngle;
            }
            else
            {
                forwardDistance = 380 - currentThreeSixtyAngle + pTargetRotationAngle;
            }

            if (forwardDistance < backwardDistance)
            {
                return true;
            }
            else if (backwardDistance < forwardDistance)
            {
                return false;
            }
            else
            {
                bool randomBoolValue = UnityEngine.Random.Range(0, 2) != 0;
                return randomBoolValue;
            }
        }

        public static Vector3 GetMovementDirectionFromCamera(Transform pCameraTransform, Transform pMovingTransform, Vector3 pInput, out float pTargetGroundedRotationAngleRadians, out float pTargetGroundRotationAngleDegrees)
        {
            // Get the local position
            Vector3 fromTransformLookDirection = -(pCameraTransform.position - pMovingTransform.position).normalized;
            float cameraAngle = Mathf.Atan2(fromTransformLookDirection.x, fromTransformLookDirection.z);
            float inputAngle = Mathf.Atan2(pInput.x, pInput.y);
            float movementAngle = cameraAngle + inputAngle;
            pTargetGroundedRotationAngleRadians = movementAngle;
            pTargetGroundRotationAngleDegrees = EulerRotationUtility.ConvertTo360DegreeAngle(movementAngle * (180 / Mathf.PI));
            Vector3 movementDirection = new Vector3(MathF.Cos(movementAngle), 0, MathF.Sin(movementAngle));
            return movementDirection;
        }

        public static float GetYDegreesToFaceTarget(GameObject pOriginGameObject, GameObject pTargetGameObject)
        {
            Vector3 lookDirectionVector = (pTargetGameObject.transform.position - pOriginGameObject.transform.position);
            float radianAngle = Mathf.Atan2(lookDirectionVector.x, lookDirectionVector.z);
            return  radianAngle * Mathf.Rad2Deg;
        }

        public static float GetZDegreesToFaceTarget(GameObject pOriginGameObject, GameObject pTargetGameObject)
        {
            Vector3 lookDirectionVector = (pTargetGameObject.transform.position - pOriginGameObject.transform.position);
            float radianAngle = Mathf.Atan2(lookDirectionVector.y, lookDirectionVector.x);
            return radianAngle * Mathf.Rad2Deg;
        }

        public static float GetXDegreesToFaceTarget(GameObject pOriginGameObject, GameObject pTargetGameObject)
        {
            Vector3 lookDirectionVector = (pTargetGameObject.transform.position - pOriginGameObject.transform.position);
            float radianAngle = Mathf.Atan2(lookDirectionVector.y, lookDirectionVector.z);
            return -radianAngle * Mathf.Rad2Deg;
        }
    }
}