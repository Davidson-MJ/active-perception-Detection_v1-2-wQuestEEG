using System;
using System.Reflection;
using UnityEngine;

namespace Passer {

    /// <summary>
    /// A function which can be called
    /// </summary>
    [Serializable]
    public partial class FunctionCall {
        /// <summary>
        /// The target GameObject on which the function should be called.
        /// </summary>
        public GameObject targetGameObject;
        /// <summary>
        /// The name of the method to call on the GameObject
        /// </summary>
        /// The format of this string is &lt;fully qualified component type&gt;.&lt;function name&gt;
        public string methodName;

        protected INetworkObject networkObject;

        protected Delegate targetDelegate;

        protected delegate void Method();
        protected delegate void MethodBool(bool value);
        protected delegate void MethodFloat(float value);
        protected delegate void MethodInt(int value);
        protected delegate void MethodVector3(Vector3 value);
        protected delegate void MethodGameObject(GameObject value);
        protected delegate void MethodRigidbody(Rigidbody value);

        // For animation parameter calls
        protected delegate void MethodStringBool(string s, bool value);
        protected delegate void MethodStringFloat(string s, float value);
        protected delegate void MethodStringInt(string s, int value);

        public enum ParameterType {
            Void,
            Float,
            Int,
            Bool,
            Vector3,
            GameObject,
            Rigidbody,
            String,
        }
        public static Type ToSystemType(ParameterType parameterType) {
            switch (parameterType) {
                default:
                case ParameterType.Void:
                    return typeof(void);
                case ParameterType.Bool:
                    return typeof(bool);
                case ParameterType.Int:
                    return typeof(int);
                case ParameterType.Float:
                    return typeof(float);
                case ParameterType.Vector3:
                    return typeof(Vector3);
                case ParameterType.GameObject:
                    return typeof(GameObject);
                case ParameterType.Rigidbody:
                    return typeof(Rigidbody);
                case ParameterType.String:
                    return typeof(string);
            }
        }

        /// <summary>
        /// Function Parameter
        /// </summary>
        [Serializable]
        public class Parameter {
            /// <summary>
            /// The parameter value comes from the event
            /// </summary>
            /// When false one of the constant values is used, based on the type of the parameter
            public bool fromEvent = false;
            /// <summary>
            /// For future use...
            /// </summary>
            public string localProperty;
            /// <summary>
            /// The type of the parameter
            /// </summary>
            /// May be converted to a System.Type later...
            public ParameterType type;
            /// <summary>
            /// The constant float value when the parameter type is float and fromEvent is false.
            /// </summary>
            public float floatConstant;
            /// <summary>
            /// The constant integer value when the parameter type is int and fromEvent is false.
            /// </summary>
            public int intConstant;
            /// <summary>
            /// The constant boolean value when the parameter type is bool and fromEvent is false.
            /// </summary>
            public bool boolConstant;
            /// <summary>
            /// The constant string value when the parameter type is string and fromEvent is false.
            /// </summary>
            public string stringConstant;
            /// <summary>
            /// The constant Vector3 value when the parameter type is Vector3 and fromEvent is false.
            /// </summary>
            public Vector3 vector3Constant;
            /// <summary>
            /// The constant GameObject value when the parameter type is GameObject and fromEvent is false.
            /// </summary>
            public GameObject gameObjectConstant;
            /// <summary>
            /// The constant Rigidbody value when the parameter type is Rigidbody and fromEvent is false.
            /// </summary>
            public Rigidbody rigidbodyConstant;

        }
        /// <summary>
        /// For future use...
        /// </summary>
        public Parameter[] parameters;

        /// <summary>
        /// Adds a new Parameter to the function call
        /// </summary>
        /// <returns>The new Parameter</returns>
        public Parameter AddParameter() {
            if (parameters == null || parameters.Length == 0)
                parameters = new Parameter[1];

            parameters[parameters.Length - 1] = new Parameter();
            return parameters[parameters.Length - 1];
        }

        public static void Execute(GameObject target, string methodName) {
            UnityEngine.Object component = GetComponent(target, methodName);
            Method method = CreateMethod(component, methodName);
            method();
        }

