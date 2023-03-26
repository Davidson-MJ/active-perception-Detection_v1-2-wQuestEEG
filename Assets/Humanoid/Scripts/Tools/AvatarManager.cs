using UnityEngine;

namespace Passer.Humanoid {

    /// <summary>Manage avatar meshes for a humanoid</summary>
    /// The avatar manger can be used to manage multiple avatar meshes for a single humanoid.
    /// It is supported single player and networking setups.
    [HelpURLAttribute("https://passervr.com/documentation/humanoid-control/avatar-manager/")]
    public class AvatarManager : MonoBehaviour {

        /// <summary>The index of the current avatar in the list</summary>
        public int currentAvatarIndex = 0;
        /// <summary>The list of avatars for the humanoid</summary>
        /// For networked avatars this avatar will be used for the local client.
        public Animator[] fpAvatars = new Animator[0];
        /// <summary>The list of third person avatar for networked humanoids</summary>
        /// This is the avatar which will be used to show a player of remote clients.
        /// When no third person avatar is specified, the first person avatar will be used
        /// as the third person avatar.
        public Animator[] tpAvatars = new Animator[0];

        private HumanoidControl humanoid;

        protected virtual void Start() {
            humanoid = GetComponent<HumanoidControl>();
            SetAvatar(currentAvatarIndex);
        }

        /// <summary>Replaces the current avatar by the next avatar in the list.</summary>
        /// This will wrap around when the last avatar is the current avatar.
        public void NextAvatar() {
            currentAvatarIndex = mod(currentAvatarIndex + 1, fpAvatars.Length);
            SetAvatar(currentAvatarIndex);
        }

        /// <summary>Replaces the current avatar by the previous avatar in the list.</summary>
        /// This will wrap around when the first avatar is the current avatar.
        public void PreviousAvatar() {
            currentAvatarIndex = mod(currentAvatarIndex - 1, fpAvatars.Length);
            SetAvatar(currentAvatarIndex);
        }

        /// <summary>This will replace the current avatar with the avatar indicated by the avatarIndex.</summary>
        /// <param name="avatarIndex">The index of the avatar in the list of avatars</param>
        public void SetAvatar(int avatarIndex) {
            if (humanoid == null)
                return;
            if (avatarIndex < 0 || avatarIndex > fpAvatars.Length)
                return;

            if (fpAvatars[avatarIndex] != null) {
#if hNW_UNET || hNW_PHOTON
            if (avatarIndex < tpAvatars.Length && tpAvatars[avatarIndex] != null)
                humanoid.ChangeAvatar(fpAvatars[avatarIndex].gameObject, tpAvatars[avatarIndex].gameObject);
            else
#endif
                humanoid.ChangeAvatar(fpAvatars[avatarIndex].gameObject);
            }
        }

        public static int mod(int k, int n) {
            k %= n;
            return (k < 0) ? k + n : k;
        }
    }
}