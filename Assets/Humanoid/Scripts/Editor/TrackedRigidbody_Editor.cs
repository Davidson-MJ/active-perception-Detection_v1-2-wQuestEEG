using UnityEditor;
using UnityEngine;

namespace Passer {

    [CustomEditor(typeof(TrackedRigidbody))]
    public class TrackedRigidbody_Editor : Editor {

        public override void OnInspectorGUI() {
            serializedObject.Update();

            SensorInspector();
            HybridPhysics.PhysicsMode physicsMode = PhysicsModeInspector();
            if (physicsMode != HybridPhysics.PhysicsMode.Kinematic) {
                StrengthInspector();
                if (physicsMode == HybridPhysics.PhysicsMode.HybridKinematic)
                    KinematicMass();
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void SensorInspector() {
            SerializedProperty sensorComponentProp = serializedObject.FindProperty("target");
            Transform sensorTransform = (Transform)sensorComponentProp.objectReferenceValue;
            SensorComponent sensorComponent =
                sensorTransform != null ?
                sensorTransform.GetComponent<SensorComponent>() :
                null;
            sensorComponent = (SensorComponent)EditorGUILayout.ObjectField("Sensor", sensorComponent, typeof(SensorComponent), true);
            sensorComponentProp.objectReferenceValue =
                sensorComponent != null ?
                sensorComponent.transform :
                null;
        }

        protected virtual HybridPhysics.PhysicsMode PhysicsModeInspector() {
            SerializedProperty modeProp = serializedObject.FindProperty("mode");
            HybridPhysics.PhysicsMode physicsMode = (HybridPhysics.PhysicsMode)EditorGUILayout.EnumPopup("Physics Mode", (HybridPhysics.PhysicsMode)modeProp.intValue);
            modeProp.intValue = (int)physicsMode;
            return physicsMode;
        }

        protected virtual void StrengthInspector() {
            SerializedProperty strengthProp = serializedObject.FindProperty("strength");
            strengthProp.floatValue = EditorGUILayout.FloatField("Strength", strengthProp.floatValue);
        }

        protected virtual void KinematicMass() {
            SerializedProperty kinematicMassProp = serializedObject.FindProperty("kinematicMass");
            kinematicMassProp.floatValue = EditorGUILayout.FloatField("Kinematic Mass", kinematicMassProp.floatValue);
        }
    }

}