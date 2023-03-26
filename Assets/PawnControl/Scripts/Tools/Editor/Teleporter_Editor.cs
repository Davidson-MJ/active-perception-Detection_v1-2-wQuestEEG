using UnityEditor;
using UnityEngine;

namespace Passer {
#if pHUMANOID
    using Humanoid;
#endif

    [CustomEditor(typeof(Teleporter))]
    public class Teleporter_Editor : InteractionPointer_Editor {
        protected Teleporter teleporter;

        protected SerializedProperty transportTypeProp;

        #region Enable
        public override void OnEnable() {
            base.OnEnable();
            teleporter = (Teleporter)target;

#if pHUMANOID
            teleporter.transformToTeleport = FindDeepParentComponent(teleporter.transform, typeof(HumanoidControl));
#else
            teleporter.transformToTeleport = FindDeepParentComponent(teleporter.transform, typeof(Pawn.PawnControl));
#endif
            teleporter.clickEvent.SetMethod(EventHandler.Type.OnStart, teleporter.TeleportTransform);

            transportTypeProp = serializedObject.FindProperty("transportType");
        }

        protected Transform FindDeepParentComponent(Transform t, System.Type type) {
            Component component = t.GetComponent(type.Name);
            if (component == null) {
                if (t.parent != null)
                    return FindDeepParentComponent(t.parent, type);
                else
                    return null;
            } else
                return t;
        }
        #endregion

        #region Inspector
        public override void OnInspectorGUI() {
            serializedObject.Update();

            pointer.active = EditorGUILayout.Toggle("Active", pointer.active);
            pointer.timedClick = EditorGUILayout.FloatField("Timed teleport", pointer.timedClick);
            pointer.focusPointObj = (GameObject)EditorGUILayout.ObjectField("Target Point Object", pointer.focusPointObj, typeof(GameObject), true);

            pointerModeProp.intValue = (int)(InteractionPointer.RayType)EditorGUILayout.EnumPopup("Mode", (InteractionPointer.RayType)pointerModeProp.intValue);
            transportTypeProp.intValue = (int)(Teleporter.TransportType)EditorGUILayout.EnumPopup("Transport Type", (Teleporter.TransportType)transportTypeProp.intValue);

            if (pointer.rayType == InteractionPointer.RayType.Straight) {
                maxDistanceProp.floatValue = EditorGUILayout.FloatField("Maximum Distance", maxDistanceProp.floatValue);
            }
            if (pointer.rayType == InteractionPointer.RayType.Bezier) {
                maxDistanceProp.floatValue = EditorGUILayout.FloatField("Maximum Distance", maxDistanceProp.floatValue);
                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                resolutionProp.floatValue = EditorGUILayout.FloatField("Resolution", resolutionProp.floatValue);
                EditorGUI.EndDisabledGroup();
            }
            if (pointer.rayType == InteractionPointer.RayType.Gravity) {
                speedProp.floatValue = EditorGUILayout.FloatField("Speed", speedProp.floatValue);
                resolutionProp.floatValue = EditorGUILayout.FloatField("Resolution", resolutionProp.floatValue);
            }
            else if (pointer.rayType == InteractionPointer.RayType.SphereCast) {
                maxDistanceProp.floatValue = EditorGUILayout.FloatField("Maximum Distance", maxDistanceProp.floatValue);
                radiusProp.floatValue = EditorGUILayout.FloatField("Radius", radiusProp.floatValue);
            }
            serializedObject.ApplyModifiedProperties();
        }
        #endregion
    }
}
