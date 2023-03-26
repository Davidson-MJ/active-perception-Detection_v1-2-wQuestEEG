namespace Passer.Humanoid {

    public class NetworkingSpawner : NetworkingStarter {
        public HumanoidSpawner humanoidSpawner;

        public void OnNetworkingStarted() {
            humanoidSpawner.SpawnHumanoid();
        }
    }
}