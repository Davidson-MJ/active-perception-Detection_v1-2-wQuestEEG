using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Passer {

    public class GameObjectEvent_Editor : Event_Editor {

        #region EventList

        public static void EventInspector(
            SerializedProperty eventSourceProp, GameObjectEventHandlers eventSource,
            ref int selectedEventSourceIx, ref int selectedEventIx) {

            EventInspector(eventSourceProp, ref selectedEventSourceIx, ref selectedEventIx,
                GameObjectMethodCheck, InitGameObjectEvent);
        }

        protected static void InitGameObjectEvent(SerializedProperty eventProp) {
        }

        protected static bool GameObjectMethodCheck(MethodInfo method, out string label) {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0 && method.ReturnType == typeof(void)) {
                label = method.Name + " ()";
                return true;
            }
            else if (parameters.Length == 1 && (
                parameters[0].ParameterType == typeof(GameObject) ||
                parameters[0].ParameterType == typeof(float) ||
                parameters[0].ParameterType == typeof(int) ||
                parameters[0].ParameterType == typeof(bool) ||
                parameters[0].ParameterType == typeof(GameObject) ||
                parameters[0].ParameterType == typeof(Rigidbody)
                )) {

                label = method.Name + " (" + parameters[0].ParameterType.Name + ")";
                return true;
            }

            label = "";
            return false;
        }

        public static void Cleanup(GameObjectEventHandlers eventList) {
            foreach (GameObjectEvent goEvent in eventList.events)
                Cleanup(goEvent);

            eventList.events.RemoveAll(goEvent => goEvent.eventType == EventHandler.Type.Never);
        }

        public static void Cleanup(GameObjectEvent goEvent) {
        }

        public static void Cleanup(UnityEventBase eventBase) {
            int nCalls = eventBase.GetPersistentEventCount();
            for (int i = 0; i < nCalls; i++) {
                if (eventBase.GetPersistentTarget(i) == null) {
                    UnityEventTools.RemovePersistentListener(eventBase, i);
                    return;
                }
            }
        }

        #endregion

    }

}