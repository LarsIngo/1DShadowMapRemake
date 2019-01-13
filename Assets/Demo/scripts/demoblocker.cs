using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// This is a rectangular object which blocks light
//

public class demoblocker : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {
	}
	
	// Update is called once per frame
	void Update ()
    {
        // Make the light blocker slowly spin around
        transform.localEulerAngles = new Vector3(0.0f, 0.0f, transform.localEulerAngles.z + Time.deltaTime * 45.0f);
	}

    // Get the outline of the object for shadow map rendering
    public void GetEdges(List<Vector2> edges)
    {
        Vector3 v1 = new Vector3(-0.5f, -0.5f, 0.0f);
        Vector3 v2 = new Vector3(+0.5f, -0.5f, 0.0f);
        Vector3 v3 = new Vector3(+0.5f, +0.5f, 0.0f);
        Vector3 v4 = new Vector3(-0.5f, +0.5f, 0.0f);

        v1 = transform.localToWorldMatrix.MultiplyPoint(v1);
        v2 = transform.localToWorldMatrix.MultiplyPoint(v2);
        v3 = transform.localToWorldMatrix.MultiplyPoint(v3);
        v4 = transform.localToWorldMatrix.MultiplyPoint(v4);

        edges.Add(new Vector2(v1.x, v1.y));
        edges.Add(new Vector2(v2.x, v2.y));

        edges.Add(new Vector2(v2.x, v2.y));
        edges.Add(new Vector2(v3.x, v3.y));

        edges.Add(new Vector2(v3.x, v3.y));
        edges.Add(new Vector2(v4.x, v4.y));

        edges.Add(new Vector2(v4.x, v4.y));
        edges.Add(new Vector2(v1.x, v1.y));
    }
}
