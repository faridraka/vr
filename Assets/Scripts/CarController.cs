using UnityEngine;
using System.Collections;

public class CubeController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 180f;
    [SerializeField] private float _rotationSensitivity = 2f;
    [SerializeField] private float _minRotationX = -60f;
    [SerializeField] private float _maxRotationX = 60f;

    [Header("Anti-Stuck Settings (Pojokan)")]
    [SerializeField] private float sensorLength = 1.5f;     // Jarak jangkauan sensor laser depan
    [SerializeField] private float avoidSideForce = 5f;     // Kekuatan dorong otomatis agar menjauh dari pojokan
    [SerializeField] private LayerMask wallLayer;            // Memastikan sensor hanya mendeteksi objek dinding

    [Header("References (Auto Filled)")]
    public Transform MainCamera;
    private Transform myTransform;
    private Rigidbody rb;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float knockbackDuration = 0.25f;

    private float InputVertical, InputHorizontal, MouseX, MouseY;
    private float currentRotationX, currentRotationY;
    private Vector3 knockbackVelocity = Vector3.zero;
    private bool isKnockedBack = false;

    void Start()
    {
        myTransform = GetComponent<Transform>();
        
        if (MainCamera == null)
        {
            Camera cam = Camera.main;
            if (cam != null) MainCamera = cam.transform;
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        
        rb.freezeRotation = true; 
        rb.useGravity = true;

        if (_moveSpeed <= 0) _moveSpeed = 12f;
        if (_rotationSensitivity <= 0) _rotationSensitivity = 2f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentRotationX = myTransform.localEulerAngles.y;
        if (MainCamera != null) currentRotationY = MainCamera.localEulerAngles.x;

        // Otomatis set layer jika belum diatur di Editor
        if (wallLayer == 0) wallLayer = ~0; 
    }

    void Update()
    {
        InputVertical = Input.GetAxis("Vertical");
        InputHorizontal = Input.GetAxis("Horizontal");
        MouseX = Input.GetAxis("Mouse X");
        MouseY = Input.GetAxis("Mouse Y");

        if (MainCamera != null)
        {
            currentRotationX += MouseX * _rotationSensitivity;
            currentRotationY -= MouseY * _rotationSensitivity;
            currentRotationY = Mathf.Clamp(currentRotationY, _minRotationX, _maxRotationX);

            myTransform.localRotation = Quaternion.Euler(0f, currentRotationX, 0f);
            MainCamera.localRotation = Quaternion.Euler(currentRotationY, 0f, 0f);
        }
    }

    void FixedUpdate()
    {
        if (isKnockedBack)
        {
            myTransform.Translate(knockbackVelocity * Time.fixedDeltaTime, Space.World);
            return;
        }

        // Kalkulasi pergerakan standar WASD
        Vector3 moveDirection = (myTransform.forward * InputVertical) + (myTransform.right * InputHorizontal);
        
        // TAMBAHAN: Sistem Otomatis Penghindar Pojokan Jalan
        Vector3 avoidance = CalculateAvoidance();
        if (avoidance != Vector3.zero)
        {
            // Jika mendekati pojokan, arah gerak digeser menjauhi pojokan tersebut
            moveDirection += avoidance;
        }

        myTransform.Translate(moveDirection.normalized * _moveSpeed * Time.fixedDeltaTime, Space.World);
    }

    // Fungsi Sensor Laser (Raycast) untuk mendeteksi pojokan jalan
    private Vector3 CalculateAvoidance()
    {
        Vector3 avoidanceForce = Vector3.zero;

        // Tentukan posisi sensor di bemper kiri dan kanan depan mobil
        Vector3 leftSensorPos = myTransform.position - (myTransform.right * 0.4f) + (myTransform.forward * 0.5f);
        Vector3 rightSensorPos = myTransform.position + (myTransform.right * 0.4f) + (myTransform.forward * 0.5f);

        // Arahkan sensor agak serong keluar sedikit agar peka terhadap sudut pojokan
        Vector3 leftSensorDir = (myTransform.forward - myTransform.right * 0.5f).normalized;
        Vector3 rightSensorDir = (myTransform.forward + myTransform.right * 0.5f).normalized;

        RaycastHit hit;

        // Sensor Kiri mendeteksi pojokan kiri -> dorong mobil ke kanan
        if (Physics.Raycast(leftSensorPos, leftSensorDir, out hit, sensorLength, wallLayer))
        {
            if (hit.collider.CompareTag("Wall"))
            {
                avoidanceForce += myTransform.right * avoidSideForce;
            }
        }

        // Sensor Kanan mendeteksi pojokan kanan -> dorong mobil ke kiri
        if (Physics.Raycast(rightSensorPos, rightSensorDir, out hit, sensorLength, wallLayer))
        {
            if (hit.collider.CompareTag("Wall"))
            {
                avoidanceForce -= myTransform.right * avoidSideForce;
            }
        }

        return avoidanceForce;
    }

    // Menampilkan garis sensor berwarna di Scene View untuk memudahkan debug/analisis kamu
    void OnDrawGizmos()
    {
        if (myTransform == null) myTransform = transform;

        Vector3 leftSensorPos = myTransform.position - (myTransform.right * 0.4f) + (myTransform.forward * 0.5f);
        Vector3 rightSensorPos = myTransform.position + (myTransform.right * 0.4f) + (myTransform.forward * 0.5f);
        Vector3 leftSensorDir = (myTransform.forward - myTransform.right * 0.5f).normalized;
        Vector3 rightSensorDir = (myTransform.forward + myTransform.right * 0.5f).normalized;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(leftSensorPos, leftSensorDir * sensorLength);
        Gizmos.DrawRay(rightSensorPos, rightSensorDir * sensorLength);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Vector3 wallNormal = collision.contacts[0].normal;
            wallNormal.y = 0f;
            StartCoroutine(KnockbackCoroutine(wallNormal));
        }
    }

    private IEnumerator KnockbackCoroutine(Vector3 direction)
    {
        isKnockedBack = true;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            knockbackVelocity = Vector3.Lerp(direction * knockbackForce, Vector3.zero, elapsed / knockbackDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        knockbackVelocity = Vector3.zero;
        isKnockedBack = false;
    }
}
