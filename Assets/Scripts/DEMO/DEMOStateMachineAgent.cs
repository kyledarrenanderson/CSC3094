using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
//TODO


public class DEMOStateMachineAgent : MonoBehaviour
{
    //Unity NavMeshAgent
    UnityEngine.AI.NavMeshAgent agent;

    //GameObject Variables
    GameObject[] allNodes;
    public GameObject[] friendlyAgents;
    GameObject[] enemyAgents;
    DEMOEnemyAgent targetAgent;
    
    //LayerMask
    public LayerMask LevelComponents;
    public LayerMask AIAgents;


    public int agentID;

    //Boolean Variables
    bool inList;
    bool recalculatePath = false;
    public bool validPath;

    //Integer Variables
    public int failedPathCount;
    public int health = 100;
    

    //Float Variables
    public float fieldOfView = 45;
    float newH;
    float newG;
    float newF;
    float checkVisible = 0;
    float canShoot = 0;
    public float rotationSpeed = 100f;

    //levelNode Variables
    levelNode randomNode;
    levelNode node;
    levelNode n;
    levelNode visibleNode;
    public levelNode destinationLocation = null;
    public levelNode start = null;
    public levelNode currentNode;
    public levelNode previousNode;
    levelNode targetLocation;
    
    

    //List<levelNode> Variables
    public List<levelNode> path = new List<levelNode>();
    List<levelNode> openList = new List<levelNode>();
    List<levelNode> closedList = new List<levelNode>();
    public List<levelNode> nodesToIgnore = new List<levelNode>();
    List<levelNode> nearbyNodes = new List<levelNode>();
    List<levelNode> friendlyHalfNodes = new List<levelNode>();

    Vector3[] lineRenderPositions;
    public LineRenderer route;

    //Status for other agents to read to help them decide what to do0
    public enum Status
    {
        Roaming, //Explores the level area
        Holding, //Stationary, waiting for enemies to attack
        Fighting, //Enemy in line of site currently, takes priority above all else
        Pushing, //All units enter pushing at the same time, will all move to one location then enter defending
        Defending, //After pushing will move nearby the point they pushed and enter holding to defend the point
    }

    public Status currentStatus;
    Status previousStatus;

    //Picks a random noode from allNodes to be the destination of pathfinding
    levelNode generateTargetLocation()
    {
        if(allNodes != null)
        {
            if(currentStatus == Status.Roaming)
            {
                randomNode = friendlyHalfNodes[Random.Range(0,friendlyHalfNodes.Count - 1)];
                return randomNode;
            }
            else if(currentStatus == Status.Pushing)
            {
                if(targetLocation == destinationLocation)
                {
                    currentStatus = Status.Defending;
                    return generateTargetLocation();
                }
                else
                {
                    return targetLocation;
                }
                
            }
            else if(currentStatus == Status.Defending)
            {
                foreach(GameObject node in allNodes)
                {
                    n = node.GetComponent<levelNode>();
                    if((n.transform.position - transform.position).magnitude < 5 && n.nodeType != levelNode.NodeType.Wall && n != destinationLocation)
                    {
                        nearbyNodes.Add(n);
                    }
                }
                targetLocation = nearbyNodes[Random.Range(0,nearbyNodes.Count - 1)];
                currentStatus = Status.Holding;
                return targetLocation;
            }
            else if(currentStatus == Status.Holding)
            {
                return targetLocation;
            }
            else
            {
                return null;
            }
        
            
        }
        return null;
    }

    //Calculates euclidian distance between node and destination node
	float HeuristicCalculator(levelNode n, levelNode destinationNode)
	{
		return(n.transform.position - destinationNode.transform.position).magnitude;
	}

