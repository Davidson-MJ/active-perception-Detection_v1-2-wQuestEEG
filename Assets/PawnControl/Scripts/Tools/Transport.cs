using System.Collections;
using UnityEngine;

namespace Passer {
    public enum MovementType {
        Teleport,
        Dash,
        Walk
    }

    public class Transport {

        /// <summary>
        /// Dashes the transform to the target position
        /// This needs to be called using MonoBehaviour.StartCoroutine
        /// </summary>
        /// <param name="transform">The transform to move</param>
        /// <param name="targetPosition">The target position of the dash movement</param>
        /// <param name="duration">Duration of the dash</param>
        /// <param name="minSpeed">Minimum speed for the dash, duration may be shortened to reach this speed</param>
        /// <returns>Coroutine result</returns>
        public static IEnumerator DashCoroutine(Transform transform, Vector3 targetPosition, float duration = 0.1F, float minSpeed = 5) {
            float distance = Vector3.Distance(transform.position, targetPosition - transform.forward * 0.5f);

            float minDistance = minSpeed / 0.1F;
            if (distance < minDistance)
                duration = distance / minSpeed;

            Vector3 startPosition = transform.position;
            float elapsedTime = 0;
            float t = 0;

            while (t < 1) {
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                transform.LookAt(targetPosition);
                elapsedTime += Time.deltaTime;
                t = elapsedTime / duration;
                yield return new WaitForEndOfFrame();
            }
            transform.position = targetPosition;
        }

        public static IEnumerator LookAt(Transform transform, Vector3 targetPosition, float rotationSpeed = 60) {
            Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
            yield return RotateTo(transform, targetRotation, rotationSpeed);
        }

        public static IEnumerator RotateTo(Transform transform, Quaternion targetRotation, float rotationSpeed = 60) {
            float angle;
            float maxAngle;
            do {
                maxAngle = rotationSpeed * Time.deltaTime;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxAngle);
                angle = Quaternion.Angle(transform.rotation, targetRotation);
                yield return new WaitForFixedUpdate();
            } while (angle > maxAngle);
        }


        public static IEnumerator WalkCoroutine(Transform transform, Vector3 targetPosition, float maxSpeed = 1) {
            yield return LookAt(transform, targetPosition, 60);

            while (Vector3.Distance(transform.position, targetPosition) > 0.03F) {
                transform.MoveForward(maxSpeed * Time.deltaTime);
                yield return new WaitForEndOfFrame();
            }
            transform.position = targetPosition;
        }
        public static IEnumerator WalkCoroutine(Transform transform, Vector3 targetPosition, Quaternion targetRotation, float maxSpeed = 1) {
            yield return LookAt(transform, targetPosition, 60);

            while (Vector3.Distance(transform.position, targetPosition) > 0.03F) {
                transform.MoveForward(maxSpeed * Time.deltaTime);
                yield return new WaitForEndOfFrame();
            }
            transform.position = targetPosition;

            yield return RotateTo(transform, targetRotation, 60);
        }
    }

    public static class TransformTransport {
        public static void MoveTo(this Transform transform, Vector3 position, MovementType movementType = MovementType.Teleport) {
            MonoBehaviour monoBehaviour = transform.GetComponent<MonoBehaviour>();

            switch (movementType) {
                case MovementType.Teleport:
                    TransformMovements.Teleport(transform, position);
                    break;
                case MovementType.Dash:
                    if (monoBehaviour == null) {
                        Debug.LogError("Dash not possible. No MonoBehaviour found on " + transform);
                    }
                    else
                        monoBehaviour.StartCoroutine(TransformMovements.DashCoroutine(transform, position));
                    break;
                case MovementType.Walk:
                    if (monoBehaviour == null) {
                        Debug.LogError("Walk not possible. No MonoBehaviour found on " + transform);
                    }
                    else
                        monoBehaviour.StartCoroutine(Transport.WalkCoroutine(transform, position));
                    break;
                default:
                    break;
            }

        }
    }
}