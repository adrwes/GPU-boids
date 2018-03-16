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
    public float PursuitForceFactor = 3.0f;
    public float FoodForceFactor = 1.0f;
    public float SpeedForceFactor = 1;
    public float BoundsForceFactor;
    public float DragCoefficient = -0.05f;
    public float ForceFieldFallofExponent = 1;
    public float PursueOffset = 1;

    public float AlignmentDistance = 3.0f;
    public float CohesionDistance = 3.0f;
    public float SeparationDistance = 2.0f;
    public float FleeDistance = 20.0f;
    public float PursuitDistance = 20.0f;
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
    const int BoidsThreadGroupSize = 1024;
    const int PredatorsThreadGroupSize = 16;
        
    void Start ()
    {
        if(BoidsCount < BoidsThreadGroupSize || PredatorCount < 16)
            throw new Exception("Number of boids or predators cant be less than the thread group sizes");

        simulationBounds = GetComponent<BoxCollider>();

        updateBoidkernel = BoidCalculation.FindKernel("UpdateBoid");
        updatePredatorKernel = BoidCalculation.FindKernel("UpdatePredator");

        boidsBuffer = InitAndBindBoidBuffer();
        predatorBuffer = InitAndBindPredatorBuffer();
        foodsBuffer = InitAndBindFoodsBuffer();
        forceFieldBuffer = InitAndBindForceFieldsBuffer();
        argsBuffer1 = InitArgsBuffer1();
        argsBuffer2 = InitArgsBuffer2();
        InitAndBindFloats();
    }

    ComputeBuffer InitAndBindBoidBuffer()
    {
        var boidsBuffer = new ComputeBuffer(BoidsCount, BoidStride);
        boidsBuffer.SetData(GetRandomBoids(BoidsCount).ToArray());
        BoidCalculation.SetBuffer(updateBoidkernel, "boids", boidsBuffer);
        BoidCalculation.SetBuffer(updatePredatorKernel, "boids", boidsBuffer);
        return boidsBuffer;
    }

    ComputeBuffer InitAndBindPredatorBuffer()
    {
        var predatorBuffer = new ComputeBuffer(PredatorCount, BoidStride);
        predatorBuffer.SetData(GetRandomBoids(PredatorCount).ToArray());
        BoidCalculation.SetBuffer(updateBoidkernel, "predators", predatorBuffer);
        BoidCalculation.SetBuffer(updatePredatorKernel, "predators", predatorBuffer);
        return predatorBuffer;
    }

    IEnumerable<Boid> GetRandomBoids(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new Boid
            {
                position = transform.position + simulationBounds.center + Random.insideUnitSphere *
                           Mathf.Clamp(SpawnRadius, 0, simulationBounds.size.ToArray().Max()),
                velocity = Random.insideUnitSphere * SpawnVelocity,
                acceleration = Vector3.zero,
                mass = Random.Range(1f, 2f)
            };
        }
    }

    ComputeBuffer InitAndBindFoodsBuffer()
    {
        var foods = GameObject.FindGameObjectsWithTag("Food").Select(g => g.transform.position).ToArray();
        var foodsBuffer = new ComputeBuffer(new [] {foods.Length, 1}.Max(), FoodStride);
        foodsBuffer.SetData(foods);
        BoidCalculation.SetBuffer(updateBoidkernel, "foods", foodsBuffer);
        return foodsBuffer;
    }

    ComputeBuffer InitAndBindForceFieldsBuffer()
    {
        var forceFields = GameObject.FindGameObjectsWithTag("ForceField").Select(g => new Field { position = g.transform.position, force = g.GetComponent<ForceField>().Force }).ToArray();
        var forceFieldBuffer = new ComputeBuffer(forceFields.Length, ForceFieldStride);
        forceFieldBuffer.SetData(forceFields);
        BoidCalculation.SetBuffer(updateBoidkernel, "forceFields", forceFieldBuffer);
        BoidCalculation.SetBuffer(updatePredatorKernel, "forceFields", forceFieldBuffer);
        return forceFieldBuffer;
    }

    ComputeBuffer InitArgsBuffer1()
    {
        var args = new uint[] { BoidMesh.GetIndexCount(0), (uint)BoidsCount, 0, 0, 0 };
        var argsBuffer1 = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer1.SetData(args);
        return argsBuffer1;
    }

    ComputeBuffer InitArgsBuffer2()
    {
        var args = new uint[] { BoidMesh.GetIndexCount(0), (uint)PredatorCount, 0, 0, 0 };
        var argsBuffer2 = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer2.SetData(args);
        return argsBuffer2;
    }

    void InitAndBindFloats()
    {
        BoidCalculation.SetFloat("alignmentForceFactor", AlignmentForceFactor);
        BoidCalculation.SetFloat("cohesionForceFactor", CohesionForceFactor);
        BoidCalculation.SetFloat("separationForceFactor", SeparationForceFactor);
        BoidCalculation.SetFloat("fleeForceFactor", FleeForceFactor);
        BoidCalculation.SetFloat("foodForceFactor", FoodForceFactor);
        BoidCalculation.SetFloat("pursuitForceFactor", PursuitForceFactor);
        BoidCalculation.SetFloat("speedForceFactor", SpeedForceFactor);
        BoidCalculation.SetFloat("boundsForceFactor", BoundsForceFactor);
        BoidCalculation.SetFloat("dragCoefficient", DragCoefficient);
        BoidCalculation.SetFloat("forceFieldFallofExponent", ForceFieldFallofExponent);
        BoidCalculation.SetFloat("pursueOffset", PursueOffset);

        BoidCalculation.SetFloat("alignmentDistance", AlignmentDistance);
        BoidCalculation.SetFloat("cohesionDistance", CohesionDistance);
        BoidCalculation.SetFloat("separationDistance", SeparationDistance);
        BoidCalculation.SetFloat("fleeDistance", FleeDistance);
        BoidCalculation.SetFloat("foodDistance", FoodDistance);
        BoidCalculation.SetFloat("pursuitDistance", PursuitDistance);
        BoidCalculation.SetFloat("boundsDistance", BoundsDistance);

        BoidCalculation.SetFloats("simulationCenter", (transform.position + simulationBounds.center).ToArray());
        BoidCalculation.SetFloats("simulationSize", simulationBounds.size.ToArray());

        BoidCalculation.SetFloat("maxSpeed", MaxSpeed);
        BoidCalculation.SetFloat("minSpeed", MinSpeed);
    }
	
	void Update ()
    {
        BoidCalculation.SetFloat("deltaTime", Time.deltaTime);

        if(foodsBuffer != null)
            foodsBuffer.Release();
        foodsBuffer = InitAndBindFoodsBuffer();

        int threadGroupsCount = Mathf.CeilToInt((float) BoidsCount / BoidsThreadGroupSize);
        BoidCalculation.Dispatch(updateBoidkernel, threadGroupsCount, 1, 1);
        
        BoidMaterial.SetBuffer("boids", boidsBuffer);

        var bounds = new Bounds{center = transform.position, size = simulationBounds.size * 1.1f };

        //render
        Graphics.DrawMeshInstancedIndirect(BoidMesh, 0, BoidMaterial, bounds, argsBuffer1);

        
        threadGroupsCount = Mathf.CeilToInt((float) PredatorCount / PredatorsThreadGroupSize);
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
