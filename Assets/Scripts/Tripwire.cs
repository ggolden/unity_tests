using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tripwire : MonoBehaviour
{
    // for timer
    public bool withTimer;
    float time;
    float interval;

    // Start is called before the first frame update
    void Start()
    {
         Debug.Log("Tripwire: Start(): " + gameObject.name);
         time = 0;
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (withTimer) {
            Debug.Log("Tripwire: OnTriggerEnter(): " + gameObject.name + " and " + other.name 
                + " at " + time + " interval " + (time - interval));
            interval = time;            
        } else {
            Debug.Log("Tripwire: OnTriggerEnter(): " + gameObject.name + " and " + other.name);
        }
    }
}
