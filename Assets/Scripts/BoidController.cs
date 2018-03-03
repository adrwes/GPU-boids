using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(BoxCollider))]
public class BoidController : MonoBehaviour
{
    public int BoidsCount = 2048;
    public float AlignmentForceFactor;
    public float CohesionForceFactor;
    public float SeparationForceFactor;
    public float FoodForceFactor = 1.0f;
    public float SpeedForceFactor = 1;
    public float BoundsForceFactor;
    public float DragCoefficient = -0.05f;
    public float ForceFieldFallofExponent = 1;
    public float AlignmentDistance = 3.0f;
    public float CohesionDistance = 3.0f;
    public float SeparationDistance = 2.0f;
    public float FoodDistance = 20f;
    public float BoundsDistance = 2.0f;
    public float SpawnRadius;
    public float SpawnVelocity;
    public float MinSpeed;
    public float MaxSpeed;
    public ComputeShader BoidCalculation;
    public Mesh BoidMesh;
    public Material BoidMaterial;
    
    BoxCollider simulationBounds;
    int updateBoidkernelIndex;
    ComputeBuffer boidsBuffer;
    ComputeBuffer argsBuffer;
    ComputeBuffer forceFieldBuffer;
    ComputeBuffer foodsBuffer;

    //stride is size of float members in bytes of buffer elements
    const int BoidStride = sizeof(float) * 3 * 3; 
    const int ForceFieldStride = sizeof(float) * (3 + 1);
    const int FoodStride = sizeof(float) * 3;
    const int ThreadGroupSize = 1024;
        
    void Start ()
    {
        simulationBounds = GetComponent<BoxCollider>();

        updateBoidkernelIndex = BoidCalculation.FindKernel("UpdateBoid");
        
        InitAndBindBoidBuffer();
        InitAndBindFoodsBuffer();
        InitAndBindForceFieldsBuffer();
        InitAndBindArgsBuffer();
        InitAndBindFloats();
    }
    
    void InitAndBindBoidBuffer()
    {
        boidsBuffer = new ComputeBuffer(BoidsCount, BoidStride);
        boidsBuffer.SetData(GetRandomBoids(BoidsCount).ToArray());
        BoidCalculation.SetBuffer(updateBoidkernelIndex, "boids", boidsBuffer);
    }

    IEnumerable<Boid> GetRandomBoids(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new Boid
            {
                position = simulationBounds.center + Random.insideUnitSphere *
                           Mathf.Clamp(SpawnRadius, 0, simulationBounds.size.ToArray().Max()),
                velocity = Random.insideUnitSphere * SpawnVelocity,
                acceleration = Vector3.zero
            };
        }
    }

    void InitAndBindFoodsBuffer()
    {
        if(foodsBuffer != null)
            foodsBuffer.Release();

        var foods = GameObject.FindGameObjectsWithTag("Food").Select(g => g.transform.position).ToArray();
        foodsBuffer = new ComputeBuffer(new [] {foods.Length, 1}.Max(), FoodStride);
        foodsBuffer.SetData(foods);
        BoidCalculation.SetBuffer(updateBoidkernelIndex, "foods", foodsBuffer);
    }

    void InitAndBindForceFieldsBuffer()
    {
        var forceFields = GameObject.FindGameObjectsWithTag("ForceField").Select(g => new Field { position = g.transform.position, force = g.GetComponent<ForceField>().Force }).ToArray();
        forceFieldBuffer = new ComputeBuffer(forceFields.Length, ForceFieldStride);
        forceFieldBuffer.SetData(forceFields);
        BoidCalculation.SetBuffer(updateBoidkernelIndex, "forceFields", forceFieldBuffer);
    }

    void InitAndBindArgsBuffer()
    {
        var args = new uint[] { BoidMesh.GetIndexCount(0), (uint)BoidsCount, 0, 0, 0 };
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
    }

    void InitAndBindFloats()
    {
        BoidCalculation.SetFloat("alignmentForceFactor", AlignmentForceFactor);
        BoidCalculation.SetFloat("cohesionForceFactor", CohesionForceFactor);
        BoidCalculation.SetFloat("separationForceFactor", SeparationForceFactor);
        BoidCalculation.SetFloat("foodForceFactor", FoodForceFactor);
        BoidCalculation.SetFloat("speedForceFactor", SpeedForceFactor);
        BoidCalculation.SetFloat("boundsForceFactor", BoundsForceFactor);
        BoidCalculation.SetFloat("dragCoefficient", DragCoefficient);
        BoidCalculation.SetFloat("forceFieldFallofExponent", ForceFieldFallofExponent);
        BoidCalculation.SetFloat("alignmentDistance", AlignmentDistance);
        BoidCalculation.SetFloat("cohesionDistance", CohesionDistance);
        BoidCalculation.SetFloat("separationDistance", SeparationDistance);
        BoidCalculation.SetFloat("foodDistance", FoodDistance);
        BoidCalculation.SetFloat("boundsDistance", BoundsDistance);
        BoidCalculation.SetFloats("simulationCenter", simulationBounds.center.ToArray());
        BoidCalculation.SetFloats("simulationSize", simulationBounds.size.ToArray());
        BoidCalculation.SetFloat("maxSpeed", MaxSpeed);
        BoidCalculation.SetFloat("minSpeed", MinSpeed);
    }
	
	void Update ()
    {
        BoidCalculation.SetFloat("deltaTime", Time.deltaTime);

        InitAndBindFoodsBuffer();

        int threadGroupsCount = Mathf.CeilToInt((float) BoidsCount / ThreadGroupSize);
        BoidCalculation.Dispatch(updateBoidkernelIndex, threadGroupsCount, 1, 1);
        
        BoidMaterial.SetBuffer("boids", boidsBuffer);

        //render
        Graphics.DrawMeshInstancedIndirect(BoidMesh, 0, BoidMaterial, new Bounds(Vector3.zero, Vector3.one * 1.1f * simulationBounds.size.x), argsBuffer);
    }

    void OnDestroy()
    {
        boidsBuffer.Release();
        foodsBuffer.Release();
        forceFieldBuffer.Release();
        argsBuffer.Release();
    }

    struct Boid
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
    }

    struct Field
    {
        public Vector3 position;
        public float force;
    }
}
