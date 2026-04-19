using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Speed Balance")]
    [SerializeField] private float baseSpeed;
    [SerializeField] private float extraSignalSpeed;

    [Header("References")]
    public SignalReceptor PlayerSignalReceiver;
    [SerializeField] private Transform innerMesh;
    public GameObject collision;

    [Header("Misc")]
    private InputSystem_Actions inputSystemActions;
    public Transform currentZone = null;

    private Rigidbody rb; //hehe
    public bool controlsEnabled = true;
    [SerializeField]
    private Weapon weapon;

    public static PlayerController Instance;
    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody>();
        PlayerSignalReceiver = GetComponentInChildren<SignalReceptor>();
        weapon.signalReceptor = PlayerSignalReceiver;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inputSystemActions = new InputSystem_Actions();
        inputSystemActions.Enable();
    }

    private void OnDestroy()
    {
        inputSystemActions.Disable();
    }

    public Weapon GetWeapon()
    {
        return weapon;
    }

    private void Update()
    {
        if (controlsEnabled)
        {
            HandleRotation();
            HandleMovement();
        }
    }
    private void FixedUpdate()
    {
        HandleCurrentZone();
        if (controlsEnabled)
        {
            HandleMovement();

        }
    }
    void HandleMovement()
    {
        if (!currentZone)
            return;

        Vector2 moveDir = inputSystemActions.Player.Move.ReadValue<Vector2>();
        //Vector2 localMoveDir = currentZone.InverseTransformDirection(moveDir);
        Vector2 localMoveDir = moveDir;

        //rb.MovePosition(rb.transform.position + new Vector3(localMoveDir.x, localMoveDir.y, 0) * (baseSpeed + PlayerSignalReceiver.ReceptionStrenght * extraSignalSpeed) * Time.fixedDeltaTime);
        //transform.localPosition += new Vector3(localMoveDir.x, localMoveDir.y, 0) * (baseSpeed + PlayerSignalReceiver.ReceptionStrenght * extraSignalSpeed) * Time.deltaTime;
        //rb.position = transform.position;

        //need actual physics here if we dont want the player clipping trough walls
        rb.linearVelocity = new Vector3(localMoveDir.x, localMoveDir.y, 0) * (baseSpeed + PlayerSignalReceiver.ReceptionStrenght * extraSignalSpeed);
    }

    void HandleRotation()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos);
        worldMousePos.z = 0;

        Vector3 diff = worldMousePos - transform.position;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        innerMesh.transform.eulerAngles = new Vector3(0, 0, angle - 90);
    }

    //What the hell sure
    void HandleCurrentZone()
    {
        Physics.Raycast(transform.position + Vector3.back * 0.5f, Vector3.forward, out RaycastHit hit, 25, LayerMask.GetMask("Zone"));
        if (hit.collider != null)
        {
            if (transform.parent != hit.transform.parent)
            {
                //transform.SetParent(hit.transform.Find("combatHolder"),true);           
                transform.SetParent(hit.transform.parent, true);
                currentZone = hit.transform.parent;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            var enemy = collision.gameObject.GetComponentInParent<Enemy>();
            if (enemy != null)
                Destroy(enemy.gameObject);

            GameManager.Instance.TriggerGameOverSequence();
        }
    }
}
