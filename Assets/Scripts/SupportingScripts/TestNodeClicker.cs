using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestNodeClicker : MonoBehaviour
{

    float totalFailures;
    float totalTimeTaken;
    public bool addTimes;
    public bool displayedResults = false;
    public GameObject[] friendlyASAgents;
    public GameObject[] friendlyLRASAgentsV1;
    public GameObject[] friendlyLRASAgentsV2;
    
    void Start()
    {
        friendlyASAgents = GameObject.FindGameObjectsWithTag("FriendlyAgentTestAStar");
        friendlyLRASAgentsV1 = GameObject.FindGameObjectsWithTag("FriendlyAgentTestLRAStarV1");
        friendlyLRASAgentsV2 = GameObject.FindGameObjectsWithTag("FriendlyAgentTestLRAStarV2");
    }

    void Update()
    {
        //on mouse click run this code
        if(Input.GetMouseButtonDown(0))
        {
            Ray ray = GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);

            RaycastHit info;
            if(Physics.Raycast(ray, out info))
            {
                //detect the node clicked, if dangerous(red) make safe(green) and vice versa, otherwise set dangerous
                levelNode node = info.collider.gameObject.GetComponent<levelNode>();
                if(node)
                {
                    if(node.nodeType == levelNode.NodeType.Safe)
                    {
                        node.SetDangerous();
                    }
                    else if(node.nodeType == levelNode.NodeType.Dangerous)
                    {
                        node.SetSafe();
                    }
                    else
                    {
                        node.SetDangerous();
                    }
                }
            }
        }
    }

    void FixedUpdate()
    {
        if(!displayedResults)
        {
            if(friendlyASAgents.Length != 0)
            {
                foreach(GameObject agent in friendlyASAgents)
                {
                    TESTAStarAgent a = agent.GetComponent<TESTAStarAgent>();
                    if(a.reachedDestination)
                    {
                        addTimes = true;
                    }
                    else
                    {
                        addTimes = false;
                        break;
                    }
                }
            }
            else if(friendlyLRASAgentsV1.Length != 0)
            {
                foreach(GameObject agent in friendlyLRASAgentsV1)
                {
                    TESTLRASBasic a = agent.GetComponent<TESTLRASBasic>();
                    if(a.reachedDestination)
                    {
                        addTimes = true;
                    }
                    else
                    {
                        addTimes = false;
                        break;
                    }
                }
            }
            else if(friendlyLRASAgentsV2.Length != 0)
            {
                foreach(GameObject agent in friendlyLRASAgentsV2)
                {
                    TESTLRASAgentImproved a = agent.GetComponent<TESTLRASAgentImproved>();
                    if(a.reachedDestination)
                    {
                        addTimes = true;
                    }
                    else
                    {
                        addTimes = false;
                        break;
                    }
                }
            }

            if(addTimes && Input.GetKeyDown(KeyCode.P))
            {
                if(friendlyASAgents.Length != 0)
                {
                    foreach(GameObject agent in friendlyASAgents)
                    {
                        TESTAStarAgent a = agent.GetComponent<TESTAStarAgent>();
                        totalTimeTaken += a.totalTime;
                        totalFailures += a.failureCount;
                    }
                }
                else if(friendlyLRASAgentsV1.Length != 0)
                {
                    foreach(GameObject agent in friendlyLRASAgentsV1)
                    {
                        TESTLRASBasic a = agent.GetComponent<TESTLRASBasic>();
                        totalTimeTaken += a.totalTime;
                        totalFailures += a.failureCount;
                    }
                }

                else if(friendlyLRASAgentsV2.Length != 0)
                {
                    foreach(GameObject agent in friendlyLRASAgentsV2)
                    {
                        TESTLRASAgentImproved a = agent.GetComponent<TESTLRASAgentImproved>();
                        totalTimeTaken += a.totalTime;
                        totalFailures += a.failureCount;
                    }
                }
                
                displayedResults = true;
                Debug.Log("Average Time Taken");
                Debug.Log(totalTimeTaken/1250);
                Debug.Log("Average Failures");
                Debug.Log(totalFailures/1250);
            }
        }
    }
}
