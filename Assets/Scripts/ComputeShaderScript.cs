using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ComputeShaderScript : MonoBehaviour
{
    [SerializeField]
    ComputeShader computeShader;

    int kernelIndex;
    
    void Start ()
	{
        kernelIndex = computeShader.FindKernel("Tripple");
        
	    Debug.Log(RunTripple(new List<float> {1f, 2f, 3f, 4f}.Select(x => new Vector3(x, x, x)))
	        .Select(x => x.ToString())
	        .Aggregate((acc, x) => acc + " " + x));
	}
    
	void Update () {
		
	}

    IEnumerable<Vector3> RunTripple(IEnumerable<Vector3> input)
    {
        var inputArray = input.ToArray();
        var computeBuffer = new ComputeBuffer(inputArray.Length, 4*3);
        computeBuffer.SetData(inputArray);
        computeShader.SetBuffer(kernelIndex, "dataBuffer", computeBuffer);

        computeShader.Dispatch(kernelIndex, inputArray.Length, 1, 1);

        var outputArray = new Vector3[inputArray.Length];
        computeBuffer.GetData(outputArray); //Bad and slow
        computeBuffer.Release();
        return outputArray;
    }
}
