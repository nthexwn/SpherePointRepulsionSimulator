using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControls : MonoBehaviour
{
    public float mouseSensitivity = 400;
    public float keyboardSensitivity = 4;

    private float startRotationX = 25f;
    private float startRotationY = 0f;
    private float startRotationZ = 0f;
    private float startPositionX = 0f;
    private float startPositionY = 1f;
    private float startPositionZ = -2f;

    private float rotationX;
    private float rotationY;

    private void Start()
    {
        // Prevent cursor from getting in way of camera.
        /*
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        */

        // Reset to starting position and rotation.
        transform.rotation = Quaternion.Euler(startRotationX, startRotationY, startRotationZ);
        transform.position = new Vector3(startPositionX, startPositionY, startPositionZ);

        // Record initial orientation.
        rotationX = startRotationX;
        rotationY = startRotationY;
    }

    private void Update()
    {
        Vector3 translation;

        // Only accept input if right-mouse is pressed.
        if(Input.GetMouseButton(1))
        {
            // Process mouse input.
            rotationY += Input.GetAxisRaw("Mouse X") * Time.deltaTime * mouseSensitivity;
            rotationX -= Input.GetAxisRaw("Mouse Y") * Time.deltaTime * mouseSensitivity;
            rotationX = Mathf.Clamp(rotationX, -90, 90);
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);

            // Process keyboard input.
            translation = new Vector3(0, 0, 0);
            translation.z += Input.GetKey(KeyCode.W) ? 1 : 0;
            translation.x -= Input.GetKey(KeyCode.A) ? 1 : 0;
            translation.z -= Input.GetKey(KeyCode.S) ? 1 : 0;
            translation.x += Input.GetKey(KeyCode.D) ? 1 : 0;
            translation.y -= Input.GetKey(KeyCode.Q) ? 1 : 0;
            translation.y += Input.GetKey(KeyCode.E) ? 1 : 0;
            translation *= keyboardSensitivity * Time.deltaTime;
            transform.Translate(translation);
        }
    }
}
