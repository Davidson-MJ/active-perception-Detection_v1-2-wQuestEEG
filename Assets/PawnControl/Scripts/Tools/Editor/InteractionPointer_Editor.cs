using UnityEditor;
using UnityEngine;

namespace Passer {

    [CustomEditor(typeof(InteractionPointer))]
    public class InteractionPointer_Editor : Editor {
        protected InteractionPointer pointer;

        protected SerializedProperty timedClickProp;
        protected SerializedProperty pointerModeProp;
        protected SerializedProperty maxDistanceProp;
        protected SerializedProperty resolutionProp;
        protected SerializedProperty speedProp;
        protected SerializedProperty radiusProp;


        #region Enable
        public virtual void OnEnable() {
            pointer = (InteractionPointer)target;

            timedClickProp = serializedObject.FindProperty("timedClick");
            pointerModeProp = serializedObject.FindProperty("rayType");
            maxDistanceProp = serializedObject.FindProperty("maxDistance");
            resolutionProp = serializedObject.FindProperty("resolution");
            speedProp = serializedObject.FindProperty("speed");
            radiusProp = serializedObject.FindProperty("radius");

            InitEvents();
        }
        #endregion

        #region Inspector
        public override void OnInspectorGUI() {
            serializedObject.Update();

            pointer.active = EditorGUILayout.Toggle("Active", pointer.active);
            timedClickProp.floatValue = EditorGUILayout.FloatField("Timed Click", timedClickProp.floatValue);
            pointer.focusPointObj = (GameObject)EditorGUILayout.ObjectField("Focus Point Object", pointer.focusPointObj, typeof(GameObject), true);

            EditorGUILayout.ObjectField("Object in Focus", pointer.objectInFocus, typeof(GameObject), true);

            pointerModeProp.intValue = (int)(InteractionPointer.RayType)EditorGUILayout.EnumPopup("Mode", (InteractionPointer.RayType)pointerModeProp.intValue);

            if (pointer.rayType == InteractionPointer.RayType.Straight) {
                maxDistanceProp.floatValue = EditorGUILayout.FloatField("Maximum Distance", maxDistanceProp.floatValue);
            }
            if (pointer.rayType == InteractionPointer.RayType.Bezier){
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

            EventsInspector();

            serializedObject.ApplyModifiedProperties();
        }

        #region Events
        protected SerializedProperty focusEventsProp;
        protected SerializedProperty focusPointEventsProp;
        protected SerializedProperty clickEventsProp;

        protected void InitEvents() {
            focusEventsProp = serializedObject.FindProperty("focusEvent");
            pointer.focusEvent.id = 0;
            focusPointEventsProp = serializedObject.FindProperty("focusPointEvent");
            pointer.focusPointEvent.id = 1;
            clickEventsProp = serializedObject.FindProperty("clickEvent");
            pointer.clickEvent.id = 2;
        }

        protected bool showEvents;
        protected int selectedEventSource = -1;
        protected int selectedEvent;

        protected void EventsInspector() {
            showEvents = EditorGUILayout.Foldout(showEvents, "Events", true);
            if (showEvents) {
                EditorGUI.indentLevel++;

                GameObjectEvent_Editor.EventInspector(focusEventsProp, pointer.focusEvent, ref selectedEventSource, ref selectedEvent);
                Vector3Event_Editor.EventInspector(focusPointEventsProp, pointer.focusPointEvent, ref selectedEventSource, ref selectedEvent);
                GameObjectEvent_Editor.EventInspector(clickEventsProp, pointer.clickEvent, ref selectedEventSource, ref selectedEvent);
                //EditorGUILayout.BeginHorizontal();

                ////Labels
                //EditorGUILayout.BeginVertical(GUILayout.MinWidth(110));

                //GUILayout.Space(3);
                //EditorGUILayout.LabelField("Focus", GUILayout.Width(110));
                //if (pointer.focusEvent.events == null || pointer.focusEvent.events.Count == 0)
                //    pointer.focusEvent.events.Add(new GameObjectEvent(Event.Type.Never));
                //for (int i = 1; i < pointer.focusEvent.events.Count; i++) {
                //    GUILayout.Space(1);
                //    EditorGUILayout.LabelField(" ", GUILayout.Width(140));
                //}

                //GUILayout.Space(1);
                //EditorGUILayout.LabelField("Focus Point", GUILayout.Width(110));
                //if (pointer.focusPointEvent.events == null || pointer.focusPointEvent.events.Count == 0)
                //    pointer.focusPointEvent.events.Add(new Vector3Event(Event.Type.Never));
                //for (int i = 1; i < pointer.focusPointEvent.events.Count; i++) {
                //    GUILayout.Space(1);
                //    EditorGUILayout.LabelField(" ", GUILayout.Width(140));
                //}

                //GUILayout.Space(1);
                //EditorGUILayout.LabelField("Click", GUILayout.Width(110));
                //if (pointer.clickEvent.events == null || pointer.clickEvent.events.Count == 0)
                //    pointer.clickEvent.events.Add(new GameObjectEvent(Event.Type.Never));
                //for (int i = 1; i < pointer.clickEvent.events.Count; i++) {
                //    GUILayout.Space(1);
                //    EditorGUILayout.LabelField(" ", GUILayout.Width(140));
                //}

                //EditorGUILayout.EndVertical();

                //// Buttons
                //int eventCount =
                //    pointer.focusEvent.events.Count +
                //    pointer.focusPointEvent.events.Count +
                //    pointer.clickEvent.events.Count;

                //string[] buttonTexts = new string[eventCount];
                //int[] eventListIndex = new int[eventCount];
                //SerializedProperty[] eventListProps = new SerializedProperty[eventCount];
                //int index = 0;
                //SerializedProperty eventsProp = focusEventsProp.FindPropertyRelative("events");
                //for (int i = 0; i < pointer.focusEvent.events.Count; i++) {
                //    buttonTexts[index] = Event.GetInputButtonLabel(pointer.focusEvent.events[i]);
                //    eventListIndex[index] = 0;
                //    eventListProps[index] = eventsProp.GetArrayElementAtIndex(i);
                //    index++;
                //}
                //eventsProp = focusPointEventsProp.FindPropertyRelative("events");
                //for (int i = 0; i < pointer.focusPointEvent.events.Count; i++) {
                //    buttonTexts[index] = Event.GetInputButtonLabel(pointer.focusPointEvent.events[i]);
                //    eventListIndex[index] = 1;
                //    eventListProps[index] = eventsProp.GetArrayElementAtIndex(i);
                //    index++;
                //}
                //eventsProp = clickEventsProp.FindPropertyRelative("events");
                //for (int i = 0; i < pointer.clickEvent.events.Count; i++) {
                //    buttonTexts[index] = Event.GetInputButtonLabel(pointer.clickEvent.events[i]);
                //    eventListIndex[index] = 2;
                //    eventListProps[index] = eventsProp.GetArrayElementAtIndex(i);
                //    index++;
                //}

                //int oldFontSize = GUI.skin.button.fontSize;
                //GUI.skin.button.fontSize = 9;
                //if (selectedEvent >= buttonTexts.Length)
                //    selectedEvent = -1;
                //selectedEvent = GUILayout.SelectionGrid(selectedEvent, buttonTexts, 1);
                //GUI.skin.button.fontSize = oldFontSize;

                //EditorGUILayout.EndHorizontal();

                //// Details
                //if (selectedEvent >= 0)
                //    EventDetails(eventListIndex[selectedEvent], eventListProps[selectedEvent]);

                EditorGUI.indentLevel--;
            }
        }

        //protected void EventDetails(int selectedEventList, SerializedProperty eventProp) {
        //    switch (selectedEventList) {
        //        case 0:
        //            GameObjectEvent_Editor.EventListDetails(pointer.focusEvents, eventProp);
        //            break;
        //        case 1:
        //            Vector3Event_Editor.EventListDetails(pointer.focusPointEvents, eventProp);
        //            break;
        //        case 2:
        //            GameObjectEvent_Editor.EventListDetails(pointer.clickEvents, eventProp);
        //            break;
        //    }
        //}
        #endregion

        #endregion

    }
}