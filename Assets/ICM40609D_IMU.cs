using System.Collections;
using UnityEngine;
using System.IO; // For CSV logging

[RequireComponent(typeof(Rigidbody))]
public class ICM40609D_IMU : MonoBehaviour
{    
    private float previousRealTime;
    private float currentRealTime;
    private Rigidbody imu;

    [Header("IMU Settings")]
    public float imuUpdateRate; // ideal IMU update rate in Hz
    public float imuDt; // ideal time step between IMU updates, equal to 1 / imuUpdateRate


    [Header("Kinematics")]
    public Vector3 currentAcceleration;
    public Vector3 currentAngularRate;

    private Vector3 currentVelocity;
    private Vector3 lastVelocity;

    private StreamWriter imuWriter;
    private string imuLogPath;

    void Start()
    {
        imu = GetComponent<Rigidbody>();
        imu.useGravity = true;

        imuUpdateRate = 200;
        imuDt = 1f / imuUpdateRate;

        Debug.Log($"IMU Update Rate: {imuUpdateRate} Hz, dt: {imuDt:F5}s");

        lastVelocity = imu.velocity;
        previousRealTime = Time.realtimeSinceStartup;

        // Set up CSV logging
        imuLogPath = Path.Combine(Application.persistentDataPath, "imu_log.csv");
        imuWriter = new StreamWriter(imuLogPath, false);
        imuWriter.WriteLine("Time,AccelX,AccelY,AccelZ,GyroX,GyroY,GyroZ");

        StartCoroutine(IMULoop());
    }

    IEnumerator IMULoop()
    {
        while (true) //TODO: ARE WE USING REAL TIME OR SIM TIME FOR TIMESTAMPS?
        {
            currentRealTime = Time.realtimeSinceStartup;

            currentVelocity = imu.velocity;
            currentAcceleration = ((currentVelocity - lastVelocity) / Time.fixedDeltaTime) + Physics.gravity;
            currentAcceleration = imu.transform.InverseTransformDirection(currentAcceleration);

            currentAngularRate = imu.angularVelocity;
            currentAngularRate = imu.transform.InverseTransformDirection(currentAngularRate);

            float simulatedIMUTimestamp = previousRealTime;
            while (simulatedIMUTimestamp < currentRealTime)
            {
                imuWriter.WriteLine($"{simulatedIMUTimestamp:F6}," +
                                    $"{currentAcceleration.x:F6},{currentAcceleration.y:F6},{currentAcceleration.z:F6}," +
                                    $"{currentAngularRate.x:F6},{currentAngularRate.y:F6},{currentAngularRate.z:F6}");

                simulatedIMUTimestamp += imuDt;
            }

            lastVelocity = currentVelocity;
            previousRealTime = currentRealTime;
            yield return null;
        }
    }

    void FixedUpdate()
    {
        imu.AddForce(-Physics.gravity, ForceMode.Acceleration);
    }

    void OnApplicationQuit()
    {
        if (imuWriter != null)
        {
            imuWriter.Flush();
            imuWriter.Close();
            Debug.Log($"IMU data saved to {imuLogPath}");
        }
    }
}
