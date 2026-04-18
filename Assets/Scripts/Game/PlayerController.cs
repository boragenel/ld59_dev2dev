using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions inputSystemActions;
    public SignalReceptor playerSignalReceiver;

    public Transform currentZone = null;
    [SerializeField]
    private Transform innerMesh;
    [SerializeField]
    private float speed = 4;
    private Rigidbody rbody;
    public bool controlsEnabled = true;
    [SerializeField]
    private Weapon weapon;

    public static PlayerController Instance;
    private void Awake()
    {
        Instance = this;
        weapon.signalReceptor = playerSignalReceiver;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rbody = GetComponent<Rigidbody>();
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

    //What in the luney tunes ???????????????????
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

    void HandleMovement()
    {
        if (!currentZone)
            return;

        Vector2 moveDir = inputSystemActions.Player.Move.ReadValue<Vector2>();
        Vector2 localMoveDir = currentZone.InverseTransformDirection(moveDir);
        transform.localPosition += new Vector3(localMoveDir.x, localMoveDir.y, 0) * speed * Time.deltaTime;
        rbody.position = transform.position;
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

    // Update is called once per frame
    void Update()
    {
        HandleCurrentZone();
        if (controlsEnabled)
        {
            HandleMovement();
            HandleRotation();
        }

    }
}
