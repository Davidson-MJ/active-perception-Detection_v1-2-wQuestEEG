using UnityEditor;
using UnityEditor.EventSystems;
using UnityEngine;

namespace Passer {

    [CanEditMultipleObjects]
    [CustomEditor(typeof(TeleportTarget))]
    public class TeleportTarget_Editor : EventTriggerEditor {
        protected SerializedProperty transformToTeleportProp;
        protected SerializedProperty teleportRootProp;
        protected SerializedProperty checkCollisionProp;
        protected SerializedProperty transportTypeProp;
        protected SerializedProperty targetPosRotProp;
        protected SerializedProperty targetTransformProp;
#if pHUMANOID
        protected SerializedProperty poseProp;
        protected SerializedProperty enableFootAnimatorProp;
#endif
        protected SerializedProperty unityEventsProp;

        protected override void OnEnable() {
            base.OnEnable();

            transformToTeleportProp = serializedObject.FindProperty("transformToTeleport");
            teleportRootProp = serializedObject.FindProperty("teleportRoot");
            checkCollisionProp = serializedObject.FindProperty("checkCollision");
            transportTypeProp = serializedObject.FindProperty("movementType");
            targetPosRotProp = serializedObject.FindProperty("targetPosRot");
            targetTransformProp = serializedObject.FindProperty("targetTransform");
#if pHUMANOID
            poseProp = serializedObject.FindProperty("pose");
            enableFootAnimatorProp = serializedObject.FindProperty("enableAnimators");
#endif
            unityEventsProp = serializedObject.FindProperty("unityEvents");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            InspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void InspectorGUI() {
            teleportRootProp.boolValue = EditorGUILayout.Toggle("Teleport Root", teleportRootProp.boolValue);
            checkCollisionProp.boolValue = EditorGUILayout.Toggle("Check Collision", checkCollisionProp.boolValue);
            transportTypeProp.intValue = (int)(MovementType)EditorGUILayout.EnumPopup("Movement Type", (MovementType)transportTypeProp.intValue);
            targetPosRotProp.intValue = (int)(TeleportTarget.TargetPosRot)EditorGUILayout.EnumPopup("Target Pos/Rot", (TeleportTarget.TargetPosRot)targetPosRotProp.intValue);
            if (targetPosRotProp.intValue == (int)TeleportTarget.TargetPosRot.Transform) {
                targetTransformProp.objectReferenceValue = (Transform)EditorGUILayout.ObjectField("Target Transform", targetTransformProp.objectReferenceValue, typeof(Transform), true);
            }
#if pHUMANOID
            poseProp.objectReferenceValue = (Humanoid.Pose)EditorGUILayout.ObjectField("Pose", poseProp.objectReferenceValue, typeof(Humanoid.Pose), false);
            enableFootAnimatorProp.boolValue = EditorGUILayout.Toggle("Enable Foot Animator", enableFootAnimatorProp.boolValue);
#endif

            EditorGUILayout.PropertyField(unityEventsProp, new GUIContent("OnTeleported"));
        }
    }

}
