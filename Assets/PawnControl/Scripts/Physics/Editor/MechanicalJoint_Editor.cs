using UnityEngine;
using UnityEditor;

namespace Passer {
    [CustomEditor(typeof(MechanicalJoint), true)]
    public class MechanicalJoint_Editor : Editor {
        MechanicalJoint mechanicalJoint;

        SerializedProperty limitXProp;
        SerializedProperty limitYProp;
        SerializedProperty limitZProp;

        SerializedProperty basePositionProp;
        SerializedProperty minLocalPositionProp;
        SerializedProperty maxLocalPositionProp;

        SerializedProperty baseRotationProp;

        GUIContent m_IconToolbarMinus;
        GUIContent m_EventIDName;
        GUIContent[] m_EventTypes;
        GUIContent m_AddButonContent;

        #region Enable

        protected virtual void OnEnable() {
            mechanicalJoint = (MechanicalJoint)target;

            limitXProp = serializedObject.FindProperty("limitX");
            limitYProp = serializedObject.FindProperty("limitY");
            limitZProp = serializedObject.FindProperty("limitZ");

            basePositionProp = serializedObject.FindProperty("basePosition");
            minLocalPositionProp = serializedObject.FindProperty("minLocalPosition");
            maxLocalPositionProp = serializedObject.FindProperty("maxLocalPosition");

            baseRotationProp = serializedObject.FindProperty("baseRotation");
            //limitAngleProp = serializedObject.FindProperty("limitAngle");
            //maxLocalAngleProp = serializedObject.FindProperty("maxLocalAngle");
            //limitAxisProp = serializedObject.FindProperty("limitAngleAxis");

            InitEvents();
        }

        #endregion

        #region Inspector

