using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// This is a torch-like item which emits light.
//

public class demolight : GameLight
{
    public Vector3 mVel;

    public Vector3 GetRandomVel()
    {
        Vector3 vel = transform.position.normalized;
        vel.x = vel.x * Random.Range(0.5f, 1.0f);
        vel.y = vel.y * Random.Range(0.5f, 1.0f);
        vel.z = 0.0f;
        return vel;
    }

	// Use this for initialization
	void Start ()
    {
        mVel = GetRandomVel();

        Spread =  Random.Range(45.0f, 180.0f);

        Angle = Random.Range(0.0f, 360.0f);

        mColour = new Color(Random.Range(0.25f, 1.0f), Random.Range(0.25f, 1.0f), Random.Range(0.25f, 1.0f), 1.0f);

        base.Start();
    }
	
	// Update is called once per frame
	void FixedUpdate ()
    {
		if (transform.position.magnitude > 8.0f)
        {
            mVel = GetRandomVel() * -1.0f;
        }
        transform.localPosition += mVel * Time.fixedDeltaTime;

        Angle += Time.deltaTime * 360.0f * 0.1f;
    }
}