    //A* algorithm to find shortest path to target node from start point
	public bool FindPath(levelNode startNode, levelNode destinationNode)
	{
		path.Clear();
        if(!startNode || !destinationNode)
        {
            return false;
        }
        recalculatePath = false;
        startNode.parent = null;
        startNode.g = 0.0f;
        startNode.h = HeuristicCalculator(startNode, destinationNode);

        openList.Clear();
        closedList.Clear();

        openList.Add(startNode);

        while(openList.Count > 0)
		{
			node = openList[0];
			for(int i = 1; i < openList.Count; ++i)
			{
				if(openList[i].GenerateFScore(openList[i].g, openList[i].h) < node.GenerateFScore(node.g, node.h))
				{
					node = openList[i];
				}
				if(node == destinationNode)
                {
                    while(node)
                    {
                        path.Add(node);
                        node.previousType = node.nodeType;
                        node = node.parent;
                    }
                    path.Reverse();
                    nodesToIgnore.Clear();
                    return true;
                }
			}

			foreach(levelNode n in node.allAdjacent)
            {
                if(closedList.Contains(n))
                {
                    continue;
                }
                
                if(n.IsWall())
                {
                    closedList.Add(n);
                    continue;
                }

                if(nodesToIgnore.Count > 0)
                {
                    if(nodesToIgnore.Contains(n))
                    {
                        nodesToIgnore.Remove(n);
                        closedList.Add(n);
                        continue;
                    }
                }


                newH = HeuristicCalculator(n, destinationNode);
                newG = node.g + n.traversalCost;
                newF = newG + newH;
                inList = openList.Contains(n);

                if(newF < node.GenerateFScore(node.g, node.h) || !inList)
                {
                    if(!inList)
                    {
                        n.h = newH;
                        openList.Add(n);
                    }
                    n.g = newG;
                    n.h = newH;
                    n.parent = node;
                }
            }
            openList.Remove(node);
            closedList.Add(node);
		}
        return false;
	}

