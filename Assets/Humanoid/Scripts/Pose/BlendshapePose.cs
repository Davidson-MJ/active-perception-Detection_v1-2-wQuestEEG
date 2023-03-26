using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Passer.Humanoid {

    [System.Serializable]
    public class BlendshapePose {

        public SkinnedMeshRenderer renderer;
        public int blendshapeId;
        public float value;

        public virtual void ShowPose(HumanoidControl humanoid, float value) {
            if (renderer == null || blendshapeId >= renderer.sharedMesh.blendShapeCount || this.value == 0)
                return;

            renderer.SetBlendShapeWeight(blendshapeId, this.value * value * 100);
        }
    }

}