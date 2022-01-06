using System.Collections.Generic;
using System.Linq;
using TMPro;

using UnityEngine;

public enum AIState
{
    PATROLLING, STANDING_GUARD, RETURN_HOME, INVESTIGATING, ALERTED, SEARCHING, CALL_FOR_HELP
}

[System.Serializable]
public struct StatusDisplay
{
    public char characterToDisplay;
    public Color displayColor;
}

[System.Serializable]
public struct Waypoint
{
    public GraphNode node;
    public float time;
}

[RequireComponent(typeof(CharacterController))]
public class AIAgent : MonoBehaviour
{
    [Header("Generic Variables")]
    public AgentPerception agentPerception;
    public AIState defaultAIState = AIState.PATROLLING;
    public float mass = 1f;
    public float maxSpeed = 3f;
    [Space]
    [SerializeField] Animator _characterModel;
    [SerializeField] float _lookSpeed;

    Vector3 _agentVelocity;
    AIState _aiState;
    CharacterController _characterController;
    float _timer = 0;
    bool _timerSet = false;
    Vector3 _steeringForce;
    bool _switchDirection = false;
    
    [Header("Patrolling Variables")]
    public List<Waypoint> patrollingWaypoints;
    public bool pingPongPatrol;
    public float waypointDetectionDistance = 1f;
    
    int _currentWaypointIndex = 0;
    Vector3 _facingDirection;

    [Header("Standing Guard Variables")]
    public Quaternion lookoutFromAngle;
    public Quaternion lookoutToAngle;

    [Header("Investigating/Alerted Variables")]
    public StatusDisplay investigatingDisplayInfo;
    public StatusDisplay alertedDisplayInfo;
    public StatusDisplay searchingDisplayInfo;
    [Space]
    public float memoryLength = 5f;
    [Tooltip("The increased length in seconds of the passive timer")]
    public float attentionLengthModifier = 5f;
    [Tooltip("The length in seconds until the active timer is set")]
    public float attentionPowerupModifier = 2f;
    public TextMeshProUGUI behaviourStatus;

    //Memory Record
    internal Dictionary<GameObject, Vector3> _lastKnownOOIPosition = new Dictionary<GameObject, Vector3>();
    internal Dictionary<GameObject, bool> _ooiCurrentlyVisible = new Dictionary<GameObject, bool>();
    internal Dictionary<GameObject, bool> _ooiCurrentlyFocused = new Dictionary<GameObject, bool>();
    internal Dictionary<GameObject, float> _passiveOOITimers = new Dictionary<GameObject, float>();
    internal Dictionary<GameObject, float> _activeOOITimers = new Dictionary<GameObject, float>();
    internal Queue<GameObject> _oois;

    GameObject _currentlySearchingFor;
    int _searchNode;
    int _prevSearchNode;
    [Header("Homecoming Variables")]
    public StatusDisplay returningHomeDisplayInfo;

    Graph _pathfindingGraph;
    Vector3 _homePosition;
    int _homePathNextNode;
    List<int> _homePathNodes;

    void Start()
    {
        if(!agentPerception)
        {
            agentPerception = GetComponentInChildren<AgentPerception>(true);
        }

        _aiState = defaultAIState;
        _characterController = GetComponent<CharacterController>();
        _facingDirection = Vector3.forward;
        _pathfindingGraph = FindObjectOfType<Graph>();
        _homePosition = _pathfindingGraph.nodes[_pathfindingGraph.FindNearestNode(transform.position)].transform.position;
        _oois = new Queue<GameObject>();
    }

