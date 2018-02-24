using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(SphereCollider))]
public class BoidController : MonoBehaviour
{
    [SerializeField] float alignmentForceFactor;
    [SerializeField] float cohesionForceFactor;
    [SerializeField] float separationForceFactor;
    [SerializeField] float boundsForceFactor;
    [SerializeField] float alignmentDistance = 3.0f;
    [SerializeField] float cohesionDistance = 3.0f;
    [SerializeField] float separationDistance = 2.0f;
    [SerializeField] float spawnRadius;
    [SerializeField] float spawnVelocity;
    [SerializeField] float minVelocity;
    [SerializeField] float maxVelocity;

    [SerializeField] ComputeShader BoidCalculation;
    [SerializeField] Mesh boidMesh;
    [SerializeField] Material boidMaterial;

    SphereCollider simulationBounds;
    int updateBoidkernelIndex;
    ComputeBuffer boidsBuffer;
    ComputeBuffer argsBuffer;
    
    const int boidsCount = 1024;
    const int boidStride = sizeof(float) * 3 * 3; //size of float members in bytes
    const int threadGroupSize = 1024;
    
    struct Boid
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
    }
    
    void Start ()
    {
        simulationBounds = GetComponent<SphereCollider>();

        updateBoidkernelIndex = BoidCalculation.FindKernel("UpdateBoid");
        
        InitAndBindBoidBuffer();

        InitAndBindArgsBuffer();

        InitAndBindFloats();
    }

    void InitAndBindArgsBuffer()
    {
        var args = new uint[] { boidMesh.GetIndexCount(0), boidsCount, 0, 0, 0 };
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
    }

    void InitAndBindBoidBuffer()
    {
        var boids = new Boid[boidsCount];
        for (int i = 0; i < boids.Length; i++)
        {
            boids[i].position = simulationBounds.center + Random.insideUnitSphere * Mathf.Clamp(spawnRadius, 0, simulationBounds.radius);
            boids[i].velocity = Random.insideUnitSphere * spawnVelocity;
            boids[i].acceleration = Vector3.zero;
        }

        boidsBuffer = new ComputeBuffer(boidsCount, boidStride);
        boidsBuffer.SetData(boids);
        BoidCalculation.SetBuffer(updateBoidkernelIndex, "boids", boidsBuffer); //TODO: necessary every frame?
    }

    void InitAndBindFloats()
    {
        BoidCalculation.SetFloat("alignmentForceFactor", alignmentForceFactor);
        BoidCalculation.SetFloat("cohesionForceFactor", cohesionForceFactor);
        BoidCalculation.SetFloat("separationForceFactor", separationForceFactor);
        BoidCalculation.SetFloat("boundsForceFactor", boundsForceFactor);
        BoidCalculation.SetFloat("alignmentDistance", alignmentDistance);
        BoidCalculation.SetFloat("cohesionDistance", cohesionDistance);
        BoidCalculation.SetFloat("separationDistance", separationDistance);
        BoidCalculation.SetFloats("simulationCenter", simulationBounds.center.x, simulationBounds.center.y, simulationBounds.center.z);
        BoidCalculation.SetFloat("simulationRadius", simulationBounds.radius);
        BoidCalculation.SetFloat("maxVelocity", maxVelocity);
        BoidCalculation.SetFloat("minVelocity", minVelocity);
    }
	
	void Update ()
    {
        BoidCalculation.SetFloat("deltaTime", Time.deltaTime);

        int threadGroupsCount = Mathf.CeilToInt((float) boidsCount / threadGroupSize);
        BoidCalculation.Dispatch(updateBoidkernelIndex, threadGroupsCount, 1, 1);
        
        boidMaterial.SetBuffer("boids", boidsBuffer);

        //render
        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, new Bounds(Vector3.zero, Vector3.one * 1.1f * simulationBounds.radius), argsBuffer);
    }

    void OnDestroy()
    {
        boidsBuffer.Release();
        argsBuffer.Release();
    }
}
