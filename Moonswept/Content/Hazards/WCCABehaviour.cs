/*using System;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine.Events;

namespace Moonswept {
    public class WCCABehaviour : NetworkBehaviour {
        public GameObject EnkephalinBoxPrefab;
        public float PercentagePerBox = 0.2f;
        public float PercentageDontOpen = 0.2f;
        public AnimationCurve DontOpenChanceOverTime;
        public float LeftShellOpenRot;
        public float RightShellOpenRot;
        public Transform RightShell;
        public Transform LeftShell;
        public FireEventOnPlayerTrigger IteriorTrigger;

        //

        private StopwatchArray stopwatches = new();

        private PlayerControllerB currentStuckPlayer;
        private OpenState state = OpenState.Idle;
        private float targetDelay;
        private bool closed = false;
        private int damageTicksDealt = 0;

        public void FixedUpdate() {
            switch (state) {
                case OpenState.Opening:
                    stopwatches[state] += Time.fixedDeltaTime;
                    float coeff = Mathf.Lerp(0f, Mathf.Abs(LeftShellOpenRot), stopwatches[state] / targetDelay);
                    RightShell.transform.localRotation = Quaternion.Euler(0, 0, coeff);
                    LeftShell.transform.localRotation = Quaternion.Euler(0, 0, -coeff);

                    if (stopwatches[state] >= targetDelay) {
                        SetStateClientRpc((int)OpenState.Idle);
                    }
                    
                    break;
                case OpenState.Closing:
                    stopwatches[state] += Time.fixedDeltaTime;
                    float coeff2 = Mathf.Lerp(0f, Mathf.Abs(LeftShellOpenRot), 1f - (stopwatches[state] / targetDelay));
                    RightShell.transform.localRotation = Quaternion.Euler(0, 0, coeff2);
                    LeftShell.transform.localRotation = Quaternion.Euler(0, 0, -coeff2);

                    if (stopwatches[state] >= targetDelay) {
                        SetStateClientRpc((int)OpenState.Idle);
                    }
                    
                    break;
                case OpenState.BeginningOperation:
                    stopwatches[state] += Time.fixedDeltaTime;
                    float coeff3 = Mathf.Lerp(0f, Mathf.Abs(LeftShellOpenRot), 1f - (stopwatches[state] / targetDelay));
                    RightShell.transform.localRotation = Quaternion.Euler(0, 0, coeff3);
                    LeftShell.transform.localRotation = Quaternion.Euler(0, 0, -coeff3);

                    if (stopwatches[state] >= targetDelay) {
                        SetStateClientRpc((int)OpenState.Operational);
                    }
                    
                    break;
                case OpenState.Operational:
                    stopwatches[state] += Time.fixedDeltaTime;
                    if (stopwatches[state] >= targetDelay) {
                        stopwatches[state] = 0f;
                        damageTicksDealt++;
                        currentStuckPlayer.DamagePlayer(5, true, true, CauseOfDeath.Stabbing);
                    }

                    if (damageTicksDealt >= 5) {
                        damageTicksDealt = 0;
                        SetStateClientRpc((int)OpenState.Opening);
                    }

                    break;
                case OpenState.Idle:
                    // do nothing
                    break;
            }
        }

        public void SetStateOpen() {
            if (state != OpenState.Idle) return;
            Debug.Log("setting state to open");
            SetStateClientRpc((int)OpenState.Opening);
        }
        public void SetStateClosed() {
            if (state != OpenState.Idle) return;
            if (state == OpenState.BeginningOperation || state == OpenState.Operational) return;
            Debug.Log("setting state to closed");
            SetStateClientRpc((int)OpenState.Closing);
        }
        public void SetStateBeginOperation() {
            if (state == OpenState.Operational || state == OpenState.BeginningOperation) return;
            Debug.Log("setting state to beginning operation");
            currentStuckPlayer = IteriorTrigger.inside.GetComponent<PlayerControllerB>();
            SetStateClientRpc((int)OpenState.BeginningOperation);
        }

        public void ToggleState() {
            if (state != OpenState.Idle) {
                return;
            }

            OpenState nextState = closed ? OpenState.Opening : OpenState.Closing;

            SetStateClientRpc((int)nextState);
        }

        [ClientRpc]
        public void SetStateClientRpc(int nextState) {
            Debug.Log("setting state: " + nextState);

            state = (OpenState)nextState;
            
            stopwatches[state] = 0f;

            switch (state) {
                case OpenState.Operational:
                    targetDelay = 0.5f;
                    break;
                case OpenState.BeginningOperation:
                    targetDelay = 1f;
                    break;
                case OpenState.Opening:
                    targetDelay = 3f;
                    break;
                case OpenState.Closing:
                    targetDelay = 3f;
                    break;
                case OpenState.Idle:
                    targetDelay = 0f;
                    break;
            }
        }
    }

    public enum OpenState {
        Opening,
        Closing,
        Operational,
        BeginningOperation,
        Idle
    }

    public class FireEventOnPlayerTrigger : MonoBehaviour {
        private float stopwatch = 0f;
        public float ActivationDelay;
        internal Collider inside;
        public UnityEvent OnTriggerFill;
        public UnityEvent OnTriggerEmpty;
        private bool shouldCheck = false;

        public void OnTriggerEnter(Collider col) {
            if (!col.GetComponent<PlayerControllerB>()) return;

            Debug.Log("entered");

            if (!inside) {
                inside = col;
                Debug.Log("calling OnTriggerFill");
                OnTriggerFill.Invoke();
            }
        }

        public void OnTriggerStay(Collider col) {
            if (!col.GetComponent<PlayerControllerB>()) return;
            
            inside = col;
        }

        public void OnTriggerExit(Collider col) {
            if (!col.GetComponent<PlayerControllerB>()) return;
            
            Debug.Log("exited");

            inside = null;
            shouldCheck = true;
        }

        public void FixedUpdate() {
            if (shouldCheck && !inside) {
                stopwatch += Time.fixedDeltaTime;

                if (stopwatch >= ActivationDelay) {
                    stopwatch = 0f;
                    shouldCheck = false;
                   
                    Debug.Log("calling OnTriggerEmpty");
                    OnTriggerEmpty.Invoke();
                    
                }
            }
            else {
                stopwatch = 0f;
            }
        }
    }
}*/