        /// <summary>
        /// For future use...
        /// </summary>
        public enum Networking {
            No,
            Yes
        }

        /// <summary>
        /// Execute the void function call
        /// </summary>
        /// <param name="networking">For future use...</param>
        public virtual void Execute(Networking networking = Networking.No) {
            if (targetDelegate == null) {
                GetTargetMethod();
                if (targetDelegate == null)
                    return;
            }

            if (targetDelegate is Method) {
                ((Method)targetDelegate)();
                ExecuteRemote();
            }
            else if (targetDelegate is MethodBool) {
                ((MethodBool)targetDelegate)(parameters[0].boolConstant);
                ExecuteRemote(parameters[0].boolConstant);
            }
            else if (targetDelegate is MethodInt)
                ((MethodInt)targetDelegate)(parameters[0].intConstant);
            else if (targetDelegate is MethodFloat)
                ((MethodFloat)targetDelegate)(parameters[0].floatConstant);
            else if (targetDelegate is MethodGameObject) {
                ((MethodGameObject)targetDelegate)(parameters[0].gameObjectConstant);
            }
            else if (targetDelegate is MethodVector3)
                ((MethodVector3)targetDelegate)(parameters[0].vector3Constant);
            else if (targetDelegate is MethodRigidbody)
                ((MethodRigidbody)targetDelegate)(parameters[0].rigidbodyConstant);
        }

        private void ExecuteRemote() {
            if (NetworkObject.connected == false)
                return;

            if (networkObject == null)
                networkObject = NetworkObject.GetINetworkObject(this);
            if (networkObject != null)
                networkObject.RPC(this);
        }

        private void ExecuteRemote(bool value) {
            if (NetworkObject.connected == false)
                return;

            if (networkObject == null)
                networkObject = NetworkObject.GetINetworkObject(this);
            if (networkObject != null)
                networkObject.RPC(this, value);
        }

        public static void Execute(GameObject target, string methodName, bool boolValue) {
            UnityEngine.Object component = GetComponent(target, methodName);
            MethodBool method = CreateMethodBool(component, methodName);
            method(boolValue);
        }

        /// <summary>
        /// Execute a function call with a boolean parameter
        /// </summary>
        /// <param name="value">The boolean value to pass to the function</param>
        /// <param name="networking">For future use...</param>
        public void Execute(bool value, Networking networking = Networking.No) {
            if (targetDelegate == null) {
                GetTargetMethod();
                if (targetDelegate == null)
                    return;
            }
            ((MethodBool)targetDelegate)(value);

            ExecuteRemote(value);
        }

        /// <summary>
        /// Call the function with an integer parameter
        /// </summary>
        /// <param name="value">The integer value to pass to the function</param>
        public void Execute(int value) {
            if (targetDelegate == null) {
                GetTargetMethod();
                if (targetDelegate == null)
                    return;
            }
            ((MethodInt)targetDelegate)(value);

        }

        /// <summary>
        /// Call the function with a float parameter
        /// </summary>
        /// <param name="value">The float value to pass to the function</param>
        public virtual void Execute(float value) {
            if (targetDelegate == null) {
                GetTargetMethod();
                if (targetDelegate == null)
                    return;
            }
            ((MethodFloat)targetDelegate)(value);
        }

        /// <summary>
        /// Call the function with a Vector3 parameter
        /// </summary>
        /// <param name="value">The Vector3 value to pass to the function</param>
        public void Execute(Vector3 value) {
            if (targetDelegate == null) {
                GetTargetMethod();
                if (targetDelegate == null)
                    return;
            }
            ((MethodVector3)targetDelegate)(value);
        }

        /// <summary>
        /// Call the funtion with a GameObject parameter
        /// </summary>
        /// <param name="value">The GameObject value to pass to the function</param>
        public void Execute(GameObject value) {
            if (targetDelegate == null) {
                GetTargetMethod();
                if (targetDelegate == null)
                    return;
            }
            ((MethodGameObject)targetDelegate)(value);
        }

