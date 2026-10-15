using Unity.Netcode;
using UnityEngine;

public class ProjectilScript : NetworkBehaviour
{
    public float velocidadBala = 20f;

    // Cuando aparece el proyectil
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Rigidbody rb = GetComponent<Rigidbody>();

            rb.useGravity = false;
            rb.linearVelocity = transform.forward * velocidadBala;

            // El proyectil desaparece después de 6 segundos
            Invoke("EliminarProyectil", 6f);
        }
    }

    // Cuando el proyectil choca con un objeto
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        PlayerController jugador = collision.gameObject.GetComponentInParent<PlayerController>();

        if (jugador != null)
        {
            jugador.CambiarColorPorImpactoServer();

            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    // Eliminamos el proyectil
    private void EliminarProyectil()
    {
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}
