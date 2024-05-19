using System.Collections;
using UnityEngine;

public class ParticleNonNetwork : MonoBehaviour
{



    void Start()
    {


        StartCoroutine(DestoryAfterSeconds());
    }
    IEnumerator DestoryAfterSeconds()
    {
        yield return new WaitForSeconds(2);
        Destroy(gameObject);
    }
}
