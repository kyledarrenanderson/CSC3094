using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DEMONodeClicker : MonoBehaviour
{
    public levelNode targetLocation;
    
    void Start()
    {
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
    }
}