    void Update()
    {
        behaviourStatus.transform.LookAt(Camera.main.transform);

        switch (_aiState)
        {
            case AIState.PATROLLING:
                behaviourStatus.enabled = false;

                if ((patrollingWaypoints[_currentWaypointIndex].node.transform.position - transform.position).magnitude < waypointDetectionDistance && !_timerSet)
                {
                    _timer = patrollingWaypoints[_currentWaypointIndex].time;
                    _timerSet = true;
                }
                else if ((patrollingWaypoints[_currentWaypointIndex].node.transform.position - transform.position).magnitude < waypointDetectionDistance && _timerSet)
                {
                    _agentVelocity = Vector3.zero;
                    transform.rotation = Quaternion.Slerp(Quaternion.Euler(0, transform.eulerAngles.y, 0),
                                                          Quaternion.Euler(0, transform.eulerAngles.y + 160f, 0), Time.deltaTime);
                }
                else
                {
                    _steeringForce = ((patrollingWaypoints[_currentWaypointIndex].node.transform.position - transform.position).normalized * maxSpeed) - _agentVelocity;
                    ApplyMotion();
                }

                if (_timerSet && _timer <= 0)
                {
                    SetNextWaypoint();
                }

                break;
            case AIState.STANDING_GUARD:
                behaviourStatus.enabled = false;

                if (!_timerSet)
                {
                    _timer = 5f;
                    _timerSet = true;
                }

                _agentVelocity = Vector3.zero;
                if (Quaternion.Angle(transform.rotation, lookoutToAngle) <= Quaternion.Angle(lookoutFromAngle, lookoutToAngle) &&
                   Quaternion.Angle(transform.rotation, lookoutFromAngle) <= Quaternion.Angle(lookoutFromAngle, lookoutToAngle))
                {
                    transform.rotation = Quaternion.Slerp(_switchDirection ? lookoutToAngle : lookoutFromAngle,
                                                          _switchDirection ? lookoutFromAngle : lookoutToAngle,
                                                          Mathf.SmoothStep(0f, 1f, 1f - (_timer / 5f)));
                }
                else if (Quaternion.Angle(transform.rotation, lookoutToAngle) > Quaternion.Angle(lookoutFromAngle, lookoutToAngle))
                {
                    _timerSet = false;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, lookoutFromAngle.eulerAngles.y - 10f, 0), Time.deltaTime);
                }
                else if (Quaternion.Angle(transform.rotation, lookoutFromAngle) > Quaternion.Angle(lookoutFromAngle, lookoutToAngle))
                {
                    _timerSet = false;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, lookoutToAngle.eulerAngles.y - 10f, 0), Time.deltaTime);
                }

                if (_timerSet && _timer <= 0)
                {
                    _switchDirection = !_switchDirection;
                }
                break;
            case AIState.INVESTIGATING:
                if (!_currentlySearchingFor) _currentlySearchingFor = _oois.Dequeue();

                if (!_timerSet)
                {
                    _timer = _passiveOOITimers[_currentlySearchingFor] / 2;
                    _timerSet = true;
                }

                if (_timer <= 0 && _timerSet)
                {
                    _switchDirection = !_switchDirection;
                }

                if ((_lastKnownOOIPosition[_currentlySearchingFor] - transform.position).magnitude < waypointDetectionDistance)
                {
                    _agentVelocity = Vector3.zero;
                    transform.rotation = Quaternion.Slerp(Quaternion.Euler(0, transform.eulerAngles.y, 0),
                                                          _switchDirection ? Quaternion.Euler(0, transform.eulerAngles.y + 80f, 0) : Quaternion.Euler(0, transform.eulerAngles.y - 80f, 0), Time.deltaTime);
                }
                else
                {
                    _steeringForce = ((_lastKnownOOIPosition[_currentlySearchingFor] - transform.position).normalized * maxSpeed) - _agentVelocity;
                    ApplyMotion();
                }
                behaviourStatus.enabled = true;

                behaviourStatus.text = investigatingDisplayInfo.characterToDisplay.ToString();
                behaviourStatus.color = investigatingDisplayInfo.displayColor;
                agentPerception._fovMesh._viewMeshFilter.GetComponent<MeshRenderer>().material.color = new Color(investigatingDisplayInfo.displayColor.r,
                                                                                                                 investigatingDisplayInfo.displayColor.g,
                                                                                                                 investigatingDisplayInfo.displayColor.b,
                                                                                                                 agentPerception._fovMesh._viewMeshFilter.GetComponent<MeshRenderer>().material.color.a);
                break;
            case AIState.ALERTED:
                agentPerception.sightRadiusModifier = agentPerception.sightFocusedModifier;
                if (!_timerSet)
                {
                    _timer = _passiveOOITimers[_currentlySearchingFor] / 2;
                    _timerSet = true;
                }

                if (_timer <= 0 && _timerSet)
                {
                    _switchDirection = !_switchDirection;
                }

                if ((_lastKnownOOIPosition[_currentlySearchingFor] - transform.position).magnitude < waypointDetectionDistance)
                {
                    _agentVelocity = Vector3.zero;
                    transform.rotation = Quaternion.Slerp(Quaternion.Euler(0, transform.eulerAngles.y, 0),
                                                          _switchDirection ? Quaternion.Euler(0, transform.eulerAngles.y + 80f, 0) : Quaternion.Euler(0, transform.eulerAngles.y - 80f, 0), Time.deltaTime);
                }
                else
                {
                    _steeringForce = ((_lastKnownOOIPosition[_currentlySearchingFor] - transform.position).normalized * maxSpeed) - _agentVelocity;
                    ApplyMotion();
                }
                behaviourStatus.enabled = true;

                behaviourStatus.text = alertedDisplayInfo.characterToDisplay.ToString();
                behaviourStatus.color = alertedDisplayInfo.displayColor;
                agentPerception._fovMesh._viewMeshFilter.GetComponent<MeshRenderer>().material.color = new Color(alertedDisplayInfo.displayColor.r,
                                                                                                                 alertedDisplayInfo.displayColor.g,
                                                                                                                 alertedDisplayInfo.displayColor.b,
                                                                                                                 agentPerception._fovMesh._viewMeshFilter.GetComponent<MeshRenderer>().material.color.a);
                break;
            case AIState.SEARCHING:

                if ((_pathfindingGraph.nodes[_searchNode].transform.position - transform.position).magnitude < waypointDetectionDistance)
                {
                    int tempInvestigateNearby = Random.Range(0, _pathfindingGraph.nodes[_searchNode].adjacencyList.Count - 1);
                    print(tempInvestigateNearby);
                    List<GraphEdge> tempList = _pathfindingGraph.nodes[_searchNode].adjacencyList.Where(x => x.toNodeIndex != _prevSearchNode && x.toNodeIndex != _searchNode).ToList();
                    print(tempList.Count + " " + tempList[tempInvestigateNearby].toNodeIndex);
                    print(_searchNode + " " + _prevSearchNode);
                    print("-------------");

                    int tempSearch = tempList[tempInvestigateNearby].toNodeIndex;
                    _prevSearchNode = _searchNode;
                    _searchNode = tempSearch;
                }
                else
                {
                    _steeringForce = ((_pathfindingGraph.nodes[_searchNode].transform.position - transform.position).normalized * maxSpeed) - _agentVelocity;
                    ApplyMotion();
                }

                behaviourStatus.enabled = true;

                behaviourStatus.text = searchingDisplayInfo.characterToDisplay.ToString();
                behaviourStatus.color = searchingDisplayInfo.displayColor;
                agentPerception._fovMesh._viewMeshFilter.GetComponent<MeshRenderer>().material.color = new Color(searchingDisplayInfo.displayColor.r, 
                                                                                                                 searchingDisplayInfo.displayColor.g, 
                                                                                                                 searchingDisplayInfo.displayColor.b, 
                                                                                                                 agentPerception._fovMesh._viewMeshFilter.GetComponent<MeshRenderer>().material.color.a);
                break;
            case AIState.RETURN_HOME:
                if ((_pathfindingGraph.nodes[_homePathNodes[_homePathNextNode]].transform.position - transform.position).magnitude < waypointDetectionDistance)
                {
                    _homePathNextNode++;
                    if (_homePathNextNode >= _homePathNodes.Count)
                    {
                        _aiState = defaultAIState;
                    }
                }

                if (_homePathNextNode < _homePathNodes.Count) _steeringForce = ((_pathfindingGraph.nodes[_homePathNodes[_homePathNextNode]].transform.position - transform.position).normalized * maxSpeed) - _agentVelocity;
                else _steeringForce = ((_homePosition - transform.position).normalized * maxSpeed) - _agentVelocity;
                ApplyMotion();
                behaviourStatus.enabled = true;

                behaviourStatus.text = returningHomeDisplayInfo.characterToDisplay.ToString();
                behaviourStatus.color = returningHomeDisplayInfo.displayColor;
                agentPerception._fovMesh._viewMeshFilter.GetComponent<MeshRenderer>().material.color = new Color(returningHomeDisplayInfo.displayColor.r,
                                                                                                                 returningHomeDisplayInfo.displayColor.g,
                                                                                                                 returningHomeDisplayInfo.displayColor.b,
                                                                                                                 agentPerception._fovMesh._viewMeshFilter.GetComponent<MeshRenderer>().material.color.a);
                break;
        }

        //Behaviour Switching
        if((_aiState == AIState.INVESTIGATING && _ooiCurrentlyFocused.Count > 0) || (_aiState == AIState.SEARCHING && _ooiCurrentlyVisible[_currentlySearchingFor]))
        {
            _aiState = AIState.ALERTED;
        }
        if(_aiState <= (AIState)2 && _ooiCurrentlyVisible.Count > 0)
        {
            _aiState = AIState.INVESTIGATING;
            _timer = 0f;
            _timerSet = true;
        }

        foreach (GameObject go in _oois)
        {
            RemoveOoIFromMemory(go);
        }

        if (_currentlySearchingFor)
        {
            RemoveOoIFromMemory(_currentlySearchingFor);
        }

        _characterController.Move(_agentVelocity * Time.deltaTime);

        if (_timer > 0) _timer -= Time.deltaTime;
        else _timerSet = false;
    }

    void ApplyMotion()
    {
        _agentVelocity += Vector3.ClampMagnitude((_steeringForce / mass) * Time.deltaTime, maxSpeed);
        _facingDirection = new Vector3(_agentVelocity.x, 0, _agentVelocity.z).normalized;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_facingDirection, Vector3.up), Time.deltaTime * _lookSpeed);
    }

    void RemoveOoIFromMemory(GameObject ooi)
    {
        if (_ooiCurrentlyVisible.ContainsKey(ooi))
        {
            if (_passiveOOITimers.ContainsKey(ooi) && !_ooiCurrentlyVisible[ooi] && (!_activeOOITimers.ContainsKey(ooi) || (_activeOOITimers.ContainsKey(ooi) && _activeOOITimers[ooi] <= 0)))
            {
                if (_passiveOOITimers[ooi] > 0) _passiveOOITimers[ooi] -= Time.deltaTime;
                else
                {
                    if (_ooiCurrentlyVisible.ContainsKey(ooi)) _ooiCurrentlyVisible.Remove(ooi);
                    if (_ooiCurrentlyFocused.ContainsKey(ooi)) _ooiCurrentlyFocused.Remove(ooi);
                    if (_passiveOOITimers.ContainsKey(ooi)) _passiveOOITimers.Remove(ooi);
                    if (_activeOOITimers.ContainsKey(ooi)) _activeOOITimers.Remove(ooi);
                    if (_lastKnownOOIPosition.ContainsKey(ooi)) _lastKnownOOIPosition.Remove(ooi);
                    if (_oois.Contains(ooi)) _oois = new Queue<GameObject>(_oois.Where(x => !x.Equals(ooi)));

                    _aiState = AIState.RETURN_HOME;
                    _homePathNodes = _pathfindingGraph.FindPath(_pathfindingGraph.FindNearestNode(transform.position), _pathfindingGraph.FindNearestNode(_homePosition));
                    _homePathNodes.Reverse();
                    _homePathNextNode = 0;
                    _currentWaypointIndex = 0;
                }
            }

            if (_activeOOITimers.ContainsKey(ooi) && !_ooiCurrentlyVisible[ooi] && _ooiCurrentlyFocused[ooi])
            {
                if (_activeOOITimers[ooi] > 0) _activeOOITimers[ooi] -= Time.deltaTime;
                else
                {
                    if (_ooiCurrentlyFocused.ContainsKey(ooi)) _ooiCurrentlyFocused[ooi] = false;
                    agentPerception._fovMesh._focusedFOV = false;
                    agentPerception.sightRadiusModifier = 1f;
                    _prevSearchNode = _searchNode = _pathfindingGraph.FindNearestNode(transform.position);
                    _aiState = AIState.SEARCHING;
                }
            }
        }
    }

    Waypoint GetNextWaypoint() { return patrollingWaypoints[_currentWaypointIndex]; }
    void SetNextWaypoint() 
    {
        if(!pingPongPatrol || (!_switchDirection && pingPongPatrol))
        {
            if(_currentWaypointIndex + 1 < patrollingWaypoints.Count)
            {
                _currentWaypointIndex++;
            }
            else
            {
                if (!pingPongPatrol) _currentWaypointIndex = 0;
                else _switchDirection = !_switchDirection;
            }
        }
        else
        {
            if (_currentWaypointIndex > 0)
            {
                _currentWaypointIndex--;
            }
            else
            {
                _switchDirection = !_switchDirection;
            }
        }
    }
}
