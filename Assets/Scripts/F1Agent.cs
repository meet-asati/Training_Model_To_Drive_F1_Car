using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class F1Agent : Agent
{
    private Rigidbody rb;
    private Vector3 startingPosition;
    private Quaternion startingRotation;

    public float moveSpeed = 50f;
    public float turnSpeed = 100f;

    public Transform[] checkpoints;
    private int nextCheckpointIndex = 0;

    public float gripMultiplier = 0.95f;

    private void FixedUpdate()
    {
        Vector3 forwardVelocity = transform.forward * Vector3.Dot(rb.linearVelocity, transform.forward);

        Vector3 rightVelocity = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);

        rb.linearVelocity = forwardVelocity + rightVelocity * (1f - gripMultiplier);
    }

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        startingPosition = transform.localPosition;
        startingRotation = transform.localRotation;
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.localPosition = startingPosition;
        transform.localRotation = startingRotation;

        nextCheckpointIndex = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(rb.linearVelocity);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float steeringAction = actions.ContinuousActions[0];

        float gasAction = Mathf.Clamp(actions.ContinuousActions[1], 0f, 1f);

        rb.AddForce(transform.forward * gasAction * moveSpeed, ForceMode.Acceleration);

        float turnAmount = steeringAction * turnSpeed * Time.fixedDeltaTime;
        transform.Rotate(0f, turnAmount, 0f);

        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        if (forwardSpeed > 0)
        {
            // The AI finally gets paid for pressing the gas!
            AddReward(forwardSpeed * 0.01f);
        }

        Debug.Log(rb.linearVelocity.magnitude);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;

        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            SetReward(-1f);

            EndEpisode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == checkpoints[nextCheckpointIndex])
        {
            // Give a piece of kibble! (Small dense reward)
            AddReward(0.5f);

            // Move the target to the next checkpoint in the array
            nextCheckpointIndex++;

            // 2. Did we just pass the final checkpoint?
            if (nextCheckpointIndex >= checkpoints.Length)
            {
                // Lap complete! Give the giant steak and finish the run.
                SetReward(2.0f);
                Debug.Log("Lap Completed! Good AI!");
                EndEpisode();
            }
        }
        // 3. Did we hit a checkpoint, but it was the WRONG one?
        else if (other.CompareTag("Checkpoint"))
        {
            // The car is driving backward or trying to cheat by cutting the track!
            // Give a massive penalty and stop the run.
            SetReward(-1.0f);
            EndEpisode();
        }
    }
}