using UnityEngine;

public class RotateY : MonoBehaviour
{
    [SerializeField] float speed = 30f;

    void Update()
    {
        transform.Rotate(0f, speed * Time.deltaTime, 0f);
    }
}
