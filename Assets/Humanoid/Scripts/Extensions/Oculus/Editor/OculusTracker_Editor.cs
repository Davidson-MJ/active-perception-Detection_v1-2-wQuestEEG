using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Passer.Humanoid {
    using Passer.Tracking;

    [CustomEditor(typeof(Passer.Tracking.OculusTracker))]
    public class OculusTracker_Editor : Editor {

        public override void OnInspectorGUI() {
            serializedObject.Update();

            PersistentTrackingInspector();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void PersistentTrackingInspector() {
            SerializedProperty persistentTrackingProp = serializedObject.FindProperty("persistentTracking");
            persistentTrackingProp.boolValue = EditorGUILayout.Toggle("Persistent Tracking", persistentTrackingProp.boolValue);

            if (persistentTrackingProp.boolValue) {
                EditorGUI.indentLevel++;
                RealWorldConfigurationInspector();
                EditorGUI.indentLevel--;
            }
        }

        protected virtual void RealWorldConfigurationInspector() {
            SerializedProperty realWorldConfigurationProp = serializedObject.FindProperty("realWorldConfiguration");
            EditorGUILayout.ObjectField(realWorldConfigurationProp);
            RealWorldConfiguration configuration = (RealWorldConfiguration)realWorldConfigurationProp.objectReferenceValue;

            if (configuration == null) {
                EditorGUILayout.HelpBox("Real World Configuration is required for persistent Tracking", MessageType.Warning);
                return;
            }

            RealWorldConfiguration.TrackingSpace trackingSpace =
                configuration.trackers.Find(space => space.trackerId == TrackerId.Oculus);
            if (trackingSpace == null)
                return;

            Passer.Tracking.OculusTracker tracker = (Passer.Tracking.OculusTracker) serializedObject.targetObject;
            tracker.transform.position = trackingSpace.position;
            tracker.transform.rotation = trackingSpace.rotation;
        }
    }
}