using UnityEditor;
using UnityEngine;

namespace Passer.Humanoid {

    [HelpURL("http://passervr.com/documentation/humanoid-control/object-tracker/")]
    [CustomEditor(typeof(ObjectTracker))]
    public class ObjectTracker_Editor : Editor {
        public enum Side {
            Left,
            Right
        }

        protected ObjectTracker objectTracker;

        #region Enable
        public void OnEnable() {
            objectTracker = (ObjectTracker)target;

            //if (objectTracker.targetTransform == null)
            //    objectTracker.targetTransform = objectTracker.transform;
            //            HumanoidControl humanoid = objectTracker.humanoid;
            //            if (humanoid != null) {
            //#if hOCULUS
            //                objectTracker.oculusTouch.tracker = humanoid.oculus;
            //#endif
            //            }

            //#if hOCULUS
            //            if (objectTracker.oculusTouch.target == null)
            //                objectTracker.oculusTouch.target = objectTracker;
            //#endif
        }
        #endregion

        #region Disable

        public void OnDisable() {
            if (!Application.isPlaying) {
                serializedObject.Update();

                SetSensor2Object();

                serializedObject.ApplyModifiedProperties();
            }
        }

        protected void SetSensor2Object() {
            SensorComponent sensorComponent = objectTracker.sensorComponent;
            if (sensorComponent == null)
                return;

            SerializedProperty sensor2ObjectRotationProp = serializedObject.FindProperty("sensor2ObjectRotation");
            sensor2ObjectRotationProp.quaternionValue = Quaternion.Inverse(sensorComponent.transform.rotation) * objectTracker.transform.rotation;

            SerializedProperty sensor2ObjectPositionProp = serializedObject.FindProperty("sensor2ObjectPosition");
            sensor2ObjectPositionProp.vector3Value = -InverseTransformPointUnscaled(objectTracker.transform, sensorComponent.transform.position);
        }

        private static Vector3 InverseTransformPointUnscaled(Transform transform, Vector3 position) {
            var worldToLocalMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
            return worldToLocalMatrix.MultiplyPoint3x4(position);
        }

        #endregion

        #region Inspector

        public override void OnInspectorGUI() {
            ObjectTracker objectTracker = (ObjectTracker)target;
            serializedObject.Update();

            SerializedProperty sensorComponentProp = serializedObject.FindProperty("sensorComponent");
            sensorComponentProp.objectReferenceValue = (SensorComponent)EditorGUILayout.ObjectField("Sensor Component", sensorComponentProp.objectReferenceValue, typeof(SensorComponent), true);

            Settings(objectTracker);

            serializedObject.ApplyModifiedProperties();
        }

        #region Settings

        private static bool showSettings = false;
        public static void Settings(ObjectTracker objectTracker) {
            showSettings = EditorGUILayout.Foldout(showSettings, "Settings", true);
            if (showSettings) {
                EditorGUI.indentLevel++;
                objectTracker.showRealObjects = EditorGUILayout.Toggle("Show Real Objects", objectTracker.showRealObjects);
                ShowTrackers(objectTracker, objectTracker.showRealObjects);
                // Work in progress
                //objectTracker.physics = EditorGUILayout.Toggle("Physics", objectTracker.physics);
                EditorGUI.indentLevel--;
            }
        }

        private static void ShowTrackers(ObjectTracker objectTracker, bool shown) {
            objectTracker.ShowTrackers(shown);
        }

        #endregion

        #endregion

        #region Scene
        //private Vector3 objectPosition;
        //private Quaternion objectLocalRotation;
        public void OnSceneGUI() {
            ObjectTracker objectTracker = (ObjectTracker)target;

            //if (!Application.isPlaying)
            //    objectTracker.UpdateSensorsFromTarget();
            
        }
        #endregion

    }
}