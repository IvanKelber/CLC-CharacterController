using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityPlane : GravitySource
{
    [SerializeField]
	float gravity = 9.81f;

    [SerializeField]
    float minRange = 1f;

    [SerializeField]
    float gizmoScale = 1;

	public override Vector3 GetGravity (Vector3 position) {
		Vector3 up = transform.up;
        float distanceFromPlane = Vector3.Dot(up, position - transform.position);
        if(distanceFromPlane > minRange) {
            return Vector3.zero;
        }
        float g = -gravity;
        if(distanceFromPlane > 0f) {
            g *= 1 - (distanceFromPlane / minRange);
        }
		return g * up;
	}
    
    void OnDrawGizmos () {
        Vector3 scale = transform.localScale;
		scale.y = minRange;
		Gizmos.matrix =
			Matrix4x4.TRS(transform.position, transform.rotation, scale);
		Vector3 size = new Vector3(1, 0f, 1);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(Vector3.zero, size);

        if (minRange > 0f) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireCube(Vector3.up, size);
		}
		
	}
}