        /// <summary>
        /// Call the function with a Rigidbody parameter
        /// </summary>
        /// <param name="value">The Rigidbody value to pass to the function </param>
        protected void Execute(Rigidbody value) {
            if (targetDelegate == null) {
                GetTargetMethod();
                if (targetDelegate == null)
                    return;
            }
            ((MethodRigidbody)targetDelegate)(value);
        }

        /// <summary>
        /// Call the function with a string and a boolean parameter
        /// </summary>
        /// <param name="s">The string value to pass to the function as the first parameter</param>
        /// <param name="value">The boolean value to pass to the function as the second parameter</param>
        public void ExecuteString(string s, bool value) {
            if (targetDelegate == null) {
                GetTargetMethod();
                if (targetDelegate == null)
                    return;
            }
            ((MethodStringBool)targetDelegate)(s, value);
        }
        /// <summary>
        /// Call the function with a string and a float parameter
        /// </summary>
        /// <param name="s">The string value to pass to the function as the first parameter</param>
        /// <param name="value">The float value to pass to the function as the second parameter</param>
        public void ExecuteString(string s, float value) {
            if (targetDelegate == null) {
                GetTargetMethod();
                if (targetDelegate == null)
                    return;
            }
            ((MethodStringFloat)targetDelegate)(s, value);
        }
        /// <summary>
        /// Call the function with a string and a integer parameter
        /// </summary>
        /// <param name="s">The string value to pass to the function as the first parameter</param>
        /// <param name="value">The integer value to pass to the function as the second parameter</param>
        public void ExecuteString(string s, int value) {
            if (targetDelegate == null) {
                GetTargetMethod();
                if (targetDelegate == null)
                    return;
            }
            ((MethodStringInt)targetDelegate)(s, value);
        }

        protected virtual void GetTargetMethod() {
            if (targetGameObject == null)
                return;

            string localMethodName;
            if (methodName != null && methodName.Length > 21 &&
                methodName.Substring(0, 21).Equals("SetAnimatorParameter/")) {
                CreateAnimationParameterMethod(targetGameObject, methodName);
                if (targetDelegate != null)
                    return;
            }

            UnityEngine.Object targetComponent = GetComponent(targetGameObject, methodName, out localMethodName);
            if (targetComponent == null)
                return;

            if (targetComponent is Script) {
                Script script = (Script)targetComponent;
                targetDelegate = (Method)(() => script.Execute());
                return;
            }

            if (parameters == null || parameters.Length == 0) {
                targetDelegate = CreateMethod(targetGameObject, methodName);
                return;
            }

            switch (parameters[0].type) {
                case ParameterType.Void:
                    targetDelegate = CreateMethod(targetGameObject, methodName);
                    break;
                case ParameterType.Bool:
                    CreateTargetMethod(targetComponent, localMethodName, parameters[0].fromEvent, parameters[0].boolConstant);
                    break;
                case ParameterType.Int:
                    CreateTargetMethod(targetComponent, localMethodName, parameters[0].fromEvent, parameters[0].intConstant);
                    break;
                case ParameterType.Float:
                    CreateTargetMethod(targetComponent, localMethodName, parameters[0].fromEvent, parameters[0].floatConstant);
                    break;
                case ParameterType.String:
                    CreateTargetMethodString(targetComponent, localMethodName, parameters[0].fromEvent, parameters[0].stringConstant);
                    break;
                case ParameterType.Vector3:
                    CreateTargetMethod(targetComponent, localMethodName, parameters[0].fromEvent, parameters[0].vector3Constant);
                    break;
                case ParameterType.GameObject:
                    CreateTargetMethod(targetComponent, localMethodName, parameters[0].fromEvent, parameters[0].gameObjectConstant);
                    break;
                case ParameterType.Rigidbody:
                    CreateTargetMethod(targetComponent, localMethodName, parameters[0].fromEvent, parameters[0].rigidbodyConstant);
                    break;
                default:
                    return;

            }
        }

        protected static UnityEngine.Object GetComponent(GameObject target, string fullMethodName) {
            string methodName;
            return GetComponent(target, fullMethodName, out methodName);
        }

