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
        //TODO: Possibly improve this?
        foreach (var playerScript in StartOfRound.Instance.allPlayerScripts) {
            if (!playerScript || playerScript.isPlayerDead || !playerScript.isPlayerControlled) continue;

            if (!collider.bounds.Contains(playerScript.playerEye.position)) continue;

            playerScript.drunknessInertia = Mathf.Clamp(playerScript.drunknessInertia + Time.fixedDeltaTime / 2F * playerScript.drunknessSpeed, 0.1F, 4.5F);
            playerScript.increasingDrunknessThisFrame = true;
        }

        _stopwatch += Time.fixedDeltaTime;
        if (_stopwatch >= destroyAfter - 1.5F) {
            particleSystem.Stop();
            Destroy(localFog);
        }

        if (_stopwatch >= destroyAfter) Destroy(gameObject);
    }
}