using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoidController : MonoBehaviour
{
    [SerializeField] float spawnRadius;
    [SerializeField] float spawnVelocity;
    [SerializeField] Boid[] boidsArray = new Boid[boidsCount];
    [SerializeField] ComputeShader BoidCalculation;

    int updateBoidkernelIndex;
    ComputeBuffer boidsBuffer;

    const int boidsCount = 32;
    const int boidStride = sizeof(float) * 3 * 3; //size of float members in bytes
    const int threadGroupSize = 32;

    [Serializable]
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
    }
	
	void Update ()
    {
        BoidCalculation.SetFloat("deltaTime", Time.deltaTime);

        int threadGroupsCount = Mathf.CeilToInt((float) boidsCount / threadGroupSize);
        BoidCalculation.Dispatch(updateBoidkernelIndex, threadGroupsCount, 1, 1);
        
        boidsBuffer.GetData(boidsArray); //Bad!
    }

    void OnDestroy()
    {
        boidsBuffer.Release();
    }
}
