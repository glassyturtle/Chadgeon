using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChadAI : PigeonAI
{
    private void Update()
    {
        FindNearbyPigeon();
        FindNearbyFood();
    }
    private void FixedUpdate()
    {
       
    }
}
