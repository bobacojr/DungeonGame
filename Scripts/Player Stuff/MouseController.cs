using UnityEngine;

public class MouseController : MonoBehaviour
{

    public float mouseSensitivity = 120f;
    public float xRotation = 0f;

    public Transform playerBody;
    public Animator animator;

    void Awake()
    {
        animator = GetComponentInParent<Animator>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        animator.SetFloat("Turn", mouseX, 0.1f, Time.deltaTime);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Can't turn head around in circles

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
