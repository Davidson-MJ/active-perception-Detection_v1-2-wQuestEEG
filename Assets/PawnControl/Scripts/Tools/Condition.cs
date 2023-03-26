using System.Reflection;
using UnityEngine;

namespace Passer {

    [System.Serializable]
    public class Condition {

        public GameObject targetGameObject;
        public string fullPropertyName;
        protected PropertyInfo propertyInfo;

        public enum PropertyType {
            Unsupported,
            Bool,
            Int,
            Float,
            Object,
        };
        public static PropertyType GetFromType(System.Type systemType) {
            if (systemType == typeof(bool))
                return PropertyType.Bool;
            else if (systemType == typeof(int))
                return PropertyType.Int;
            else if (systemType == typeof(float))
                return PropertyType.Float;
            else if (systemType == typeof(Object))
                return PropertyType.Object;
            else
                return PropertyType.Unsupported;
        }
        public PropertyType propertyType;

        public int operandIndex;

        public int intConstant;
        public float floatConstant;

        public static readonly string[] boolOperands = new string[] {
            "== false",
            "== true"
        };
        public static readonly string[] intOperands = new string[] {
            "==",
            "!=",
            ">",
            ">=",
            "<",
            "<="
        };
        public static readonly string[] floatOperands = new string[] {
            ">",
            "<"
        };
        public static readonly string[] objectOperands = new string[] {
            "== null",
            "!= null"
        };

        public bool Check() {
            if (targetGameObject == null)
                return true;

            Component targetComponent = GetComponent(targetGameObject, fullPropertyName);

            if (propertyInfo == null) {
                GetTargetProperty();
                if (propertyInfo == null)
                    return true;
            }

            object propertyValue = propertyInfo.GetValue(targetComponent, null);
            switch (propertyType) {
                case PropertyType.Bool:
                    return CheckBool(propertyValue);
                case PropertyType.Int:
                    return CheckInt(propertyValue);
                case PropertyType.Float:
                    return CheckFloat(propertyValue);
                case PropertyType.Object:
                    return CheckObject(propertyValue);
            }
            return true;
        }

        protected virtual void GetTargetProperty() {
            string propertyName = fullPropertyName;
            string componentName = "";
            int slashPos = propertyName.LastIndexOf("/");
            if (slashPos >= 0) {
                propertyName = propertyName.Substring(slashPos + 1);
                componentName = fullPropertyName.Substring(0, slashPos);
            }
            if (componentName == "")
                return;

            //Component targetComponent = GetComponent(targetGameObject, fullPropertyName);
            Component targetComponent = targetGameObject.GetComponent(componentName);
            System.Type targetType = targetComponent.GetType();
            propertyInfo = targetType.GetProperty(propertyName);
        }

        protected Component GetComponent(GameObject targetGameObject, string propertyName) {
            string localPropertyName = propertyName;
            string componentName = "";
            int slashPos = localPropertyName.LastIndexOf("/");
            if (slashPos >= 0) {
                localPropertyName = localPropertyName.Substring(slashPos + 1);
                componentName = propertyName.Substring(0, slashPos);
            }
            if (componentName == "")
                return null;

            Component targetComponent = targetGameObject.GetComponent(componentName);
            return targetComponent;
        }

        public bool CheckBool(object propertyValue) {
            if (!(propertyValue is bool))
                return false;

            if (operandIndex == 1) // == true
                return (bool)propertyValue == true;
            else
                return (bool)propertyValue == false;
        }

        public bool CheckInt(object propertyValue) {
            if (!(propertyValue is int))
                return false;
            int value = (int)propertyValue;

            switch (operandIndex) {
                case 0: // ==
                    return value == intConstant;
                case 1: // !=
                    return value != intConstant;
                case 2: // >
                    return value > intConstant;
                case 3: // >=
                    return value >= intConstant;
                case 4: // <
                    return value < intConstant;
                case 5: // <=
                    return value <= intConstant;
                }
            return false;
        }

        public bool CheckFloat(object propertyValue) {
            if (!(propertyValue is float))
                return false;

            float value = (float)propertyValue;
            switch (operandIndex) {
                case 0: // >
                    return value > floatConstant;
                case 1: // <
                    return value < floatConstant;
            }
            return false;
        }

        public bool CheckObject(object propertyValue) {
            if (!(propertyValue is Object))
                return false;

            Object value = (Object)propertyValue;
            switch(operandIndex) {
                case 0: // == null
                    return value == null;
                case 1: // != null
                    return value != null;
            }
            return false;
        }
    }

}