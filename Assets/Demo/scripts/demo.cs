using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class demo : MonoBehaviour
{
    public int mLightCount = 64;

    public static demo Instance
    {
        get;
        private set;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public Transform mBlockerPrefab;
    public Transform mLightPrefab;

    public Vector3 RandomPosition()
    {
        float range = 6.0f;
        return new Vector3(
            Random.Range(-range, +range),
            Random.Range(-range, +range),
            Random.Range(0.0f, 0.0f));
    }

    // Use this for initialization
    void Start()
    {
        //
        // Create some light blockers - rectangles which will slowly spin around and obstruct light
        //

        int blockercount = 50;

        for (int i = 0; i < blockercount; i++)
        {
            Transform blocker = GameObject.Instantiate(mBlockerPrefab, transform);

            blocker.position = RandomPosition();

            blocker.localEulerAngles =
                new Vector3(
                    0.0f,
                    0.0f,
                    Random.Range(0, 360.0f));

            blocker.localScale =
                new Vector3(
                Random.Range(0.25f, 1.0f),
                Random.Range(0.25f, 1.0f), 1.0f);
        }

        //
        // Create some lights - like torches, they will have a cone shaped beam.
        //

        int lightCount = Mathf.Min(64, mLightCount);

        for (int i = 0; i < lightCount; i++)
        {
            Transform light = GameObject.Instantiate(mLightPrefab, transform);

            light.position = RandomPosition();
        }
    }

    // Update is called once per frame
    void Update ()
    {
        GetLightBlockerMesh();
    }

    // Create a mesh containing all the light blocker edges
    public Mesh GetLightBlockerMesh()
    {
        demoblocker[] blockers = GetComponentsInChildren<demoblocker>();

        List<Vector2> edges = new List<Vector2>();
        foreach (demoblocker b in blockers)
        {
            b.GetEdges(edges);
        }

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> normals = new List<Vector2>();
        for (int i = 0; i < edges.Count; i += 2)
        {
            verts.Add(edges[i + 0]);
            verts.Add(edges[i + 1]);
            normals.Add(edges[i + 1]);
            normals.Add(edges[i + 0]);
        }

        // Simple 1:1 index buffer
        int[] incides = new int[edges.Count];
        for (int i = 0; i < edges.Count; i++)
        {
            incides[i] = i;
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, normals);
        mesh.SetIndices(incides, MeshTopology.Lines, 0);
        return mesh;
    }
}
