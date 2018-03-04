using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(BoxCollider))]
public class BoidController : MonoBehaviour
{
    public int BoidsCount = 2048;
    public int PredatorCount = 16;

    public float AlignmentForceFactor;
    public float CohesionForceFactor;
    public float SeparationForceFactor;
    public float FleeForceFactor = 3.0f;
    public float FoodForceFactor = 1.0f;
    public float SpeedForceFactor = 1;
    public float BoundsForceFactor;
    public float DragCoefficient = -0.05f;
    public float ForceFieldFallofExponent = 1;

    public float AlignmentDistance = 3.0f;
    public float CohesionDistance = 3.0f;
    public float SeparationDistance = 2.0f;
    public float FleeDistance = 20.0f;
    public float FoodDistance = 20f;
    public float BoundsDistance = 2.0f;
    public float SpawnRadius;
    public float SpawnVelocity;
    public float MinSpeed;
    public float MaxSpeed;
    public ComputeShader BoidCalculation;
    public Mesh BoidMesh;
    public Material BoidMaterial;
    public Material PredatorMaterial;
    
    BoxCollider simulationBounds;
    int updateBoidkernel;
    int updatePredatorKernel;
    ComputeBuffer boidsBuffer;
    ComputeBuffer predatorBuffer;
    ComputeBuffer argsBuffer1;
    ComputeBuffer argsBuffer2;
    ComputeBuffer forceFieldBuffer;
    ComputeBuffer foodsBuffer;

    //stride is size of float members in bytes of buffer elements
    const int BoidStride = sizeof(float) * 12; 
    const int ForceFieldStride = sizeof(float) * (3 + 1);
    const int FoodStride = sizeof(float) * 3;
    const int ThreadGroupSize = 1024;
        
    void Start ()
    {
        if(BoidsCount < ThreadGroupSize || PredatorCount < 16)
            throw new Exception("Number of boids or predators cant be less than the thread group size");

        simulationBounds = GetComponent<BoxCollider>();

        updateBoidkernel = BoidCalculation.FindKernel("UpdateBoid");
        updatePredatorKernel = BoidCalculation.FindKernel("UpdatePredator");
        
        InitAndBindBoidBuffer();
        InitAndBindPredatorBuffer();
        InitAndBindFoodsBuffer();
        InitAndBindForceFieldsBuffer();
        InitArgsBuffer1();
        InitArgsBuffer2();
        InitAndBindFloats();
    }
    
    void InitAndBindBoidBuffer()
    {
        boidsBuffer = new ComputeBuffer(BoidsCount, BoidStride);
        boidsBuffer.SetData(GetRandomBoids(BoidsCount).ToArray());
        BoidCalculation.SetBuffer(updateBoidkernel, "boids", boidsBuffer);
        BoidCalculation.SetBuffer(updatePredatorKernel, "boids", boidsBuffer);
    }

    void InitAndBindPredatorBuffer()
    {
        predatorBuffer = new ComputeBuffer(PredatorCount, BoidStride);
        predatorBuffer.SetData(GetRandomBoids(PredatorCount).ToArray());
        BoidCalculation.SetBuffer(updateBoidkernel, "predators", predatorBuffer);
        BoidCalculation.SetBuffer(updatePredatorKernel, "predators", predatorBuffer);
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
                acceleration = Vector3.zero,
                mass = Random.Range(1f, 2f),
                type = i < 10u ? 0u : 1u
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
        BoidCalculation.SetBuffer(updateBoidkernel, "foods", foodsBuffer);
    }

    void InitAndBindForceFieldsBuffer()
    {
        var forceFields = GameObject.FindGameObjectsWithTag("ForceField").Select(g => new Field { position = g.transform.position, force = g.GetComponent<ForceField>().Force }).ToArray();
        forceFieldBuffer = new ComputeBuffer(forceFields.Length, ForceFieldStride);
        forceFieldBuffer.SetData(forceFields);
        BoidCalculation.SetBuffer(updateBoidkernel, "forceFields", forceFieldBuffer);
        BoidCalculation.SetBuffer(updatePredatorKernel, "forceFields", forceFieldBuffer);
    }

    void InitArgsBuffer1()
    {
        var args = new uint[] { BoidMesh.GetIndexCount(0), (uint)BoidsCount, 0, 0, 0 };
        argsBuffer1 = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer1.SetData(args);
    }

    void InitArgsBuffer2()
    {
        var args = new uint[] { BoidMesh.GetIndexCount(0), (uint)PredatorCount, 0, 0, 0 };
        argsBuffer2 = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer2.SetData(args);
    }

    void InitAndBindFloats()
    {
        BoidCalculation.SetFloat("alignmentForceFactor", AlignmentForceFactor);
        BoidCalculation.SetFloat("cohesionForceFactor", CohesionForceFactor);
        BoidCalculation.SetFloat("separationForceFactor", SeparationForceFactor);
        BoidCalculation.SetFloat("fleeForceFactor", FleeForceFactor);
        BoidCalculation.SetFloat("foodForceFactor", FoodForceFactor);
        BoidCalculation.SetFloat("speedForceFactor", SpeedForceFactor);
        BoidCalculation.SetFloat("boundsForceFactor", BoundsForceFactor);
        BoidCalculation.SetFloat("dragCoefficient", DragCoefficient);
        BoidCalculation.SetFloat("forceFieldFallofExponent", ForceFieldFallofExponent);

        BoidCalculation.SetFloat("alignmentDistance", AlignmentDistance);
        BoidCalculation.SetFloat("cohesionDistance", CohesionDistance);
        BoidCalculation.SetFloat("separationDistance", SeparationDistance);
        BoidCalculation.SetFloat("fleeDistance", FleeDistance);
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
        BoidCalculation.Dispatch(updateBoidkernel, threadGroupsCount, 1, 1);
        
        BoidMaterial.SetBuffer("boids", boidsBuffer);

        var bounds = new Bounds{center = transform.position, size = simulationBounds.size * 1.1f };

        //render
        Graphics.DrawMeshInstancedIndirect(BoidMesh, 0, BoidMaterial, bounds, argsBuffer1);

        
        threadGroupsCount = Mathf.CeilToInt((float) PredatorCount / 16);
        BoidCalculation.Dispatch(updatePredatorKernel, threadGroupsCount, 1, 1);

        PredatorMaterial.SetBuffer("boids", predatorBuffer);
        
        //render
        Graphics.DrawMeshInstancedIndirect(BoidMesh, 0, PredatorMaterial, bounds, argsBuffer2);
    }

    void OnDestroy()
    {
        boidsBuffer.Release();
        predatorBuffer.Release();
        foodsBuffer.Release();
        forceFieldBuffer.Release();
        argsBuffer1.Release();
        argsBuffer2.Release();
    }

    struct Boid
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
        public float mass;
        public uint type;
        float padding; // For some reason it doesnt work without the padding in the struct(s) (and stride) TODO: try again
    }

    struct Field
    {
        public Vector3 position;
        public float force;
    }
}
