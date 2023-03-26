using UnityEditor;

namespace Passer {

    [CustomEditor(typeof(CollisionEventHandler))]
    public class CollisionEventHandler_Editor : Editor {
        protected CollisionEventHandler eventHandler;

        #region Enable
        protected virtual void OnEnable() {
            eventHandler = (CollisionEventHandler)target;

            InitEvents();
        }
        #endregion

        #region Disable
        protected virtual void OnDisable() {
            CleanupEvents();
        }
        #endregion

        #region Inspector
        public override void OnInspectorGUI() {
            serializedObject.Update();

            EventsInspector();

            serializedObject.ApplyModifiedProperties();
        }

        #region Events

        protected GameObjectEventHandlers[] eventLists;
        protected SerializedProperty[] eventListProps;
        protected SerializedProperty collisionEventProp;

        protected void InitEvents() {
            eventLists = new GameObjectEventHandlers[] {
                eventHandler.collisionHandlers,
            };

            collisionEventProp = serializedObject.FindProperty("collisionHandlers");
        }

        protected int selectedEvent = -1;
        protected int selectedSub = -1;

        protected void EventsInspector() {
            GameObjectEvent_Editor.EventInspector(collisionEventProp, eventHandler.collisionHandlers, ref selectedEvent, ref selectedSub);
        }

        protected void CleanupEvents() {
            foreach (GameObjectEventHandlers eventList in eventLists)
                GameObjectEvent_Editor.Cleanup(eventList);
        }

        #endregion

        #endregion
    }

}