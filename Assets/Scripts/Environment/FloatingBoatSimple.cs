using UnityEngine;

public class FloatingBoatSimple : MonoBehaviour
{
    public float bobSpeed = 1.5f;
    public float bobAmount = 0.04f;
    public float rockSpeed = 1.1f;
    public float rockAngle = 1.5f;

    private Vector3 initialPos;
    private Quaternion initialRot;

    void Start()
    {
        initialPos = transform.position;
        initialRot = transform.rotation;
    }

    public void SetParameters(float amount, float speed, float angle, float rockSpd = 1.0f)
    {
        bobAmount = amount;
        bobSpeed = speed;
        rockAngle = angle;
        rockSpeed = rockSpd;
    }

    void Update()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        float rock = Mathf.Sin(Time.time * rockSpeed + 0.5f) * rockAngle;
        transform.position = initialPos + new Vector3(0f, bob, 0f);
        transform.rotation = initialRot * Quaternion.Euler(0f, 0f, rock);
    }
}