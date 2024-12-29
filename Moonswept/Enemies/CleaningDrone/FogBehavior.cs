using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Moonswept.Enemies.CleaningDrone;

public class FogBehavior : MonoBehaviour {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public SphereCollider collider;
    public float destroyAfter;
    public ParticleSystem particleSystem;
    public LocalVolumetricFog localFog;
    private float _stopwatch;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public void FixedUpdate() {
        var localPlayer = GameNetworkManager.Instance.localPlayerController;

        if (!localPlayer || localPlayer.isPlayerDead || !localPlayer.isPlayerControlled) return;

        if (collider.bounds.Contains(localPlayer.playerEye.position)) {
            localPlayer.increasingDrunknessThisFrame = true;
            localPlayer.drunknessInertia = Mathf.Clamp(localPlayer.drunknessInertia + Time.fixedDeltaTime / 2F * localPlayer.drunknessSpeed, 0.1F, 4.5F);
        }

        _stopwatch += Time.fixedDeltaTime;
        if (_stopwatch >= destroyAfter - 1.5F) {
            particleSystem.Stop();
            Destroy(localFog);
        }

        if (_stopwatch >= destroyAfter) Destroy(gameObject);
    }
}