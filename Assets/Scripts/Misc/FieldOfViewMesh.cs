using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfViewMesh : MonoBehaviour
{
    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float distance;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _distance, float _angle)
        {
            hit = _hit;
            point = _point;
            distance = _distance;
            angle = _angle;
        }
    }

    public struct EdgeInfo
    {
        public Vector3 pointMin;
        public Vector3 pointMax;

        public EdgeInfo(Vector3 _min, Vector3 _max)
        {
            pointMin = _min;
            pointMax = _max;
        }
    }

    internal AgentPerception _agentPerception;
    internal bool _focusedFOV;
    internal MeshFilter _viewMeshFilter;
    internal float _viewFocusSpeed = 1;
    internal int _edgeResolveIterations;
    internal float _edgeDistanceThreshold;

    Mesh _viewMesh;
    bool _inFocusFOVMode;
    float _viewAngle;
    float _viewFocusTimer = 0f;
    int _viewLayerMask;

    void Start()
    {
        _inFocusFOVMode = _focusedFOV;

        _viewMesh = new Mesh();
        _viewMesh.name = "View Mesh";
        _viewMeshFilter.mesh = _viewMesh;

        _viewLayerMask = (1 << LayerMask.NameToLayer("Walls"));
    }

    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = new Vector3(
                    Mathf.Sin(globalAngle * Mathf.Deg2Rad),
                    0,
                    Mathf.Cos(globalAngle * Mathf.Deg2Rad));
        RaycastHit hit;

        float tempSightRadius = (_focusedFOV != _inFocusFOVMode ? 
            Mathf.Lerp(_agentPerception.sightRadius * (_focusedFOV ? 1f : _agentPerception.sightFocusedModifier), _agentPerception.sightRadius * (_focusedFOV ? _agentPerception.sightFocusedModifier : 1f), Mathf.SmoothStep(0f, 1f, _viewFocusTimer)) : 
            _agentPerception.sightRadius * (_focusedFOV ? _agentPerception.sightFocusedModifier : 1f));

        if (Physics.Raycast(transform.position, dir, out hit, tempSightRadius, _viewLayerMask))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else
        {
            return new ViewCastInfo(false, transform.position + dir * tempSightRadius, tempSightRadius, globalAngle);
        }
    }

    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;

        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < _edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);

            bool tempEdgeThresholdExceeded = (Mathf.Abs(minViewCast.distance - newViewCast.distance) > _edgeDistanceThreshold);
            if (newViewCast.hit == minViewCast.hit && !tempEdgeThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo(minPoint, maxPoint);
    }

    public void DrawFieldOfView()
    {
        if (_viewFocusTimer >= 1f)
        {
            _inFocusFOVMode = _focusedFOV;
            _viewFocusTimer = 0f;
        }        
        if (_focusedFOV != _inFocusFOVMode && _viewFocusTimer < 1) _viewFocusTimer += Time.deltaTime / _viewFocusSpeed;

        _viewAngle = (_focusedFOV != _inFocusFOVMode ? 
            Mathf.Lerp(2 * (_focusedFOV ? _agentPerception.sightVisibleAngle : _agentPerception.sightFocusedAngle), 2 * (_focusedFOV ? _agentPerception.sightFocusedAngle : _agentPerception.sightVisibleAngle), Mathf.SmoothStep(0f, 1f, _viewFocusTimer)) : 
            (2 * (_focusedFOV ? _agentPerception.sightFocusedAngle : _agentPerception.sightVisibleAngle)));
        
        int stepCount = Mathf.RoundToInt(_viewAngle * _agentPerception.FOVMeshResolution);
        float stepAngleSize = _viewAngle / stepCount;

        List<Vector3> viewPoints = new List<Vector3>();

        ViewCastInfo oldViewCast = new ViewCastInfo();
        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - _viewAngle / 2 + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(angle);

            if (i > 0)
            {
                bool tempEdgeThresholdExceeded = (Mathf.Abs(oldViewCast.distance - newViewCast.distance) > _edgeDistanceThreshold);
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && tempEdgeThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if (edge.pointMin != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointMin);
                    }

                    if (edge.pointMax != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointMax);
                    }
                }
            }

            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        _viewMesh.Clear();
        _viewMesh.vertices = vertices;
        _viewMesh.triangles = triangles;
        _viewMesh.RecalculateNormals();
    }
}
