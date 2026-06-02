using UnityEngine;
using System.Collections;

public class CubeController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _rotationSensitivity;
    [SerializeField] private float _scale;
    [SerializeField] private float _minRotationX = -90f;
    [SerializeField] private float _maxRotationX = 90f;
    public float InputVertical, InputHorizontal, MouseX, MouseY;
    private Transform myTransform;
    private float currentRotationX, currentRotationY;
    public Transform MainCamera;

    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;

    private Vector3 knockbackVelocity = Vector3.zero;
    private bool isKnockedBack = false;

    void Start()
    {
        myTransform = GetComponent<Transform>();
        currentRotationX = 0f;
        currentRotationY = 0f;
    }

    public void GetInput()
    {
        InputVertical = Input.GetAxis("Vertical");
        InputHorizontal = Input.GetAxis("Horizontal");
        MouseX = Input.GetAxis("Mouse X");
        MouseY = Input.GetAxis("Mouse Y");
    }

    public void Rotate()
    {
        currentRotationX += MouseX * _rotationSensitivity;
        currentRotationY -= MouseY * _rotationSensitivity;

        currentRotationY = Mathf.Clamp(currentRotationY, _minRotationX, _maxRotationX);

        myTransform.localRotation = Quaternion.Euler(0, currentRotationX, 0f);
        MainCamera.localRotation = Quaternion.Euler(currentRotationY, 0, 0f);
    }

    public void Move()
    {
        // Kalau sedang knockback, input player diblokir
        if (isKnockedBack)
        {
            myTransform.Translate(knockbackVelocity * Time.deltaTime, Space.World);
            return;
        }

        Vector3 flatForward = new Vector3(MainCamera.forward.x, 0f, MainCamera.forward.z).normalized;
        Vector3 flatRight   = new Vector3(MainCamera.right.x,   0f, MainCamera.right.z).normalized;

        myTransform.Translate(flatForward * InputVertical  * _moveSpeed * Time.deltaTime, Space.World);
        myTransform.Translate(flatRight   * InputHorizontal * _moveSpeed * Time.deltaTime, Space.World);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Ambil arah normal tembok → dorong ke arah itu
            Vector3 wallNormal = collision.contacts[0].normal;
            wallNormal.y = 0f; // biar tidak melayang ke atas

            StartCoroutine(KnockbackCoroutine(wallNormal));
        }
    }

    private IEnumerator KnockbackCoroutine(Vector3 direction)
    {
        isKnockedBack = true;
        knockbackVelocity = direction * knockbackForce;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            // Gaya dorong makin melemah seiring waktu (lerp ke nol)
            knockbackVelocity = Vector3.Lerp(direction * knockbackForce, Vector3.zero, elapsed / knockbackDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        knockbackVelocity = Vector3.zero;
        isKnockedBack = false;
    }

    void Update()
    {
        GetInput();
        Move();
        Rotate();
    }
}