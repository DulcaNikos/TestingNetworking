using Mirror;
using UnityEngine;

public class BenchActivator : MonoBehaviour
{
    [Header("Panel To Toggle (local player's UI)")]
    [SerializeField] public GameObject targetPanel;

    [Header("Detection Settings")]
    [SerializeField] public float detectionRadius = 3f;
    [SerializeField] public float checkInterval = 0.1f;

    private Transform localPlayerTransform;
    private float checkTimer;
    private bool panelActive;

    private void Start()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }
    }

    private void Update()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer < checkInterval)
            return;

        checkTimer = 0f;

        if (localPlayerTransform == null)
        {
            FindLocalPlayer();

            if (localPlayerTransform == null)
                return;
        }

        float distance = Vector3.Distance(
            transform.position,
            localPlayerTransform.position
        );

        bool shouldBeActive = distance <= detectionRadius;

        if (shouldBeActive != panelActive)
        {
            panelActive = shouldBeActive;

            if (targetPanel != null)
            {
                targetPanel.SetActive(panelActive);
            }
        }
    }

    private void FindLocalPlayer()
    {
        if (NetworkClient.localPlayer != null)
        {
            localPlayerTransform = NetworkClient.localPlayer.transform;
        }
    }
}