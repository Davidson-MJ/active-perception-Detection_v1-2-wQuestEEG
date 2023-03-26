using UnityEngine;
using UnityEditor;

namespace Passer {

    [CustomEditor(typeof(RigidbodyDisabled), true)]
    public class RigidbodyDisabled_Editor : Editor {

        #region Scene

        public void OnSceneGUI() {
            if (Application.isPlaying)
                return;

            RigidbodyDisabled rbDisabled = (RigidbodyDisabled)target;
            Handle[] handles = rbDisabled.transform.GetComponentsInChildren<Handle>();

            foreach(Handle handle in handles) {
                if (handle.socket == null)
                    continue;

                UpdateHandleTransform(handle);
            }

        }

        /// Move the handle transform to the socket it is attached to
        protected void UpdateHandleTransform(Handle handle) {
            if (handle.socket == null)
                return;

            Transform handleTransform = handle.transform;
            handleTransform.position = handle.socket.transform.position;
            handleTransform.rotation = handle.socket.transform.rotation;
        }

        #endregion
    }
}