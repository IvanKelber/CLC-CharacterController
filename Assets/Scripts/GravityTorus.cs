using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityTorus : GravitySource
{
   	[SerializeField]
	float gravity = 9.81f;

    [SerializeField, Min(0f)]
	float torusInnerRadius = 25f;

	[SerializeField, Min(0f)]
	float torusOuterRadius = 50f;

    float gravityRadius;
		
    float innerFalloffFactor, outerFalloffFactor;
	void Awake () {
		OnValidate();
        gravityRadius = (torusInnerRadius + torusOuterRadius)/2;
	}

	void OnValidate () {
		// torusInnerRadius = Mathf.Max(torusInnerRadius, innerFalloffRadius);
		torusOuterRadius = Mathf.Max(torusOuterRadius, torusInnerRadius);
	}

    public override Vector3 GetGravity (Vector3 position) {
        Vector3 up = transform.up;
        float distanceFromPlane = Vector3.Dot(up, position - transform.position);

        Vector3 nearestPointOnPlane = position - up*distanceFromPlane;
        Vector3 gravityPoint = transform.position + (nearestPointOnPlane - transform.position).normalized * gravityRadius;


		Vector3 vector = gravityPoint - position;
		float distance = vector.magnitude;
		// if (distance > outerFalloffRadius || distance < innerFalloffRadius) {
		// 	return Vector3.zero;
		// } 

        float g = gravity / distance;

        if(distance > torusOuterRadius) {
            g *= 1 - (distance - torusOuterRadius) * outerFalloffFactor;
        }
        // } else if(distance < innerRadius) {
        //     g *= 1 - (innerRadius - distance) * innerFalloffFactor;
        // }

		return g * vector;
	}

	void OnDrawGizmos () {
        Vector3 p = transform.position;
		Gizmos.color = Color.yellow;
		if (torusInnerRadius > 0f && torusInnerRadius < torusOuterRadius) {
			Gizmos.DrawWireSphere(p, torusInnerRadius);
		}
		Gizmos.DrawWireSphere(p, torusOuterRadius);
		Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(p, gravityRadius);
        for(int i = 0; i < 360; i++) {
            Gizmos.DrawRay(transform.position, 
            new Vector3(Mathf.Cos(Mathf.Deg2Rad * i), transform.position.y, Mathf.Sin(Mathf.Deg2Rad * i)) * gravityRadius);
        }
	}
}
