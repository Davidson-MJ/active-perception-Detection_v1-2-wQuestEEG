using UnityEngine;
using UnityEditor;

namespace Passer.Humanoid {

    [CustomEditor(typeof(Socket), true)]
    public class Socket_Editor : Editor {

        protected Socket socket;

        #region Enable
        public void OnEnable() {
            socket = (Socket)target;

            InitEvents();
        }
        #endregion

        #region Inspector

        public override void OnInspectorGUI() {
            serializedObject.Update();

            //socket.attachedTransform = (Transform)EditorGUILayout.ObjectField("Attached Transform", socket.attachedTransform, typeof(Transform), true);
            AttachedObjectInspector();
            SocketTagInspector();

            EventsInspector();

            serializedObject.ApplyModifiedProperties();
        }

        protected void SocketTagInspector() {
            SerializedProperty socketTagProp = serializedObject.FindProperty("socketTag");
            //socket.socketTag = EditorGUILayout.TextField("Socket Tag", socket.socketTag);
            string socketTag = socketTagProp.stringValue;

            string[] options = UnityEditorInternal.InternalEditorUtility.tags;
            int selected = 0;
            for (int i = 0; i < options.Length; i++) {
                if (options[i] == socketTag)
                    selected = i;
            }

            //selected = EditorGUILayout.MaskField("Socket Tags", selected, options);
            selected = EditorGUILayout.Popup("Socket Tag", selected, options);
            socketTagProp.stringValue = options[selected];
        }

        protected void AttachedObjectInspector() {
            SerializedProperty attachedTransformProp = serializedObject.FindProperty("attachedTransform");
            if (Application.isPlaying) {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Attached Transform", attachedTransformProp.objectReferenceValue, typeof(Transform), false);
                EditorGUI.EndDisabledGroup();
            }
            else {
                SerializedProperty attachedPrefabProp = serializedObject.FindProperty("attachedPrefab");
                if (attachedPrefabProp.objectReferenceValue != null && attachedTransformProp.objectReferenceValue == null)
                    attachedPrefabProp.objectReferenceValue = null;

                GameObject attachedPrefab = (GameObject)EditorGUILayout.ObjectField("Attached Prefab", attachedPrefabProp.objectReferenceValue, typeof(GameObject), false);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Attached Transform", attachedTransformProp.objectReferenceValue, typeof(Transform), false);
                EditorGUI.EndDisabledGroup();

                if (attachedPrefab != attachedPrefabProp.objectReferenceValue) {
                    if (attachedPrefab != null)
                        AttachPrefab(socket, attachedTransformProp, attachedPrefab);
                    else {
                        ReleaseObject(socket, attachedTransformProp);
                    }
                }
                attachedPrefabProp.objectReferenceValue = attachedPrefab;
            }
        }

        protected virtual void AttachPrefab(Socket socket, SerializedProperty attachedTransformProp, GameObject prefab) {
            if (attachedTransformProp.objectReferenceValue != null)
                ReleaseObject(socket, attachedTransformProp);

            GameObject obj = Instantiate(prefab, socket.transform.position, socket.transform.rotation);
            obj.name = prefab.name; // Remove the (Clone)

            socket.Attach(obj.transform);
            if (socket.attachedTransform == null)
                Debug.LogWarning("Could not attach transform");
            else {
                attachedTransformProp.objectReferenceValue = obj;
                //Handle handle = socket.attachedHandle;
                //if (handle == null)
                //    Handle.Create(obj, socket);                
            }
        }

        protected virtual void ReleaseObject(Socket socket, SerializedProperty attachedTransformProp) {
            Transform attachedTransform = (Transform)attachedTransformProp.objectReferenceValue;
            socket.Release();
            DestroyImmediate(attachedTransform.gameObject, true);
            attachedTransformProp.objectReferenceValue = null;
        }

        #endregion

        #region Events
        protected SerializedProperty attachEventProp;

        protected void InitEvents() {
            attachEventProp = serializedObject.FindProperty("attachEvent");
            socket.attachEvent.id = 0;
        }

        protected bool showEvents;
        protected int selectedEventSource = -1;
        protected int selectedEvent;

        protected void EventsInspector() {
            showEvents = EditorGUILayout.Foldout(showEvents, "Events", true);
            if (showEvents) {
                EditorGUI.indentLevel++;

                GameObjectEvent_Editor.EventInspector(attachEventProp, socket.attachEvent, ref selectedEventSource, ref selectedEvent);

                EditorGUI.indentLevel--;
            }
        }


        #endregion
    }

}