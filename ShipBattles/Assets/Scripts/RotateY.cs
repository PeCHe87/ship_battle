using UnityEngine;

public class RotateY : MonoBehaviour
{
    [SerializeField] float speed = 30f;

    public float Speed => speed;

    void Update()
    {
        transform.Rotate(0f, speed * Time.deltaTime, 0f);
    }
}
