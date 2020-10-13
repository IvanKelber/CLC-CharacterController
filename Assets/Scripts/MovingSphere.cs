using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 [RequireComponent(typeof(Rigidbody))]
public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0,100)]
    float maxSpeed;

    [SerializeField, Range(0,100)]
    float maxAcceleration;

    [SerializeField, Range(0, 100)]
    float maxAirAcceleration;


    [SerializeField, Range(0,3)]
    int airJumps;

    [SerializeField, Range(0,10)]
    float jumpHeight;

    [SerializeField, Range(0,90)]
    float maxGroundAngle, maxStairAngle;

    [SerializeField, Range(0f, 100f)]
	float maxSnapSpeed;

    [SerializeField, Min(0f)]
	float probeDistance;

	[SerializeField]
	LayerMask probeMask, stairsMask;

    [SerializeField]
    Transform playerInputSpace = default;

    Vector3 upAxis, rightAxis, forwardAxis;

    Vector2 playerInput;

    Vector3 velocity = Vector3.zero;
    Vector3 desiredVelocity;

    bool desiredJump;
    int jumpPhase;
    int stepsSinceLastGrounded, stepsSinceLastJump;
    int groundContactCount, steepContactCount;
    Vector3 contactNormal, steepNormal;
    bool OnGround => groundContactCount > 0;
    bool OnSteep => steepContactCount > 0;
    Rigidbody body;

    float minGroundDotProduct;
    float minStairDotProduct;

    private void OnValidate() {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minStairDotProduct = Mathf.Cos(maxStairAngle * Mathf.Deg2Rad);
    }
    private float GetMinDot(int layer) {
        return (stairsMask & (1 << layer)) == 0 ?
            minGroundDotProduct : minStairDotProduct; 
    }

    private void Awake() {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        OnValidate();
    }

    private void Update() {
        // Retrieve and clamp player input
        playerInput = new Vector2(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical"));
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        //Determine what the desired velocity is
        if(playerInputSpace) {

            rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);

        } else {
            rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);
        }
        desiredVelocity = new Vector3(playerInput.x, 0, playerInput.y) * maxSpeed;

        desiredJump |= Input.GetButtonDown("Jump");
    } 
    private void FixedUpdate() {
		Vector3 gravity = CustomGravity.GetGravity(body.position, out upAxis);
        UpdateState();
        AdjustVelocity();

        if(desiredJump) {
            desiredJump = false;
            Jump(gravity);
        }
        velocity += gravity * Time.deltaTime;

        body.velocity = velocity;
        ClearState();
    }

    private void UpdateState() {
        velocity = body.velocity;
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump++;
        if(OnGround || SnapToGround() || CheckSteepContacts()) {
            stepsSinceLastGrounded = 0;
            if(stepsSinceLastJump > 1) {
                jumpPhase = 0;
            }
            if(groundContactCount > 1) 
                contactNormal.Normalize();
        } else {
            contactNormal = upAxis;
        }
    }

    private void ClearState() {
        groundContactCount = 0;
        contactNormal = Vector3.zero;
        steepContactCount = 0;
        steepNormal = Vector3.zero;
    }

	private Vector3 ProjectOnContactPlane (Vector3 vector) {
		return vector - contactNormal * Vector3.Dot(vector, contactNormal);
	}

	private Vector3 ProjectDirectionOnPlane (Vector3 direction, Vector3 normal) {
		return (direction - normal * Vector3.Dot(direction, normal)).normalized;
	}

    private void AdjustVelocity() {
        Vector3 xAxis = ProjectDirectionOnPlane(rightAxis, contactNormal).normalized;
		Vector3 zAxis = ProjectDirectionOnPlane(forwardAxis, contactNormal).normalized;

		float currentX = Vector3.Dot(velocity, xAxis);
		float currentZ = Vector3.Dot(velocity, zAxis);


        float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
		float maxSpeedChange = acceleration * Time.deltaTime;

		float newX =
			Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
		float newZ =
			Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);
        // Debug.DrawRay(transform.localPosition, desiredVelocity, Color.black);
        // Debug.DrawRay(transform.localPosition, velocity, Color.yellow);
		velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
        
        // velocity += desiredVelocity.normalized * maxSpeedChange;
        // velocity = Vector3.ClampMagnitude(velocity, desiredVelocity.magnitude);
        // Debug.DrawRay(transform.localPosition, velocity, Color.blue);

    }

    private void Jump(Vector3 gravity) {
        Vector3 jumpDirection;
        if(OnGround) {
            jumpDirection = contactNormal;
        } else if(OnSteep) {
            jumpPhase = 0;
            jumpDirection = steepNormal;
        } else if(airJumps > 0 && jumpPhase <= airJumps) {
            if(jumpPhase == 0) {
                jumpPhase = 1;
            }
            jumpDirection = contactNormal;
        } else {
            return;
        }
        stepsSinceLastJump = 0;
        jumpPhase += 1;
        float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * jumpHeight);
        jumpDirection = (jumpDirection + upAxis).normalized;
        float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
        if(alignedSpeed > 0) {
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0);
        }
        velocity += jumpDirection * jumpSpeed;
    
    }

    // Determines whether or not to snap to ground and performs the snap
    private bool SnapToGround() {
        if(stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2) {
            return false;
        }
        float speed = velocity.magnitude;
		if (speed > maxSnapSpeed) {
			return false;
		}
        if (!Physics.Raycast(body.position, -upAxis, out RaycastHit hit, probeDistance, probeMask)) {
			return false;
		}
        float upDot = Vector3.Dot(upAxis, hit.normal);
        if(upDot < GetMinDot(hit.collider.gameObject.layer)) {
            return false;
        }
        // We have just lost contact with the ground but are still above ground
        groundContactCount = 1;
        contactNormal = hit.normal;
		float dot = Vector3.Dot(velocity, hit.normal);
        if(dot > 0) {
		    velocity = (velocity - hit.normal * dot).normalized * speed;
        }
        return true;
    }

    bool CheckSteepContacts () {
		if (steepContactCount > 1) {
			steepNormal.Normalize();
            float upDot = Vector3.Dot(upAxis, steepNormal);
			if (upDot >= minGroundDotProduct) {
				groundContactCount = 1;
				contactNormal = steepNormal;
				return true;
			}
		}
		return false;
	}

    private void OnCollisionEnter(Collision collision) {
        EvaluateCollision(collision);
    }
    private void OnCollisionStay(Collision collision) {
        EvaluateCollision(collision);
    }

    private void EvaluateCollision(Collision collision) {
        float minDot = GetMinDot(collision.gameObject.layer);
        for(int i = 0; i < collision.contactCount; i++) {
            Vector3 normal = collision.GetContact(i).normal;
            float upDot = Vector3.Dot(upAxis, normal);
            if(upDot >= minDot) {
                groundContactCount++;
                contactNormal += normal;
            }
            else if (normal.y > -0.01f) {
                // Perfectly vertical wall normal has y component of 0
				steepContactCount += 1;
				steepNormal += normal;
			}
        }
    }

    void OnDrawGizmos() {
        // Gizmos.DrawRay(playerInputSpace.forward)
    }
}