        public override void OnInspectorGUI() {
            Rigidbody rb = mechanicalJoint.GetComponent<Rigidbody>();
            if (rb == null || !rb.isKinematic)
                EditorGUILayout.HelpBox("Rigidbody Limitations should be used with a Kinematic Rigidbody", MessageType.Warning);

            serializedObject.Update();

            Vector3 minLimits = minLocalPositionProp.vector3Value;
            Vector3 maxLimits = maxLocalPositionProp.vector3Value;

            EditorGUILayout.BeginHorizontal();
            limitXProp.boolValue = EditorGUILayout.ToggleLeft("Limit Position X", limitXProp.boolValue, GUILayout.MinWidth(110));
            EditorGUI.BeginDisabledGroup(!limitXProp.boolValue);
            EditorGUILayout.LabelField("Min", GUILayout.Width(30));
            float minX = EditorGUILayout.FloatField(minLimits.x);
            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
            float maxX = EditorGUILayout.FloatField(maxLimits.x);
            if (maxX < minX) {
                float x = minX;
                minX = maxX;
                maxX = x;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            limitYProp.boolValue = EditorGUILayout.ToggleLeft("Limit Position Y", limitYProp.boolValue, GUILayout.MinWidth(110));
            EditorGUI.BeginDisabledGroup(!limitYProp.boolValue);
            EditorGUILayout.LabelField("Min", GUILayout.Width(30));
            float minY = EditorGUILayout.FloatField(minLimits.y);
            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
            float maxY = EditorGUILayout.FloatField(maxLimits.y);
            if (maxY < minY) {
                float y = minY;
                minY = maxY;
                maxY = y;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            limitZProp.boolValue = EditorGUILayout.ToggleLeft("Limit Position Z", limitZProp.boolValue, GUILayout.MinWidth(110));
            EditorGUI.BeginDisabledGroup(!limitZProp.boolValue);
            EditorGUILayout.LabelField("Min", GUILayout.Width(30));
            float minZ = EditorGUILayout.FloatField(minLimits.z);
            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
            float maxZ = EditorGUILayout.FloatField(maxLimits.z);
            if (maxZ < minZ) {
                float z = minZ;
                minZ = maxZ;
                maxZ = z;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            minLocalPositionProp.vector3Value = new Vector3(minX, minY, minZ);
            maxLocalPositionProp.vector3Value = new Vector3(maxX, maxY, maxZ);

            RotationLimitationsInspector();

            EventsInspector();

            if (!Application.isPlaying) {
                basePositionProp.vector3Value = mechanicalJoint.transform.localPosition;
                baseRotationProp.quaternionValue = mechanicalJoint.transform.localRotation;
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void RotationLimitationsInspector() {
            EditorGUILayout.BeginHorizontal();

            SerializedProperty limitAngleProp = serializedObject.FindProperty("limitAngle");

            limitAngleProp.boolValue = EditorGUILayout.ToggleLeft("Limit Angle", limitAngleProp.boolValue, GUILayout.MinWidth(110));
            EditorGUI.BeginDisabledGroup(!limitAngleProp.boolValue);

            SerializedProperty minLocalAngleProp = serializedObject.FindProperty("minLocalAngle");
            EditorGUILayout.LabelField("Min", GUILayout.Width(30));
            float minAngle = EditorGUILayout.FloatField(minLocalAngleProp.floatValue);

            SerializedProperty maxLocalAngleProp = serializedObject.FindProperty("maxLocalAngle");
            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
            float maxAngle = EditorGUILayout.FloatField(maxLocalAngleProp.floatValue);

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying) {
                if (minAngle > 0)
                    minAngle = -minAngle;
                if (maxAngle < 0)
                    maxAngle = -maxAngle;

                minLocalAngleProp.floatValue = minAngle;
                maxLocalAngleProp.floatValue = maxAngle;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.MinWidth(110));
            EditorGUILayout.LabelField("Axis", GUILayout.Width(30));
            SerializedProperty limitAxisProp = serializedObject.FindProperty("limitAngleAxis");
            limitAxisProp.vector3Value = EditorGUILayout.Vector3Field("", limitAxisProp.vector3Value, GUILayout.MinWidth(200));
            EditorGUILayout.EndHorizontal();

        }

        #region Events
        protected SerializedProperty gameObjectEventProp;
        protected SerializedProperty xSliderEventProp;
        protected SerializedProperty ySliderEventProp;
        protected SerializedProperty zSliderEventProp;

        protected virtual void InitEvents() {
            gameObjectEventProp = serializedObject.FindProperty("gameObjectEvent");
            mechanicalJoint.gameObjectEvent.id = 0;
            xSliderEventProp = serializedObject.FindProperty("xSliderEvents");
            mechanicalJoint.xSliderEvents.id = 1;
            ySliderEventProp = serializedObject.FindProperty("ySliderEvents");
            mechanicalJoint.ySliderEvents.id = 2;
            zSliderEventProp = serializedObject.FindProperty("zSliderEvents");
            mechanicalJoint.zSliderEvents.id = 3;
        }

        protected int selectedEventSource = -1;
        protected int selectedEvent;

        protected bool showEvents;
        protected virtual void EventsInspector() {
            showEvents = EditorGUILayout.Foldout(showEvents, "Events", true);
            if (showEvents) {
                EditorGUI.indentLevel++;

                SerializedProperty gameObjectEventProp = serializedObject.FindProperty("gameObjectEvent");
                GameObjectEvent_Editor.EventInspector(gameObjectEventProp, mechanicalJoint.gameObjectEvent, ref selectedEventSource, ref selectedEvent);

                FloatEvent_Editor.EventInspector(xSliderEventProp, mechanicalJoint.xSliderEvents, ref selectedEventSource, ref selectedEvent);
                FloatEvent_Editor.EventInspector(ySliderEventProp, mechanicalJoint.ySliderEvents, ref selectedEventSource, ref selectedEvent);
                FloatEvent_Editor.EventInspector(zSliderEventProp, mechanicalJoint.zSliderEvents, ref selectedEventSource, ref selectedEvent);

                SerializedProperty angleEventProp = serializedObject.FindProperty("angleEvents");
                FloatEvent_Editor.EventInspector(angleEventProp, mechanicalJoint.angleEvents, ref selectedEventSource, ref selectedEvent);

                EditorGUI.indentLevel--;
            }
        }

        #endregion

        #endregion

        #region Scene
        public void OnSceneGUI() {
            if (mechanicalJoint == null)
                return;

            if (!mechanicalJoint.isActiveAndEnabled)
                return;

            if (mechanicalJoint.limitAngle)
                DrawArc(mechanicalJoint.transform, mechanicalJoint.limitAngleAxis, mechanicalJoint.minLocalAngle, mechanicalJoint.maxLocalAngle);
        }

        private void DrawArc(Transform transform, Vector3 axis, float minAngle, float maxAngle) {
            Vector3 worldAxis = transform.rotation * axis;

            // Any direction orthogonal to the axis is ok for the zeroDirection,
            // but we choose to have Z when axis is Y or X.
            // All other zeroDirections are derived from that

            Vector3 orthoDirection = Vector3.up;
            float angle = Vector3.Angle(axis, orthoDirection);
            if (angle == 0 || angle == 180)
                orthoDirection = -Vector3.right;

            axis = baseRotationProp.quaternionValue * axis;
            orthoDirection = baseRotationProp.quaternionValue * orthoDirection;
            Vector3 zeroDirection = Vector3.Cross(axis, orthoDirection);
            if (transform.parent != null)
                zeroDirection = transform.parent.rotation * zeroDirection;

            float size = HandleUtility.GetHandleSize(transform.position) * 2;
            Handles.color = Color.yellow;
            Handles.DrawLine(transform.position, transform.position + worldAxis * size);
            Handles.DrawLine(transform.position, transform.position + zeroDirection * size);

            Handles.DrawWireArc(transform.position, worldAxis, zeroDirection, minAngle, size);
            Handles.DrawWireArc(transform.position, worldAxis, zeroDirection, maxAngle, size);
            Handles.color = new Color(1, 0.92F, 0.016F, 0.1F); // transparant yellow
            Handles.DrawSolidArc(transform.position, worldAxis, zeroDirection, minAngle, size);
            Handles.DrawSolidArc(transform.position, worldAxis, zeroDirection, maxAngle, size);

            Handles.color = Color.yellow;
            Vector3 spherePointMin = transform.position + Quaternion.AngleAxis(minAngle, worldAxis) * zeroDirection * size;
            Handles.SphereHandleCap(0, spherePointMin, Quaternion.identity, 0.05F * size, EventType.Repaint);
            Vector3 spherePointMax = transform.position + Quaternion.AngleAxis(maxAngle, worldAxis) * zeroDirection * size;
            Handles.SphereHandleCap(0, spherePointMax, Quaternion.identity, 0.05F * size, EventType.Repaint);
        }
        #endregion

    }
}