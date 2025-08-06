using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ICM40609D_IMU : MonoBehaviour
{
    private float imuDt;
    private float previousRealTime;
    private Rigidbody imu;

    [Header("IMU Settings")]
    public float imuUpdateRate; // Hz
    public int upSamplingFactor; // How many times to sample the IMU per physics step

    [Header("Kinematics")]
    public Vector3 currentAcceleration;
    public Vector3 currentAngularRate;

    private Vector3 currentVelocity;
    private Vector3 lastVelocity;

    void Start()
    {
        imu = GetComponent<Rigidbody>();
        imu.useGravity = true;

        imuUpdateRate = 200;
        upSamplingFactor = Mathf.FloorToInt(imuUpdateRate / environmentData.simRate);
        imuDt = 1f / imuUpdateRate;
        Debug.Log($"IMU Update Rate: {imuUpdateRate} Hz, dt: {imuDt:F5}s");

        lastVelocity = imu.velocity;
        previousRealTime = Time.realtimeSinceStartup;
        StartCoroutine(IMULoop());
    }

    IEnumerator IMULoop()
    {
        // Stuff outside this while loop gets executed once at the start of the coroutine
        while (true)
        {
            // Stuff in this while loop gets executed every frame

            // 1. Get time change since last physics step
            float currentRealTime = Time.realtimeSinceStartup;
            Debug.Log($"Current real time is: {currentRealTime:F5}s");
            float realDeltaTime = currentRealTime - previousRealTime;
            Debug.Log($"deltaTime is: {realDeltaTime:F5}s");

            // 2. Fetch current state from Rigidbody
            currentVelocity = imu.velocity;
            
            // 2. Compare to last state. Calculate acceleration and retrieve angular rates
            currentAcceleration = ((currentVelocity - lastVelocity) / Time.fixedDeltaTime) + Physics.gravity;
            currentAngularRate = imu.angularVelocity;

            // 3. Emit IMU data at the specified rate, which is > physics update rate
            float simulatedIMUTimestamp = currentRealTime;
            float t = realDeltaTime;

            for (int i = 0; i < upSamplingFactor; ++i)
            {

            }

            while (t >= imuDt)
            {
                simulatedIMUTimestamp += realDeltaTime / upSamplingFactor;
                // TODO: Apply noise / apply and update random walk
                Debug.Log($"[IMU @ {simulatedIMUTimestamp:F5}s] Accel: {currentAcceleration:F3}, Gyro: {currentAngularRate:F3}");
                t -= imuDt;
            }

            // 4. Store current state as last state for next iteration
            lastVelocity = currentVelocity;
            previousRealTime = currentRealTime;
            yield return null;
        }
    }

    void FixedUpdate()
    {
        imu.AddForce(-Physics.gravity, ForceMode.Acceleration);
    }


}
