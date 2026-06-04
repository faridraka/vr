using UnityEngine;

public class BebasLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;

    private float xRotation = 0f; // Untuk atas-bawah
    private float yRotation = 0f; // Untuk kanan-kiri

    void Start()
    {
        // Mengunci kursor di tengah layar game
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Mengambil input mouse dan dikalikan dengan sensitivitas & waktu
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Hitung kalkulasi rotasinya
        xRotation -= mouseY; // Mouse naik-turun mengubah rotasi sumbu X
        yRotation += mouseX; // Mouse kanan-kiri mengubah rotasi sumbu Y

        // Batasi rotasi atas-bawah agar tidak pusing (-90 sampai 90 derajat)
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Terapkan langsung kedua rotasi ke objek ini
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}