using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoidController : MonoBehaviour
{
    [SerializeField] float spawnRadius;
    [SerializeField] float spawnVelocity;
    [SerializeField] ComputeShader BoidCalculation;
    [SerializeField] Mesh boidMesh;
    [SerializeField] Material boidMaterial;

    int updateBoidkernelIndex;
    ComputeBuffer boidsBuffer;
    ComputeBuffer argsBuffer;

    const int boidsCount = 32;
    const int boidStride = sizeof(float) * 3 * 3; //size of float members in bytes
    const int threadGroupSize = 32;
    
    struct Boid
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
    }
    
    void Start ()
    {
        updateBoidkernelIndex = BoidCalculation.FindKernel("UpdateBoid");
        
        var boids = new Boid[boidsCount];
        for(int i = 0; i < boids.Length; i++)
        {
            boids[i].position = transform.position + Random.insideUnitSphere * spawnRadius; //TODO: Add bounds
            boids[i].velocity = Random.insideUnitSphere * spawnVelocity;
            boids[i].acceleration = Vector3.zero;
        }

        boidsBuffer = new ComputeBuffer(boidsCount, boidStride);
        boidsBuffer.SetData(boids);
        BoidCalculation.SetBuffer(updateBoidkernelIndex, "boids", boidsBuffer); //TODO: necessary every frame?

        var args = new uint[] {(uint) boidMesh.GetIndexCount(0), (uint) boidsCount, 0, 0, 0};
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
    }
	
	void Update ()
    {
        BoidCalculation.SetFloat("deltaTime", Time.deltaTime);

        int threadGroupsCount = Mathf.CeilToInt((float) boidsCount / threadGroupSize);
        BoidCalculation.Dispatch(updateBoidkernelIndex, threadGroupsCount, 1, 1);
        
        boidMaterial.SetBuffer("boids", boidsBuffer);

        //render
        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, new Bounds(Vector3.zero, new Vector3(100, 100, 100)), argsBuffer);
    }

    void OnDestroy()
    {
        boidsBuffer.Release();
        argsBuffer.Release();
    }
}
