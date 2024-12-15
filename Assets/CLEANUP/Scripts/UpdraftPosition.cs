using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdraftPosition : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // set the correct updraft y position
        Vector3 groundPosition = new Vector3();
        if (TerrainManager.current.TryGetPointOnTerrain(transform.position, out groundPosition, out _, out _))
        {
            transform.position = new Vector3(transform.position.x, groundPosition.y - 3, transform.position.z);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
