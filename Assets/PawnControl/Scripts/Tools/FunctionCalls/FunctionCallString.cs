using System;
using System.Reflection;
using UnityEngine;

namespace Passer {

	public partial class FunctionCall {
		protected delegate void MethodString(string value);

        public static void Execute(GameObject target, string methodName, string stringValue) {
            UnityEngine.Object component = GetComponent(target, methodName);
            MethodString method = CreateMethodString(component, methodName);
            if (method != null)
                method(stringValue);
            else
                Debug.LogWarning("Could not find Method " + methodName + " for " + component);

        }

        public void Execute(string value, bool networkSync = false) {
            if (targetDelegate == null) {
                GetTargetMethod();
                if (targetDelegate == null)
                    return;
            }
            ((MethodString)targetDelegate)(value);
//#if hNW_UNET
//            if (networkSync) {
//                if (networkObject == null)
//                    networkObject = NetworkObject.GetNetworkObject(this);
//                if (networkObject != null)
//                    networkObject.RPC(this, value);
//            }
//#endif
        }

        protected static MethodString CreateMethodString(UnityEngine.Object target, string methodName) {
            if (target is Script) {
                //Script script = (Script)target;
                // Scripts do not yet support parameters
                //MethodBool targetMethod = (bool x) => script.Execute(x);
                //return targetMethod;
                return null;
            }
            else {
                Type targetComponentType = target.GetType();
                int i = methodName.LastIndexOf('/');
                if (i >= 0)
                    methodName = methodName.Substring(i + 1);                
                MethodInfo methodInfo = targetComponentType.GetMethod(methodName, new Type[] { typeof(string) });
                if (methodInfo == null)
                    return null;

                MethodString targetMethod = (MethodString)Delegate.CreateDelegate(typeof(MethodString), target, methodInfo);
                return targetMethod;
            }
        }

        public void CreateTargetMethodString(UnityEngine.Object target, string methodName, bool fromEvent, string stringConstant) {
            Type targetType = target.GetType();
            MethodInfo methodInfo = targetType.GetMethod(methodName, new Type[] { typeof(string) });
            if (methodInfo == null)
                return;

            MethodString stringDelegate = (MethodString)Delegate.CreateDelegate(typeof(MethodString), target, methodInfo);
            if (fromEvent)
                targetDelegate = stringDelegate;
            else
                targetDelegate = (MethodString)(_ => stringDelegate(stringConstant));            
        }

    }
}