using UnityEngine;

public class PlayerScript : Pigeon
{
    private void Awake()
    {
        OnPigeonSpawn();
        transform.position = new Vector3(Random.Range(-13, 13), Random.Range(-11, 19), 0);
    }
    private void Update()
    {
        if(!isKnockedOut)
        {
            if (!isSlaming)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 pos = transform.position;
                    pos = Vector2.MoveTowards(pos, Camera.main.ScreenToWorldPoint(Input.mousePosition), 0.5f);
                    PigeonAttack(pos);
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
        if (!isKnockedOut && !isSlaming)
        {
            //Store user input as a movement vector
            Vector3 m_Input = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0).normalized;
            body.AddForce(m_Input * speed * Time.deltaTime);
            CheckPigeonDirection(new Vector2(m_Input.x, m_Input.y));
        }
        else if (isSlaming)
        {
            Vector2 direction = (slamPos -transform.position ).normalized;
            CheckPigeonDirection(direction);
            body.AddForce(direction * speed * 4 * Time.deltaTime);
            if((transform.position - slamPos).sqrMagnitude <= 0.1f)
            {
                EndSlam();
            }  
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("banish"))
        {
            transform.position = Vector3.zero;
        }
    }
}
