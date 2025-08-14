using System.Collections;
using UnityEngine;
using System.IO;
using System.Net.Sockets;
using System.Net;

[RequireComponent(typeof(Rigidbody))]
public class ICM40609D_IMU : MonoBehaviour
{
    private float currentSimTime;
    private Rigidbody imu;

    [Header("IMU Settings")]
    public float imuUpdateRate = 200f;
    public float imuDt;
    public int upSamplingFactor;

    [Header("Kinematics")]
    public Vector3 currentAcceleration;
    public Vector3 currentAngularRate;

    private Vector3 currentVelocity;
    private Vector3 lastVelocity;

    [Header("UDP Settings")]
    public string udpAddress = "192.168.1.150";
    public int udpPortTransmit = 56400;

    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;

    private Vector3 accelBias;
    private Vector3 gyroBias;

    private float accelNoiseStd = 0.0139f;
    private float gyroNoiseStd = 0.00111f;
    private float accelBiasWalkStd = 0.00005f;
    private float gyroBiasWalkStd = 0.00001f;

    void Start()
    {
        imu = GetComponent<Rigidbody>();
        imu.useGravity = true;

        imuDt = 1f / imuUpdateRate;
        upSamplingFactor = Mathf.Max(1, Mathf.RoundToInt(imuUpdateRate / environmentData.simRate));
        Debug.Log($"IMU Update Rate: {imuUpdateRate} Hz, dt: {imuDt:F5}s, upSamplingFactor: {upSamplingFactor}");

        lastVelocity = imu.velocity;
        accelBias = Vector3.zero;
        gyroBias = Vector3.zero;

        // Setup UDP socket
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(udpAddress), udpPortTransmit);
        udpClient = new UdpClient();
        udpClient.Connect(remoteEndPoint);

        StartCoroutine(IMULoop());
    }

    IEnumerator IMULoop()
    {
        while (true)
        {
            currentSimTime = Time.time;

            currentVelocity = imu.velocity;
            currentAcceleration = ((currentVelocity - lastVelocity) / Time.fixedDeltaTime) + Physics.gravity;
            currentAcceleration = imu.transform.InverseTransformDirection(currentAcceleration);

            currentAngularRate = imu.angularVelocity;
            currentAngularRate = imu.transform.InverseTransformDirection(currentAngularRate);

            for (int i = 0; i < upSamplingFactor; ++i)
            {
                float simulatedTimestamp = currentSimTime + (i * imuDt);

                for (int axis = 0; axis < 3; axis++)
                {
                    accelBias[axis] += Gaussian(0f, accelBiasWalkStd);
                    gyroBias[axis] += Gaussian(0f, gyroBiasWalkStd);
                }

                Vector3 noisyAccel = currentAcceleration + accelBias + new Vector3(
                    Gaussian(0f, accelNoiseStd),
                    Gaussian(0f, accelNoiseStd),
                    Gaussian(0f, accelNoiseStd));

                Vector3 noisyGyro = currentAngularRate + gyroBias + new Vector3(
                    Gaussian(0f, gyroNoiseStd),
                    Gaussian(0f, gyroNoiseStd),
                    Gaussian(0f, gyroNoiseStd));

                SendIMUPacket(simulatedTimestamp, noisyAccel, noisyGyro);
            }

            lastVelocity = currentVelocity;
            yield return null;
        }
    }

    void SendIMUPacket(float timestamp, Vector3 accel, Vector3 gyro)
    {
        byte[] buffer = new byte[28]; // 7 floats * 4 bytes

        int offset = 0;

        System.Buffer.BlockCopy(System.BitConverter.GetBytes(timestamp), 0, buffer, offset, 4); offset += 4;

        System.Buffer.BlockCopy(System.BitConverter.GetBytes(accel.x), 0, buffer, offset, 4); offset += 4;
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(accel.y), 0, buffer, offset, 4); offset += 4;
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(accel.z), 0, buffer, offset, 4); offset += 4;

        System.Buffer.BlockCopy(System.BitConverter.GetBytes(gyro.x), 0, buffer, offset, 4); offset += 4;
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(gyro.y), 0, buffer, offset, 4); offset += 4;
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(gyro.z), 0, buffer, offset, 4);

        udpClient.Send(buffer, buffer.Length);
    }

    void FixedUpdate()
    {
        imu.AddForce(-Physics.gravity, ForceMode.Acceleration);
    }

    void OnApplicationQuit()
    {
        udpClient?.Close();
    }

    private float Gaussian(float mean, float stdDev)
    {
        float u1 = 1.0f - Random.value;
        float u2 = 1.0f - Random.value;
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
                              Mathf.Sin(2.0f * Mathf.PI * u2);
        return mean + stdDev * randStdNormal;
    }
}
