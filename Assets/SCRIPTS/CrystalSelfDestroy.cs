using UnityEngine;
using TMPro;
using System.Linq;

public class CrystalSelfDestroy : MonoBehaviour
{
    // This values are overwritten from the prefabs!
    public float triggerDistance = 6f;
    public float countdownDuration = 5f;

    private bool countdownStarted = false;
    private float countdownTimer;
    private Transform player;
    private TextMeshPro countdownText;
    private GameObject textObject;

    void Start()
    {
        countdownTimer = countdownDuration;

        textObject = new GameObject("CountdownText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = new Vector3(0, 5f, 0);

        countdownText = textObject.AddComponent<TextMeshPro>();
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.fontSize = 3;
        countdownText.color = Color.white;
        countdownText.text = "";
    }

    void Update()
    {
        if (player == null)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            GameObject foundPlayer = allObjects.FirstOrDefault(obj => obj.tag.Contains("Player"));
            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
            else
            {
                return;
            }
        }

        Vector3 flatCrystal = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatPlayer = new Vector3(player.position.x, 0f, player.position.z);
        float distance = Vector3.Distance(flatCrystal, flatPlayer);

        if (!countdownStarted && distance <= triggerDistance)
        {
            countdownStarted = true;
        }

        if (countdownStarted)
        {
            countdownTimer -= Time.deltaTime;
            countdownText.text = Mathf.Ceil(countdownTimer).ToString();

            if (countdownTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }

        if (Camera.main != null)
        {
            textObject.transform.rotation = Quaternion.LookRotation(textObject.transform.position - Camera.main.transform.position);
        }
    }
}
