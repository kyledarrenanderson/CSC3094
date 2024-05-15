using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class timerEvent : MonoBehaviour
{
    float time = 0;
    UnityEvent timer;

    //add all levelNode spreadDanger method as event listeners
    void Start()
    {
        if(timer == null)
        {
            timer = new UnityEvent();
        }
        GameObject[] allNodes = GameObject.FindGameObjectsWithTag("levelNode");
        foreach(GameObject node in allNodes)
        {
            levelNode n = node.transform.gameObject.GetComponent<levelNode>();
            if(n && n.tag != "Wall")
            {
                timer.AddListener(n.spreadDanger);
            }
        }
    }

    //every 0.5 seconds invoke all methods that are part of the listeners
    void FixedUpdate()
    {
        time += Time.deltaTime;
        if(time >= 0.5)
        {
            timer.Invoke();
            time = 0;
        }
    }
}
