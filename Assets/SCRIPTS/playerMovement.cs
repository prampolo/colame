using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Assets.SerialPortUtility.Scripts;

public class playerMovement : MonoBehaviour
{
    public float speedfactor;
    float stepDistance = 0.5f; // Distance to move forward per valid step

    public static int indexPlayer;
    public static int indexCrystal;

    string game_type = "alternate_step";

    private Rigidbody rb;
    private SerialCommunicationFacade serialFacade;

    private readonly Queue<Action> mainThreadActions = new Queue<Action>();
    private readonly object queueLock = new object();

    public string serialPortName = "COM1";
    public int baudRate = 9600;

    // Step logic variables
    private float right_foot = 0f;
    private float left_foot = 0f;
    private float threshold = 400f;

    private bool wasRightAbove = false;
    private bool wasLeftAbove = false;

    private bool stepLeft = false;
    private bool stepRight = false;
    private string lastStep = ""; // "left" or "right"

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        transform.position = new Vector3(0f, 0.34f, 0f); // Initial position

        serialFacade = new SerialCommunicationFacade();
        try
        {
            serialFacade.Connect(baudRate, serialPortName);
            serialFacade.OnSerialMessageReceived += OnSerialData;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SERIAL] Cannot open port {serialPortName}: {ex.Message}");
        }
    }

    void Update()
    {
        // Keep player's Y position fixed
        transform.position = new Vector3(transform.position.x, 0.34f, transform.position.z);

        // Execute main-thread actions from serial callbacks
        lock (queueLock)
        {
            while (mainThreadActions.Count > 0)
                mainThreadActions.Dequeue()?.Invoke();
        }

    }

    void FixedUpdate()
    {
        // No continuous movement
    }

    void OnSerialData(byte[] data)
    {
        string raw = Encoding.ASCII.GetString(data).Trim();
        EnqueueAction(() => HandleSerialInput(raw));
    }

    void HandleSerialInput(string message)
    {
        Debug.Log($"[SERIAL] Received: {message}");

        string[] parts = message.Split(';');
        if (parts.Length != 3)
        {
            Debug.LogWarning($"[SERIAL] Invalid format. Parts.Length = {parts.Length}");
            return;
        }

        try
        {
            string[] rightSensors = parts[1].Split(',');
            string[] leftSensors = parts[2].Split(',');

            Debug.Log($"[SERIAL] Parsed sensors - R: {rightSensors.Length}, L: {leftSensors.Length}");

            if (rightSensors.Length != 4 || leftSensors.Length != 4)
            {
                Debug.LogWarning("[SERIAL] Expected 4 sensors per foot, but got " +
                                 $"R={rightSensors.Length}, L={leftSensors.Length}");
                return;
            }

            if (game_type == "alternate_step")
            {
                Debug.Log("game_type: alternate_step");
                float sumRight = 0f, sumLeft = 0f;
                for (int i = 0; i < 4; i++)
                {
                    float valR = float.Parse(rightSensors[i]);
                    float valL = float.Parse(leftSensors[i]);
                    sumRight += valR;
                    sumLeft += valL;

                    Debug.Log($"[SENSORS] R{i + 1}={valR}, L{i + 1}={valL}");
                }

            
                right_foot = sumRight / 4f;
                left_foot = sumLeft / 4f;

                Debug.Log($"[FOOT] AVG Right: {right_foot:F1}, AVG Left: {left_foot:F1}");

                bool isRightAbove = right_foot > threshold;
                bool isLeftAbove = left_foot > threshold;

                Debug.Log($"[THRESHOLD] isRightAbove={isRightAbove}, isLeftAbove={isLeftAbove} " +
                          $"(threshold={threshold})");
                Debug.Log($"[PREVIOUS] wasRightAbove={wasRightAbove}, wasLeftAbove={wasLeftAbove}");

                if (wasRightAbove && !isRightAbove && isLeftAbove)
                {
                    stepRight = true;
                    Debug.Log("[STEP CHECK] Right step transition detected");
                }
                if (wasLeftAbove && !isLeftAbove && isRightAbove)
                {
                    stepLeft = true;
                    Debug.Log("[STEP CHECK] Left step transition detected");
                }

                wasRightAbove = isRightAbove;
                wasLeftAbove = isLeftAbove;

                Debug.Log($"[STEP FLAGS] stepRight={stepRight}, stepLeft={stepLeft}, lastStep={lastStep}");

                if (stepRight && (string.IsNullOrEmpty(lastStep) || lastStep != "right"))
                {
                    Debug.Log("[STEP TRIGGER] Right foot step triggered");
                    MoveForward();
                    lastStep = "right";
                    stepRight = false;
                    stepLeft = false;
                }
                else if (stepLeft && (string.IsNullOrEmpty(lastStep) || lastStep != "left"))
                {
                    Debug.Log("[STEP TRIGGER] Left foot step triggered");
                    MoveForward();
                    lastStep = "left";
                    stepRight = false;
                    stepLeft = false;
                }
                else
                {
                    Debug.Log("[STEP TRIGGER] No valid alternated step to trigger movement");
                }
            }else if(game_type == "toes")
            {
                float sumRight = 0f, sumLeft = 0f;
                for (int i = 0; i < 3; i++)
                {
                    float valR = float.Parse(rightSensors[i]);
                    float valL = float.Parse(leftSensors[i]);
                    sumRight += valR;
                    sumLeft += valL;

                    Debug.Log($"[SENSORS] R{i + 1}={valR}, L{i + 1}={valL}");
                }


                right_foot = sumRight / 3f;
                left_foot = sumLeft / 3f;

                Debug.Log($"[FOOT] AVG Right: {right_foot:F1}, AVG Left: {left_foot:F1}");

                bool isRightToesAbove = right_foot > threshold;
                bool isLeftToesAbove = left_foot > threshold;
                bool isRightHeelBelow = float.Parse(rightSensors[3]) < threshold;
                bool isLeftHeelBelow = float.Parse(leftSensors[3]) < threshold;
                bool onToes = isRightHeelBelow && isLeftHeelBelow;

                if (wasRightAbove && !isRightToesAbove && isLeftToesAbove && onToes)
                {
                    stepRight = true;
                    Debug.Log("[STEP CHECK] Right step transition detected");
                }
                if (wasLeftAbove && !isLeftToesAbove && isRightToesAbove && onToes)
                {
                    stepLeft = true;
                    Debug.Log("[STEP CHECK] Left step transition detected");
                }

                wasRightAbove = isRightToesAbove;
                wasLeftAbove = isLeftToesAbove;

                Debug.Log($"[STEP FLAGS] stepRight={stepRight}, stepLeft={stepLeft}, lastStep={lastStep}");

                if (stepRight && (string.IsNullOrEmpty(lastStep) || lastStep != "right"))
                {
                    Debug.Log("[STEP TRIGGER] Right foot step triggered");
                    MoveForward();
                    lastStep = "right";
                    stepRight = false;
                    stepLeft = false;
                }
                else if (stepLeft && (string.IsNullOrEmpty(lastStep) || lastStep != "left"))
                {
                    Debug.Log("[STEP TRIGGER] Left foot step triggered");
                    MoveForward();
                    lastStep = "left";
                    stepRight = false;
                    stepLeft = false;
                }
                else
                {
                    Debug.Log("[STEP TRIGGER] No valid alternated step to trigger movement");
                }

            }
            else if (game_type == "heels")
            {
                float sumRight = 0f, sumLeft = 0f;
                for (int i = 0; i < 3; i++)
                {
                    float valR = float.Parse(rightSensors[i]);
                    float valL = float.Parse(leftSensors[i]);
                    sumRight += valR;
                    sumLeft += valL;

                    Debug.Log($"[SENSORS] R{i + 1}={valR}, L{i + 1}={valL}");
                }


                right_foot = sumRight / 3f;
                left_foot = sumLeft / 3f;

                Debug.Log($"[FOOT] AVG Right: {right_foot:F1}, AVG Left: {left_foot:F1}");

                bool isRightToesBelow = right_foot < threshold;
                bool isLeftToesBelow = left_foot < threshold;
                bool isRightHeelAbove = float.Parse(rightSensors[3]) > threshold;
                bool isLeftHeelAbove = float.Parse(leftSensors[3]) > threshold;
                bool onHeels = isRightToesBelow && isLeftToesBelow;

                if (wasRightAbove && !isRightHeelAbove && isLeftHeelAbove && onHeels)
                {
                    stepRight = true;
                    Debug.Log("[STEP CHECK] Right step transition detected");
                }
                if (wasLeftAbove && !isLeftHeelAbove && isRightHeelAbove && onHeels)
                {
                    stepLeft = true;
                    Debug.Log("[STEP CHECK] Left step transition detected");
                }

                wasRightAbove = isRightHeelAbove;
                wasLeftAbove = isLeftHeelAbove;

                Debug.Log($"[STEP FLAGS] stepRight={stepRight}, stepLeft={stepLeft}, lastStep={lastStep}");

                if (stepRight && (string.IsNullOrEmpty(lastStep) || lastStep != "right"))
                {
                    Debug.Log("[STEP TRIGGER] Right foot step triggered");
                    MoveForward();
                    lastStep = "right";
                    stepRight = false;
                    stepLeft = false;
                }
                else if (stepLeft && (string.IsNullOrEmpty(lastStep) || lastStep != "left"))
                {
                    Debug.Log("[STEP TRIGGER] Left foot step triggered");
                    MoveForward();
                    lastStep = "left";
                    stepRight = false;
                    stepLeft = false;
                }
                else
                {
                    Debug.Log("[STEP TRIGGER] No valid alternated step to trigger movement");
                }

            }

        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SERIAL] Parse error: {ex.Message}");
        }
    }


    private void MoveForward()
    {
        Vector3 stepVector = new Vector3(0f, 0f, stepDistance);
        Vector3 targetPos = rb.position + stepVector;

        Debug.Log($"[MOVE] Current pos: {rb.position} → Moving to: {targetPos}");

        if (rb == null)
        {
            Debug.LogWarning("[MOVE] Rigidbody is null!");
            return;
        }

        rb.MovePosition(targetPos);
    }



    // Adds main-thread-safe action to queue
    private void EnqueueAction(Action a)
    {
        lock (queueLock) { mainThreadActions.Enqueue(a); }
    }

    // Called externally to unfreeze the player's Rigidbody
    public void UnfreezeTheBall()
    {
        if (rb != null)
            rb.constraints = RigidbodyConstraints.None;
    }

    void OnApplicationQuit()
    {
        serialFacade?.Disconnect();
    }

    void OnDisable()
    {
        if (serialFacade != null)
        {
            serialFacade.Disconnect();
            Debug.Log("[SERIAL] Disconnected on disable.");
        }
    }

    public void SetAlternateStep()
    {
        game_type = "alternate_step";
    }

    public void SetToes()
    {
        game_type = "toes";
    }

    public void SetHeels()
    {
        game_type = "heels";
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("BorderRed"))
        {
            GetComponent<Renderer>().material = other.GetComponent<Renderer>().material;
            gameObject.tag = "PlayerRed";
            Destroy(other.gameObject, 1f);
            indexPlayer = 1;
        }

        if (other.gameObject.CompareTag("BorderGreen"))
        {
            GetComponent<Renderer>().material = other.GetComponent<Renderer>().material;
            gameObject.tag = "PlayerGreen";
            Destroy(other.gameObject, 1f);
            indexPlayer = 2;
        }

        if (other.gameObject.CompareTag("BorderBlue"))
        {
            GetComponent<Renderer>().material = GameObject.FindWithTag("BorderBlue").GetComponent<Renderer>().material;
            gameObject.tag = "PlayerBlue";
            Destroy(other.gameObject, 1f);
            indexPlayer = 3;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        float pitchValue = ((8 * speedfactor) - 560) / 900;

        if (other.CompareTag("BorderRed") || other.CompareTag("BorderGreen") || other.CompareTag("BorderBlue"))
        {
            Debug.Log("[TRIGGER] Entered border zone");
            ManageTheAudio.instance.Play("box", 0f);
            ManageTheAudio.instance.ListOfSounds[5].AUDIOsOURCE.pitch = pitchValue;
        }

        if (other.CompareTag("CrystalRed") && indexPlayer != 1)
        {
            Debug.Log("[GAME] Wrong crystal! Player is not red.");
            // Game over logic can go here
        }

        if (other.CompareTag("CrystalGreen") && indexPlayer != 2)
        {
            Debug.Log("[GAME] Wrong crystal! Player is not green.");
        }

        if (other.CompareTag("CrystalBlue") && indexPlayer != 3)
        {
            Debug.Log("[GAME] Wrong crystal! Player is not blue.");
        }
    }

}
