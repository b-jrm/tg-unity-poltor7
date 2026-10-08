using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 5f;

    // Update is called once per frame
    void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;
    }
}