        protected static UnityEngine.Object GetComponent(GameObject target, string fullMethodName, out string methodName) {
            methodName = fullMethodName;
            string componentName = "";
            int slashPos = methodName.LastIndexOf("/");
            if (slashPos >= 0) {
                methodName = methodName.Substring(slashPos + 1);
                componentName = fullMethodName.Substring(0, slashPos);
            }
            if (componentName == "")
                return null;
            if (componentName == "UnityEngine.GameObject")
                return target;

            if (componentName.StartsWith("UnityEngine."))
                componentName = componentName.Substring(12);

            return target.GetComponent(componentName);
        }

        /// <summary>
        /// Gets the GameObject for the Object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static GameObject GetGameObject(UnityEngine.Object obj) {
            if (obj is GameObject)
                return (GameObject)obj;
            else if (obj is Component)
                return ((Component)obj).gameObject;
            else
                return null;
        }

        protected void CreateAnimationParameterMethod(GameObject target, string fullMethodName) {
#if pHUMANOID
            //string parameterName = fullMethodName.Substring(21);
            Humanoid.HumanoidControl humanoid = target.GetComponent<Humanoid.HumanoidControl>();
            if (humanoid == null)
                return;

            Type targetType = typeof(Humanoid.HumanoidControl);
            MethodInfo methodInfo;

            switch (parameters[0].type) {
                case ParameterType.Bool:
                    methodInfo = targetType.GetMethod("SetAnimationParameterBool", new Type[] { typeof(string), typeof(bool) });
                    if (methodInfo != null)
                        targetDelegate = (MethodStringBool)Delegate.CreateDelegate(typeof(MethodStringBool), humanoid, methodInfo);
                    break;
                case ParameterType.Float:
                    methodInfo = targetType.GetMethod("SetAnimationParameterFloat", new Type[] { typeof(string), typeof(float) });
                    if (methodInfo != null)
                        targetDelegate = (MethodStringFloat)Delegate.CreateDelegate(typeof(MethodStringFloat), humanoid, methodInfo);
                    break;
                case ParameterType.Int:
                    methodInfo = targetType.GetMethod("SetAnimationParameterInt", new Type[] { typeof(string), typeof(int) });
                    if (methodInfo != null)
                        targetDelegate = (MethodStringInt)Delegate.CreateDelegate(typeof(MethodStringInt), humanoid, methodInfo);
                    break;
                case ParameterType.Void:
                    methodInfo = targetType.GetMethod("SetAnimationParameterTrigger", new Type[] { typeof(string) });
                    if (methodInfo != null)
                        targetDelegate = (MethodString)Delegate.CreateDelegate(typeof(MethodString), humanoid, methodInfo);
                    break;

            }
#endif
        }

        protected static Method CreateMethod(GameObject target, string fullMethodName) {
            string methodName;
            UnityEngine.Object component = GetComponent(target, fullMethodName, out methodName);
            if (component == null)
                return null;

            Method method = CreateMethod(component, methodName);
            return method;
        }

        protected static Method CreateMethod(UnityEngine.Object target, string methodName) {
            if (target is Script) {
                Script script = (Script)target;
                Method targetMethod = () => script.Execute();
                return targetMethod;
            }
            else {
                Type targetComponentType = target.GetType();
                MethodInfo methodInfo = targetComponentType.GetMethod(methodName, new Type[] { });
                if (methodInfo == null)
                    return null;

                Method targetMethod = (Method)Delegate.CreateDelegate(typeof(Method), target, methodInfo);
                return targetMethod;
            }
        }

        protected static MethodBool CreateMethod(GameObject target, string fullMethodName, bool boolConstant) {
            string methodName;
            UnityEngine.Object component = GetComponent(target, fullMethodName, out methodName);
            MethodBool method = CreateMethodBool(component, methodName);
            return method;
        }

        protected static MethodBool CreateMethodBool(UnityEngine.Object target, string methodName) {
            if (target is Script) {
                // Scripts do not yet support parameters
                return null;
            }
            else {
                Type targetComponentType = target.GetType();
                MethodInfo methodInfo = targetComponentType.GetMethod(methodName, new Type[] { });
                if (methodInfo == null)
                    return null;

                MethodBool targetMethod = (MethodBool)Delegate.CreateDelegate(typeof(MethodBool), target, methodInfo);
                return targetMethod;
            }
        }

