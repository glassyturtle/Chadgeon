using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class particleScript : NetworkBehaviour
{
    [SerializeField] AudioClip[] soundEffects;
    [SerializeField] AudioSource sorce;


    // Start is called before the first frame update
    void Start()
    {
        if (sorce) sorce.Play();
        if (!IsOwner) return;
        StartCoroutine(Destroy());
    }

    private IEnumerator Destroy()
    {
        yield return new WaitForSeconds(5);
        Destroy(gameObject);
    }

    private void Awake()
    {
        if (sorce != null)
        {
            sorce.clip = soundEffects[Random.Range(0, soundEffects.Length)];
        }
    }

}
