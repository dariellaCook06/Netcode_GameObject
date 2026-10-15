using UnityEngine;
using Unity.Netcode;

public class SpinningObstacle : NetworkBehaviour
{
    public float rotationSpeed = 3000f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Lo que hacemos es comprobar que sea en el server
        if (IsServer)
        {
            // Desactivamos las fisicas del Rigidbody
            rb.isKinematic = true;
        }
    }

    private void FixedUpdate()
    {
        // Lo que hacemos es que solo el servidor se encarga de la rotación
        if (!IsServer) return;

        // Giramos el objeto sobre el eje Y
        transform.Rotate(0f, rotationSpeed * Time.fixedDeltaTime, 0f);
    }
}