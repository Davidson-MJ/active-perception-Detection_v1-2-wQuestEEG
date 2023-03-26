using System.Collections.Generic;
using UnityEngine;

namespace Passer {

    public class Script : MonoBehaviour {
        public string scriptName;
        public List<Condition> conditions = new List<Condition>();
        public List<FunctionCall> functionCalls = new List<FunctionCall>();

        protected virtual void Reset() {
            this.enabled = false;
        }

        protected virtual void Start() {
            Execute();
        }

        public void Execute() {
            foreach (Condition condition in conditions) {
                if (!condition.Check())
                    return;
            }
            foreach (FunctionCall functionCall in functionCalls) {
                functionCall.Execute();
            }
        }
    }

}