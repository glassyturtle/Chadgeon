using Unity.Netcode;
using UnityEngine;
using Cinemachine;

public class PlayerScript : Pigeon
{
    [SerializeField] private GameObject nameText;
    private void Awake()
    {
        transform.position = new Vector3(Random.Range(-13, 13), Random.Range(-11, 19), 0);
    }
    private void Start()
    {
        OnPigeonSpawn();
        if (!IsOwner) return;
        FindObjectOfType<GameManager>().player = this;
        CinemachineVirtualCamera camera = FindObjectOfType<CinemachineVirtualCamera>();
        camera.Follow = transform;
    }
    private void HandleMovement()
    {
        Vector2 inputVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;
        HandleMovementServerRpc(inputVector);
    }

    [ServerRpc]
    private void HandleMovementServerRpc(Vector2 inputVector)
    {
        if (!isKnockedOut.Value && !isSlaming)
        {
            //Store user input as a movement vector
            body.AddForce(speed * Time.fixedDeltaTime * inputVector);
            CheckDirection(inputVector);
        }
        else if (isSlaming)
        {
            Vector2 direction = (slamPos - transform.position).normalized;
            CheckDirection(direction);
            body.AddForce(4 * speed * Time.fixedDeltaTime * direction);
            if ((transform.position - slamPos).sqrMagnitude <= 0.1f)
            {
                EndSlam();
            }
        }
    }

    private void Update()
    {
        SyncPigeonAttributes();
        if (!IsOwner) return;
        if (!isKnockedOut.Value)
        {
            if (!isSlaming)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 pos = transform.position;
                    pos = Vector2.MoveTowards(pos, Camera.main.ScreenToWorldPoint(Input.mousePosition), 0.5f);

                    Vector3 targ = pos;
                    targ.z = 0f;
                    targ.x -= transform.position.x;
                    targ.y -= transform.position.y;

                    float angle = Mathf.Atan2(targ.y, targ.x) * Mathf.Rad2Deg;
                    Quaternion theAngle = Quaternion.Euler(new Vector3(0, 0, angle));

                    PigeonAttackServerRpc(pos, theAngle);
                }
                else if (Input.GetKeyDown(KeyCode.Space) && canSlam)
                {
                    StartSlam(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                }

            }
        }
        else if (body.velocity.magnitude < 0.1f && canDeCollide)
        {
            canDeCollide = false;
            bodyCollider.enabled = false;
        }
    }
    private void FixedUpdate()
    {
        if (!IsOwner) return;
        HandleMovement();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("banish"))
        {
            transform.position = Vector3.zero;
        }
    }
}
