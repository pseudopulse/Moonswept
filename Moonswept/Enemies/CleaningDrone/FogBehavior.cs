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

    // Reset state when the pooled object is taken from the pool
    public void ResetForReuse() {
        _stopwatch = 0f;

        particleSystem.Clear(true);
        particleSystem.Play(true);

        localFog.enabled = true;
        collider.enabled = true;
        gameObject.SetActive(true);
    }

    // Called when the pooled object is released back to the pool
    public void OnRelease() {
        if (particleSystem.isPlaying) particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        localFog.enabled = false;
        collider.enabled = false;
    }

    public void FixedUpdate() {
        ApplyFogEffects();

        _stopwatch += Time.fixedDeltaTime;
        if (_stopwatch >= destroyAfter - 1.5F) {
            if (particleSystem.isPlaying) particleSystem.Stop();
            localFog.enabled = false;
        }

        if (_stopwatch < destroyAfter) return;
        // return to pool for reuse; pool's actionOnRelease will deactivate & OnRelease will be called
        CleaningDroneAI.FogPool?.Release(gameObject);
    }

    private void ApplyFogEffects() {
        foreach (var playerScript in StartOfRound.Instance.allPlayerScripts) {
            if (!playerScript || playerScript.isPlayerDead || !playerScript.isPlayerControlled) continue;
            if (!collider.bounds.Contains(playerScript.playerEye.position)) continue;
            if (playerScript.drunknessInertia >= 4.5F) continue;

            var drunkness = playerScript.drunknessInertia + Time.fixedDeltaTime / 2F * playerScript.drunknessSpeed;

            playerScript.drunknessInertia = Mathf.Clamp(drunkness, 0.1F, 4.5F);
            playerScript.increasingDrunknessThisFrame = true;
        }
    }
}