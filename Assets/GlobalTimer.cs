using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalTimer : MonoBehaviour
{

    public static float globalTimer;

    // Start is called before the first frame update
    void Start()
    {
        globalTimer = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        globalTimer += Time.deltaTime;
    }
}
