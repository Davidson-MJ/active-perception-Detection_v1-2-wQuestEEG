using UnityEditor;
using UnityEngine;

namespace Passer.Pawn {

    public class UnityVR_Editor { //: Tracker_Editor {
        #region Tracker

#if pUNITYXR
        public static void CleanupFirstPersonCamera(PawnControl pawn) {
            RemoveCamera(pawn.headTarget.transform);
        }

        public static void RemoveCamera(Transform targetTransform) {
            Camera camera = targetTransform.GetComponentInChildren<Camera>();
            if (camera != null) {
                if (Application.isPlaying)
                    Object.Destroy(camera.gameObject);
                else
                    Object.DestroyImmediate(camera.gameObject);
            }
        }

#endif

        #endregion

        #region Head

        //public class HandTargetProps : HandTarget_Editor.TargetProps {
        //    public HandTargetProps(SerializedObject serializedObject, HandTarget handTarget)
        //        : base(serializedObject, handTarget.unity, handTarget, "unity") {

        //    }
        //}

        #endregion

        #region Hand
        #endregion
    }

}