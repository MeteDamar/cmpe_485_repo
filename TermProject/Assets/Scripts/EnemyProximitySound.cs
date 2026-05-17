using UnityEngine;

public class EnemyProximitySound : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public AudioSource audioSource;
    public AudioClip proximityClip;

    [Header("Distance")]
    public float maxAudibleDistance = 35f;
    public float minDistanceForMaxVolume = 4f;

    [Header("Volume")]
    [Range(0f, 1f)] public float maxVolume = 1f;
    [Range(0f, 1f)] public float minVolume = 0f;
    public float volumeSmoothSpeed = 6f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (proximityClip != null)
            audioSource.clip = proximityClip;
    }

    private void Start()
    {
        if (player == null)
        {
            PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();

            if (playerMovement != null)
                player = playerMovement.transform;
        }

        if (audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    private void Update()
    {
        if (player == null || audioSource == null)
            return;

        if (audioSource.clip == null && proximityClip != null)
            audioSource.clip = proximityClip;

        if (audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();

        float targetVolume = CalculateTargetVolume();
        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime * volumeSmoothSpeed);
    }

    private float CalculateTargetVolume()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer >= maxAudibleDistance)
            return 0f;

        if (distanceToPlayer <= minDistanceForMaxVolume)
            return maxVolume;

        float distancePercent = Mathf.InverseLerp(maxAudibleDistance, minDistanceForMaxVolume, distanceToPlayer);
        return Mathf.Lerp(minVolume, maxVolume, distancePercent);
    }
}
