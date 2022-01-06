using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AgentPerception : MonoBehaviour
{
    [Header("Generic Variables")]
    public AIAgent aiAgent;

    [Header("Sight Variables")]
    public float sightRadius;
    [Range(0f, 5f)]
    public float sightFocusedModifier = 1.25f;
    public LayerMask sightObstructors;
    public float sightVisibleAngle;
    public float sightFocusedAngle;
    public float FOVMeshResolution;
    [Space]
    public List<string> sightSearchForTags;
    public List<GameObject> sightSearchForSpecificObjects;

    internal FieldOfViewMesh _fovMesh;
    internal float sightRadiusModifier = 1f;

    void Start()
    {
        GameObject tempGO = Instantiate(new GameObject("View Mesh"), transform);
        tempGO.AddComponent<MeshRenderer>();
        tempGO.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tempGO.GetComponent<MeshRenderer>().receiveShadows = false;
        tempGO.GetComponent<MeshRenderer>().material = Resources.Load<Material>("FOVVisualiser");

        _fovMesh = gameObject.AddComponent<FieldOfViewMesh>();
        _fovMesh._agentPerception = this;
        _fovMesh._focusedFOV = false;
        _fovMesh._viewMeshFilter = tempGO.AddComponent<MeshFilter>();
        _fovMesh._viewFocusSpeed = 0.5f;
        _fovMesh._edgeResolveIterations = 4;
        _fovMesh._edgeDistanceThreshold = 0.5f;

        if (!aiAgent)
        {
            aiAgent = GetComponentInParent<AIAgent>();
        }

        sightObstructors = ~sightObstructors;

        foreach (string tag in sightSearchForTags)
        {
            List<GameObject> tempObjectsWithTag = new List<GameObject>();
            tempObjectsWithTag.AddRange(GameObject.FindGameObjectsWithTag(tag));

            foreach(GameObject go in tempObjectsWithTag)
            {
                if(!sightSearchForSpecificObjects.Contains(go))
                {
                    sightSearchForSpecificObjects.Add(go);
                }
            }
        }
    }

    void Update()
    {
        foreach (GameObject go in sightSearchForSpecificObjects)
        {
            Vector3 tempVectorDistance = (go.GetComponent<Collider>() ? go.GetComponent<Collider>().bounds.center : go.transform.position) - transform.position;
            float tempRadiusSquared = Mathf.Pow((go.GetComponent<Collider>() ? go.GetComponent<Collider>().bounds.extents.GetVectorAsList().MostOf() : 1f) + (sightRadius * sightRadiusModifier), 2);

            if (tempVectorDistance.sqrMagnitude <= tempRadiusSquared)
            {
                RaycastHit tempHit;
                if (Physics.Raycast(transform.position, tempVectorDistance, out tempHit, sightRadius, ~(1 << LayerMask.NameToLayer("Guard"))))
                {
                    float tempFieldOfViewDetection = Vector3.Dot(tempVectorDistance.normalized, transform.forward.normalized);

                    if (sightObstructors == (sightObstructors | (1 << tempHit.collider.gameObject.layer)))
                    {
                        if (tempFieldOfViewDetection > Mathf.Cos(sightVisibleAngle * Mathf.Deg2Rad))
                        {
                            //Peripheral Vision
                            if (!aiAgent._ooiCurrentlyVisible.ContainsKey(go))
                            {
                                aiAgent._oois.Enqueue(go);

                                if (!aiAgent._ooiCurrentlyFocused.ContainsKey(go) || (aiAgent._ooiCurrentlyFocused.ContainsKey(go) && !aiAgent._ooiCurrentlyFocused[go]))
                                {
                                    _fovMesh._focusedFOV = false; 
                                    aiAgent._passiveOOITimers.Add(go, aiAgent.memoryLength);
                                }
                                aiAgent._lastKnownOOIPosition.Add(go, go.transform.position);
                                aiAgent._ooiCurrentlyVisible.Add(go, true);
                            }
                            else
                            {
                                if (!aiAgent._ooiCurrentlyFocused.ContainsKey(go) || (aiAgent._ooiCurrentlyFocused.ContainsKey(go) && !aiAgent._ooiCurrentlyFocused[go]))
                                {
                                    _fovMesh._focusedFOV = false;
                                    if (aiAgent._passiveOOITimers[go] < aiAgent.memoryLength) aiAgent._passiveOOITimers[go] = aiAgent.memoryLength;
                                    else if (aiAgent._passiveOOITimers[go] <= aiAgent.memoryLength + aiAgent.attentionLengthModifier) aiAgent._passiveOOITimers[go] += Time.deltaTime * (aiAgent.attentionLengthModifier / aiAgent.attentionPowerupModifier);
                                }
                                aiAgent._lastKnownOOIPosition[go] = go.transform.position;
                                aiAgent._ooiCurrentlyVisible[go] = true;
                            }


                            if (tempFieldOfViewDetection > Mathf.Cos(sightFocusedAngle * Mathf.Deg2Rad) && aiAgent._passiveOOITimers[go] > aiAgent.memoryLength + aiAgent.attentionLengthModifier)
                            {
                                //In Focus
                                if (!aiAgent._ooiCurrentlyFocused.ContainsKey(go))
                                {
                                    _fovMesh._focusedFOV = true;
                                    aiAgent._activeOOITimers.Add(go, aiAgent.memoryLength);
                                    aiAgent._ooiCurrentlyFocused.Add(go, true);
                                }
                                else
                                {
                                    _fovMesh._focusedFOV = true;
                                    aiAgent._activeOOITimers[go] = aiAgent.memoryLength;
                                    aiAgent._ooiCurrentlyFocused[go] = true;
                                }
                            }
                        }
                        else
                        {
                            if (aiAgent._ooiCurrentlyVisible.ContainsKey(go)) aiAgent._ooiCurrentlyVisible[go] = false;
                        }
                    }
                    else
                    {
                        if (aiAgent._ooiCurrentlyVisible.ContainsKey(go)) aiAgent._ooiCurrentlyVisible[go] = false;
                    }
                }
                else
                {
                    if (aiAgent._ooiCurrentlyVisible.ContainsKey(go)) aiAgent._ooiCurrentlyVisible[go] = false;
                }
            }
            else
            {
                if (aiAgent._ooiCurrentlyVisible.ContainsKey(go)) aiAgent._ooiCurrentlyVisible[go] = false;
            }

            print(string.Format("--- {0} ---\nVisible? {1} Focused? {2}\nPassive: {3}\nActive: {4}\n{5}",
                                aiAgent.name,
                                aiAgent._ooiCurrentlyVisible.ContainsKey(go) ? aiAgent._ooiCurrentlyVisible[go].ToString() : "N/A",
                                aiAgent._ooiCurrentlyFocused.ContainsKey(go) ? aiAgent._ooiCurrentlyFocused[go].ToString() : "N/A",
                                aiAgent._passiveOOITimers.ContainsKey(go) ? aiAgent._passiveOOITimers[go].ToString() : "N/A",
                                aiAgent._activeOOITimers.ContainsKey(go) ? aiAgent._activeOOITimers[go].ToString() : "N/A",
                                aiAgent._lastKnownOOIPosition.ContainsKey(go) ? aiAgent._lastKnownOOIPosition[go].ToString() : "N/A"));
        }
    }

    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.X)) _fovMesh._focusedFOV = !_fovMesh._focusedFOV;
        _fovMesh.DrawFieldOfView();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, sightRadius);

        Gizmos.color = new Color(1, 0.92f, 0.016f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, sightRadius * sightFocusedModifier);
    }
}
