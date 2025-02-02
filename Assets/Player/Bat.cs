using UnityEngine;

public class Bat : MonoBehaviour
{
    [Header("Settings")]
    public float deflectionForce = 20f; // Siła odbicia piłki.
    public Transform playerCamera;     // Kamera gracza (do kierunku).
    public LayerMask collisionMask;    // Maskowanie kolizji Raycasta (jeśli potrzebne).

    private Rigidbody ballRigidbody;  // Rigidbody piłki.
    private bool ballInRange = false; // Czy piłka jest w triggerze?
    public KeyCode deflectorKey;

    private void OnTriggerEnter(Collider other)
    {

        // Sprawdź, czy obiekt, który wszedł w trigger, to piłka
        if (other.CompareTag("Ball"))
        {
            ballInRange = true;
            ballRigidbody = other.GetComponent<Rigidbody>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Gdy piłka opuści trigger, resetuj flagę i referencję
        if (other.CompareTag("Ball"))
        {
            ballInRange = false;
            ballRigidbody = null;
        }
    }

    // Ta funkcja będzie wywoływana przez InputManager
    public void OnDeflectPerformed()
    {

        // Jeśli piłka jest w triggerze, odbij ją
        if (ballInRange && ballRigidbody != null)
        {
            DeflectBall();
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(deflectorKey)) OnDeflectPerformed();
    }

    private void DeflectBall()
    {
        // Zeruj prędkość piłki
        ballRigidbody.velocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;

        // Wyślij raycast w kierunku, w którym patrzy gracz
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        Vector3 deflectionDirection;

        // Jeśli Raycast trafi w coś, kieruj w stronę punktu trafienia
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, collisionMask))
        {
            deflectionDirection = (hit.point - ballRigidbody.transform.position).normalized;
        }
        else
        {
            deflectionDirection = playerCamera.forward; // Jeśli nic nie trafi, użyj kierunku patrzenia kamery
        }

        // Nadaj siłę piłce w wybranym kierunku
        ballRigidbody.AddForce(deflectionDirection * deflectionForce, ForceMode.Impulse);

        Debug.Log("Piłka odbita w kierunku: " + deflectionDirection);
    }
}
