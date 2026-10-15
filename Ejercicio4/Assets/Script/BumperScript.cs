using Unity.Netcode;
using UnityEngine;

public class BumperScript : NetworkBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (rb != null)
        {
            // Hacemos que el bumper permanezca inmóvil
            rb.isKinematic = true;
        }
    }

    // Creamos la variable de impulso para que el rebote se note
    private float ColisionBumper = 375f;

    // Creamos una función de colisión para impulsar al jugador
    private void OnCollisionEnter(Collision collision)
    {
        // Comprobamos que la colisión se procese únicamente en el servidor
        if (!IsServer) return;

        // Obtenemos el Rigidbody del jugador
        Rigidbody jugadorRB = collision.rigidbody;

        // Comprobamos que el jugador tenga un Rigidbody dinámico
        if (jugadorRB != null && !jugadorRB.isKinematic)
        {
            // Calculamos la dirección contraria al bumper
            Vector3 impulsoCollision = jugadorRB.worldCenterOfMass - transform.position;

            // Evitamos que el jugador salga impulsado hacia arriba o abajo
            //impulsoCollision.y = 0f;

            // Comprobamos que la dirección sea correcta
            if (impulsoCollision.sqrMagnitude > 0.001f)
            {
                impulsoCollision.Normalize();

                // Aplicamos un impulso hacia atrás
                jugadorRB.AddForce(impulsoCollision * ColisionBumper, ForceMode.Impulse);
            }
        }
    }
}