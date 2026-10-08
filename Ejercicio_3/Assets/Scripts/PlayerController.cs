using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    // ------------ COLOR RANDOM ------------
    // Creamos una variable para el color
    private Renderer playerRenderer;

    // Declaramos la variable del color del jugador
    private readonly NetworkVariable<Color> ColorPlayer = new NetworkVariable<Color>(Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );
    // Declaramos el nombre del jugador (se inicializa en cadena vacía para evitar discrepancias de sincronización inicial)
    private readonly NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>("Jugador",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    // Creamos una función para aplicar el nuevo color
    void OnColorChange(Color previousValue, Color newValue)
    {
        ApplyColor(newValue);
    }

    // Creamos una funcion para poder mostrar el nombre con el ID
    void OnNameChange(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        Debug.Log($"[RED] El nombre del cliente {OwnerClientId} ha cambiado a {newValue}");
    }

    public override void OnNetworkDespawn()
    {
        // Lo que hacemos es llamar al metodos para limpiarlo
        base.OnNetworkDespawn();

        // Lo que hacemos es que no ejecute repetidamente
        ColorPlayer.OnValueChanged -= OnColorChange;
        PlayerName.OnValueChanged -= OnNameChange;

    }

    // Creamos la función para aplicar el color
    private void ApplyColor(Color newColor)
    {
        if (playerRenderer != null)
        {
            playerRenderer.material.color = newColor;
        }
    }

    // Hacemos que el ServerRpc cambie el color del usuario y lo envíe al cliente
    [ServerRpc]
    private void ColorChangeServerRpc()
    {
        ColorPlayer.Value = new Color(Random.value, Random.value, Random.value, 1.0f);
    }


    // Creamos un rigidbody que hará referenia del jugador
    // Se usara para aplicar los movimientos y físicas
    private Rigidbody rb;

    // Creamos la velocidad que se movera el jugador
    public float speed = 5f;

    // Guardaremos la dirección de movimiento introducida por el jugador
    // X = izquierda/derecha ---- Y = arriba/abajo
    private Vector2 inputVector;

    // Creamos la variable del salto
    private float Jump = 6f;

    // Creamos la variable el sonido del salto
    private AudioSource jumpSound;

    // Una función que se utiliza para poder cargar/crear el objeto
    // Ejemplo: Es como decir a unity que cuando aparezca el jugador, que haga esto
    private void Awake()
    {
        // Buscamos el rigidbody del jugador y lo guardamos en RB de la variable
        rb = GetComponent<Rigidbody>();

        // Aqui pondremos la variable de sonido, que busca el objeto
        jumpSound = GetComponent<AudioSource>();

        if (playerRenderer == null)
        {
            playerRenderer = GetComponent<Renderer>();
        }
    }

    // Esto nos indicara que cuando este creado el jugador aplique esto.
    public override void OnNetworkSpawn()
    {
        // Sobreescribira el OnNetworkSpawn de pasra que no sea el por defecto
        base.OnNetworkSpawn();

       
        ColorPlayer.OnValueChanged += OnColorChange;
        PlayerName.OnValueChanged += OnNameChange;

        if (IsServer)
        {
            // Ponemos que el servidor ponga al player un color random
            ColorPlayer.Value = new Color(Random.value, Random.value, Random.value, 1.0f);

            // Mostramos el nombre del jugador con su ID
            PlayerName.Value = $"Jugador {OwnerClientId}";

            // Ejemplo: El servidor se encarga de la física del Rigidbody
            rb.isKinematic = false;
        }
        else
        {
            // Ejemplo: En el cliente desactivamos la física del Rigidbody.
            rb.isKinematic = true;
        }

        // Aplicamos el color 
        ApplyColor(ColorPlayer.Value);
    }

    // ----------- Server RCP -----------
    [ServerRpc]
    // Esta función puede ser llamada desde el cliente pero lo ejecutara el server
    private void SubmitInputServerRpc(Vector2 input)
    {
        inputVector = input;
    }

    // ----------- SALTO -----------
    [ServerRpc]
    private void JumpServerRpc()
    {
        // La funcion se calcelara si el jugador no toca el suelo
        if (!Physics.Raycast(transform.position, Vector3.down, 1.1f)) return;

        // Aplicamos aquí un impulso de fuerza
        rb.AddForce(Vector3.up * Jump, ForceMode.Impulse);

        // Aquí lo que hace es avisar a los clientes para sonar el salto
        PlayJumpSoundClientRpc();
    }

    [ClientRpc]
    private void PlayJumpSoundClientRpc()
    {
        // Reproducira el sonido del salto localmente
        if (jumpSound != null)
        {
            jumpSound.Play();
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return; // Solo el jugador propio puede controla su personaje

        float h = 0f;
        float v = 0f;

        // Si el jugador al precionar una tecla no es nulo hará estas acciones
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
        }

        Vector2 input = new Vector2(h, v).normalized;

        SubmitInputServerRpc(input);

        // ---- Salto ----
        // Avisaremos al servidor que el cliente a saltado
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            JumpServerRpc();
        }


        //  ------------ COLOR ------------
        // Solo el dueño puede solicitar el cambio de color al presionar la C
        if (IsOwner && Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            ColorChangeServerRpc();
        }

    }

    // ----------- MOVIMIENTO REAL -----------
    private void FixedUpdate()
    {
        if (!IsServer) return; // En caso que no sea el server no hara nada

        Vector3 moveDirection = new Vector3(inputVector.x, 0f, inputVector.y);

        Vector3 TargetVelocity = moveDirection * speed;
        rb.linearVelocity = new Vector3(TargetVelocity.x, rb.linearVelocity.y, TargetVelocity.z);


        // ------ LOOK AT ------
        // Aquí calculamos el punto al que el jugador querrá mirar
        // Se calcula "nuestra posición + la dirección en la que nos movemos"
        Vector3 puntoAMirar = transform.position + moveDirection;

        // Solo giraremos si nos movemos
        if (moveDirection != Vector3.zero)
        {
            // Giramos para mirar a ese punto
            transform.LookAt(puntoAMirar);
        }

        // Utilizamos el vector3.zero para que el jugador cuando no precione nada se quede quieto
        rb.angularVelocity = Vector3.zero;
    }
}