    //Checks to see what nodes are inside of the agents field of view
    void CanSeeNodes()
    {
        if(allNodes != null)
        {
            foreach(GameObject node in allNodes)
            {
                n = node.transform.gameObject.GetComponent<levelNode>();
                if(Mathf.Abs(Vector3.Angle(transform.forward, n.transform.position - transform.position))<fieldOfView)
                {
                    RaycastHit info;
                    if(Physics.Raycast(new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z),n.transform.position - transform.position, out info, Mathf.Infinity, LevelComponents))
                    {
                    visibleNode = info.transform.gameObject.GetComponent<levelNode>();    
                    if(visibleNode)
                        {
                            if(visibleNode.nodeType ==  levelNode.NodeType.Dangerous)
                            {
                                visibleNode.SetSafe();
                            }
                        }
                    }
                }
            }
        }
    }

    void drawRoute()
    {
        int pathSize = path.Count;
        lineRenderPositions = new Vector3[pathSize];
        for(int i = 0; i < pathSize; i++)
        {
            lineRenderPositions[i] = new Vector3(path[i].transform.position.x, path[i].transform.position.y + 1.5f, path[i].transform.position.z);
        }
        route.positionCount = pathSize;
        route.SetPositions(lineRenderPositions);
    }

    bool canSeeAgents()
    {
        foreach(GameObject enemyAgent in enemyAgents)
        {
            if(Mathf.Abs(Vector3.Angle(transform.forward, enemyAgent.transform.position - transform.position)) < fieldOfView)
            {
                RaycastHit info;
                if(Physics.Raycast(new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), enemyAgent.transform.position - transform.position, out info))
                {
                    DEMOEnemyAgent a = info.collider.GetComponent<DEMOEnemyAgent>();
                    if(a != null)
                    {
                        if(a.tag == "EnemyAgent")
                        {
                            targetAgent = a;
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    //sets up variables for other functions
    void Start()
    {
        currentStatus = Status.Roaming;
        route = GetComponent<LineRenderer>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        allNodes = GameObject.FindGameObjectsWithTag("levelNode");
        gameObject.tag = "temp";
        friendlyAgents = GameObject.FindGameObjectsWithTag("FriendlyAgentStateMachine");
        gameObject.tag = ("FriendlyAgentStateMachine");
        enemyAgents = GameObject.FindGameObjectsWithTag("EnemyAgent");
        float totalX = 0;
        float totalZ = 0;
        foreach(GameObject friendlyAgent in friendlyAgents)
        {
            totalX += friendlyAgent.transform.position.x;
            totalZ += friendlyAgent.transform.position.z;
        }
        float averageX = totalX / friendlyAgents.Length;
        float averageZ = totalZ / friendlyAgents.Length;
        Vector3 averagePosition = new Vector3(averageX, 0, averageZ);
        float totalDistance = 0f;
        foreach(GameObject node in allNodes)
        {
            totalDistance += (averagePosition - node.transform.position).magnitude;
        }

        float averageDistance = totalDistance / allNodes.Length;

        foreach(GameObject node in allNodes)
        {
            if((averagePosition - node.transform.position).magnitude < averageDistance)
            {
                friendlyHalfNodes.Add(node.GetComponent<levelNode>());
            }
        }

        

        destinationLocation = generateTargetLocation();
        agentID = gameObject.GetInstanceID();
        rotationSpeed = 100f;

    }

    void getGameObjects()
    {
        enemyAgents = GameObject.FindGameObjectsWithTag("EnemyAgent");
    }

    void FixedUpdate()
    {
        if(currentStatus != Status.Fighting)
        {
            if(agent.remainingDistance > 2f)
            {
                agent.destination = transform.position;
                recalculatePath = true;
            }
            
            //checks to see if a path exists to follow
            else if(path.Count == 0 || !validPath)
            {
                if(destinationLocation != null)
                {
                    if(destinationLocation.nodeType == levelNode.NodeType.Wall)
                    {
                        destinationLocation = generateTargetLocation();
                    }
                }
                //generate a path from the current location
                RaycastHit info;
	            Vector3 direction = new Vector3(0, -1, 0);
	            if (Physics.Raycast(transform.position, direction, out info))
	            {
		            start = info.collider.GetComponent<levelNode>();
                    if(start == null)
                    {
                        start = previousNode;
                    }
                    if(currentNode == destinationLocation)
                    {
                        path.Clear();
                        destinationLocation = generateTargetLocation();
                        failedPathCount = 0;
                    }
	            }
                if(currentNode != destinationLocation)
                {
                    validPath = FindPath(start, destinationLocation);
                }
                if(!validPath)
                {
                    failedPathCount += 1;
                }
                foreach(GameObject agent in friendlyAgents)
                {
                    //logic to try and get an agent unstuck should it become trapped
                    DEMOStateMachineAgent a = agent.GetComponent<DEMOStateMachineAgent>();
                    if(failedPathCount > 50 && a.path.Count > 0)
                    {
                        if(currentNode == a.path[0] && currentNode)
                        {
                            foreach(levelNode node in currentNode.allAdjacent)
                            {
                                if(!(node.nodeType == levelNode.NodeType.Wall || node.IsOccupied()))
                                {
                                    path.Insert(0, node);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            //if a path exists, follows
            else if(validPath)
            {
                //checks to see if the cost of any nodes on its path have changed
                foreach(levelNode node in path)
                {
                    if(node.nodeType != node.previousType)
                    {
                        //if a node has changed its value, recalculate the path and break from the loop as there is no point checking later nodes on its path
                        recalculatePath = true;
                        break;
                    }
                }
                //detects all agents inside of a spherical radius of 5 to reduce comparisons done
                foreach(GameObject agent in friendlyAgents)
                {
                    DEMOStateMachineAgent a = agent.GetComponent<DEMOStateMachineAgent>();
                    if(a.path.Count > 1 && path.Count > 1)
                    {
                        if((a.transform.position - transform.position).magnitude < 6)
                        {
                            if(a.path.Count > 7 && path.Count > 7)
                            {
                                //checks for agents pathing opposite directions into each other, accounts for both even and odd number of nodes apart
                                //the agent with the lowest id gets priority for pathfinding
                                //if there is not a valid path for an agent with a lower priority then an agent will find a new route
                                for(int i = 0; i < 6; i++)
                                {    
                                    if((a.path[i] == path[5-i] || a.path[i] == path[6-i]) && a.agentID < agentID)
                                    {
                                        nodesToIgnore.Add(path[i]);
                                    }
                                    else
                                    {
                                        nodesToIgnore.Clear();
                                    }
                                }
                                //Checks up to 6 nodes in advance for collisions
                                for(int i = 0; i < 6; i++)
                                {
                                    //add any potential collisions to nodesToIgnore
                                    if(a.path[i] == path[i] && a.agentID < agentID)
                                    {
                                        nodesToIgnore.Add(path[i]);
                                    }
                                }
                            }
                            //backup check from original LRAS
                            if(((a.path[0] == path[0]) && a.agentID < agentID))
                            {
                                nodesToIgnore.Add(path[0]);
                            }
                            if(((path[0] == a.previousNode && a.path[0] == previousNode) && a.agentID < agentID))
                            {
                                nodesToIgnore.Add(path[0]);
                            }
                            if((a.path[0] == path[0]) && (!(a.validPath)))
                            {
                                nodesToIgnore.Add(path[0]);
                            }
                        }
                        //edge cases where multiple agents have the same destination and reach it at the same time
                        if(path.Count == 1)
                        {
                            if(a.path[0] == path[0] && a.agentID < agentID )
                            {
                                start = destinationLocation;
                                nodesToIgnore.Clear();
                                break;
                            }
                        }
                    }
                }
                
                if(nodesToIgnore.Count > 0 || recalculatePath)
                {
                    validPath = FindPath(currentNode, destinationLocation);
                    recalculatePath = true;
                }
            }
            //when the agent gets close enough to its target node, sets the next node in path as its target
            if(path.Count > 0 && validPath)
            {
                try
                {
                    agent.destination = new Vector3((path[0]).transform.position.x,(path[0]).transform.position.y + 1.5f, (path[0]).transform.position.z);
                    if(agent.remainingDistance < 0.5f)
                    {
                        previousNode = path[0];
                        path.RemoveAt(0);
                        if(path.Count > 0)
                        {
                            currentNode = path[0];
                            currentNode.nodeType = levelNode.NodeType.Safe;
                        }
                    }
                    drawRoute();
                }
                catch
                {
                    //blank as this is a duct tape fix for an issue I couldnt solve
                } 
            }
        }
        else if(currentStatus == Status.Fighting)
        {
            path.Clear();
            path.Add(currentNode);
            agent.ResetPath();
            RaycastHit info;
            getGameObjects();
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation (targetAgent.transform.position - transform.position), rotationSpeed * Time.deltaTime);
            if(Physics.Raycast(new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), transform.forward, out info))
            {
                DEMOEnemyAgent currentTarget = info.collider.GetComponent<DEMOEnemyAgent>();
                if(targetAgent == currentTarget)
                {
                    if(canShoot > 0.2)
                    {
                        canShoot = 0;
                        DEMOEnemyAgent target = targetAgent.GetComponent<Collider>().GetComponent<DEMOEnemyAgent>();
                        target.health -= 5;
                    }
                }
            }
            
                
        }
        //every 0.2 seconds run CanSeeNodes
        //Human reaction time is around 0.2 seconds
        checkVisible += Time.deltaTime;
        canShoot += Time.deltaTime;
        if(checkVisible > 0.2)
        {
            checkVisible = 0;
            CanSeeNodes();
            if(canSeeAgents() && currentStatus != Status.Fighting)
            {
                previousStatus = currentStatus;
                currentStatus = Status.Fighting;
            }
            else
            {
                currentStatus = previousStatus;
            }
        }
        getGameObjects();
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(1) && currentStatus == Status.Roaming)
        {
            RaycastHit info;
            if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out info))
            {
                targetLocation = info.collider.GetComponent<levelNode>();
                if(targetLocation != null)
                {
                    currentStatus = Status.Pushing;
                    destinationLocation = generateTargetLocation();
                    validPath = false;
                    path.Clear();
                }
            }   
        }
        else if(Input.GetMouseButtonDown(1) && currentStatus != Status.Roaming && currentStatus != Status.Fighting)
        {
            currentStatus = Status.Roaming;
        }
    }
}