        protected void CreateTargetMethod(UnityEngine.Object target, string methodName, bool fromEvent, bool boolConstant) {
            Type targetType = target.GetType();
            MethodInfo methodInfo = targetType.GetMethod(methodName, new Type[] { typeof(bool) });
            if (methodInfo == null)
                return;

            MethodBool intDelegate = (MethodBool)Delegate.CreateDelegate(typeof(MethodBool), target, methodInfo);
            if (fromEvent)
                targetDelegate = intDelegate;
            else
                targetDelegate = (MethodBool)(_ => intDelegate(boolConstant));
        }

        protected void CreateTargetMethod(UnityEngine.Object target, string methodName, bool fromEvent, int intConstant) {
            Type targetType = target.GetType();
            MethodInfo methodInfo = targetType.GetMethod(methodName, new Type[] { typeof(int) });
            if (methodInfo == null)
                return;

            MethodInt intDelegate = (MethodInt)Delegate.CreateDelegate(typeof(MethodInt), target, methodInfo);
            if (fromEvent)
                targetDelegate = intDelegate;
            else
                targetDelegate = (MethodInt)(_ => intDelegate(intConstant));
        }

        protected void CreateTargetMethod(UnityEngine.Object target, string methodName, bool fromEvent, float floatConstant) {
            Type targetType = target.GetType();
            MethodInfo methodInfo = targetType.GetMethod(methodName, new Type[] { typeof(float) });
            if (methodInfo == null)
                return;

            MethodFloat floatDelegate = (MethodFloat)Delegate.CreateDelegate(typeof(MethodFloat), target, methodInfo);
            if (fromEvent)
                targetDelegate = floatDelegate;
            else
                targetDelegate = (MethodFloat)(_ => floatDelegate(floatConstant));
        }

        protected void CreateTargetMethod(UnityEngine.Object target, string methodName, bool fromEvent, Vector3 vectorConstant) {
            Type targetType = target.GetType();
            MethodInfo methodInfo = targetType.GetMethod(methodName, new Type[] { typeof(Vector3) });
            if (methodInfo == null)
                return;

            MethodVector3 vector3Delegate = (MethodVector3)Delegate.CreateDelegate(typeof(MethodVector3), target, methodInfo);
            if (fromEvent)
                targetDelegate = vector3Delegate;
            else
                targetDelegate = (MethodVector3)(_ => vector3Delegate(vectorConstant));
        }

        // This is the example for the other target method creation.
        // When it has a constant parameter, a void method is created and used which is the most efficient
        protected void CreateTargetMethod(UnityEngine.Object target, string methodName, bool fromEvent, GameObject gameObjectConstant) {
            Type targetType = target.GetType();
            MethodInfo methodInfo = targetType.GetMethod(methodName, new Type[] { typeof(GameObject) });
            if (methodInfo == null)
                return;

            MethodGameObject gameObjectDelegate = (MethodGameObject)Delegate.CreateDelegate(typeof(MethodGameObject), target, methodInfo);
            if (fromEvent)
                targetDelegate = gameObjectDelegate;
            else
                targetDelegate = (MethodGameObject)(_ => gameObjectDelegate(gameObjectConstant));
        }

        protected void CreateTargetMethod(UnityEngine.Object target, string methodName, bool fromEvent, Rigidbody rigidbodyConstant) {
            Type targetType = target.GetType();
            MethodInfo methodInfo = targetType.GetMethod(methodName, new Type[] { typeof(Rigidbody) });
            if (methodInfo == null)
                return;

            MethodRigidbody rigidbodyDelegate = (MethodRigidbody)Delegate.CreateDelegate(typeof(MethodRigidbody), target, methodInfo);
            if (fromEvent)
                targetDelegate = rigidbodyDelegate;
            else
                targetDelegate = (MethodRigidbody)(_ => rigidbodyDelegate(rigidbodyConstant));
        }

    }

}