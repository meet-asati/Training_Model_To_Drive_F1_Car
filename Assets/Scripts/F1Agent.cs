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
        // Extract the AI's choices. Continuous actions are numbers between -1.0 and 1.0.
        float steeringAction = actions.ContinuousActions[0]; // -1 is hard left, 1 is hard right
        float gasAction = actions.ContinuousActions[1];      // -1 is reverse/brake, 1 is full gas

        // Apply physical movement
        Vector3 forwardMove = transform.forward * gasAction * moveSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + forwardMove);

        float turnAmount = steeringAction * turnSpeed * Time.deltaTime;
        transform.Rotate(0f, turnAmount, 0f);

        // The Reward Function: Give a tiny piece of candy for moving forward fast
        // We use dot product to ensure it's moving forward, not sliding sideways
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (forwardSpeed > 0)
        {
            AddReward(forwardSpeed * 0.01f);
        }
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