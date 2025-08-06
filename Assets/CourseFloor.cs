using UnityEngine;

[RequireComponent(typeof(Reflectivity))]
public class CourseFloor : MonoBehaviour
{
    Vector3 floorPosition;
    float floorWidth; // Right is in +x direction
    float floorDepth; // Forward is in +z direction

    void Start()
    {
        this.name = "Course Floor";

        floorWidth = environmentData.course3Width / 10f; // Convert meters to Unity plane scale
        floorDepth = environmentData.course3Depth / 10f;
        floorPosition = new Vector3(environmentData.course3Width / 2f, 0f, environmentData.course3Depth / 2f);
        this.transform.position = floorPosition;

        // Define plane
        GameObject courseFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        courseFloor.name = "Course Floor";
        courseFloor.transform.SetParent(this.transform);
        courseFloor.transform.localPosition = Vector3.zero;
        courseFloor.transform.localScale = new Vector3(floorWidth, 1, floorDepth);

        // Add Reflectivity
        Reflectivity reflect = courseFloor.AddComponent<Reflectivity>();
        reflect.reflectivity = 20f;

        // Add Rigidbody (set up as a static body)
        Rigidbody rb = courseFloor.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true; // So it doesn't move, but still participates in collision

        // Optional: freeze all constraints explicitly
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    void Update()
    {
        // No logic here currently
    }
}
