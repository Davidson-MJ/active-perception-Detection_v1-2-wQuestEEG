#if hOCULUS
using UnityEngine;
using UnityEditor;

namespace Passer.Humanoid {

    public class OculusObject_Editor {

        #region Object Tracker
        private enum Side {
            Left,
            Right
        }

        public static void ObjectTrackerInspector(ObjectTracker objectTracker) {
//#if hOCULUS
//#if UNITY_STANDALONE_WIN
//            bool wasEnabled = objectTracker.oculusTouch.enabled;
//            SensorInspector(objectTracker.oculusTouch, "Oculus Touch");
//            if (!wasEnabled && objectTracker.oculusTouch.enabled)
//                //objectTracker.oculusTouch.Start(objectTracker.humanoid, objectTracker.transform);

//            if (objectTracker.oculusTouch.enabled) {
//                EditorGUI.indentLevel++;
//                Side side = objectTracker.isLeft ? Side.Left : Side.Right;
//                side = (Side)EditorGUILayout.EnumPopup("Controller side", side);
//                objectTracker.isLeft = (side == Side.Left);

//                objectTracker.oculusTouch.CheckSensorTransform();
//                objectTracker.oculusTouch.SetSensor2Target();

//                objectTracker.oculusTouch.sensorTransform = (Transform)EditorGUILayout.ObjectField("Tracker Transform", objectTracker.oculusTouch.sensorTransform, typeof(Transform), true);
//                EditorGUI.indentLevel--;
//            }
//            else {
//                objectTracker.oculusTouch.sensorTransform = Oculus_Editor.RemoveTransform(objectTracker.oculusTouch.sensorTransform);
//            }
//#elif UNITY_ANDROID
//            bool wasEnabled = objectTracker.oculusTouch.enabled;
//            SensorInspector(objectTracker.oculusTouch, "Gear VR Controller");
//            if (!wasEnabled && objectTracker.oculusTouch.enabled)
//                objectTracker.oculusTouch.Start(objectTracker.humanoid, objectTracker.transform);

//            if (objectTracker.oculusTouch.enabled) {
//                EditorGUI.indentLevel++;
//                Side side = objectTracker.isLeft ? Side.Left : Side.Right;
//                side = (Side)EditorGUILayout.EnumPopup("Controller side", side);
//                objectTracker.isLeft = (side == Side.Left);
//                //objectTracker.gearVRController.sensorTransform = (Transform)EditorGUILayout.ObjectField("Tracker Transform", objectTracker.gearVRController.sensorTransform, typeof(Transform), true);
//                EditorGUI.indentLevel--;
//            } else {
//                //objectTracker.gearVRController.sensorTransform = OculusTouch_Editor.RemoveOculusTracker(objectTracker.gearVRController.sensorTransform);       
//            }
//#endif
//#endif
        }

        public static void SensorInspector(UnitySensor sensor, string name) {
            EditorGUILayout.BeginHorizontal();
            sensor.enabled = EditorGUILayout.ToggleLeft(name, sensor.enabled, GUILayout.MinWidth(80));
            if (sensor.enabled && Application.isPlaying)
                EditorGUILayout.EnumPopup(sensor.status);
            EditorGUILayout.EndHorizontal();
        }
        #endregion
    }
}
#endif