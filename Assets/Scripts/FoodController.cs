using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class FoodController : MonoBehaviour
{
    public int FoodCount;
    public float LifeTime;
    public GameObject Food;

    SphereCollider spawnVolume;
    List<GameObject> foods;

	void Start ()
	{
	    spawnVolume = GetComponent<SphereCollider>();
        foods = new List<GameObject>(FoodCount);
        for (int i = 0; i < FoodCount; i++)
        {
            var go = Instantiate(Food, Random.insideUnitSphere * spawnVolume.radius, Quaternion.identity);
            go.AddComponent<Food>();
            go.GetComponent<Food>().LifeTime = Random.Range(0.0f, LifeTime);
            go.transform.parent = transform;
            foods.Add(go);
        }
    }
	
	void Update ()
    {
        foreach (var go in foods)
        {
            var food = go.GetComponent<Food>();
            food.LifeTime -= Time.deltaTime;
            if (food.LifeTime > 0)
                continue;

            food.LifeTime = LifeTime;
            go.transform.position = Random.insideUnitSphere * spawnVolume.radius;
        }
	}
}

public class Food : MonoBehaviour
{
    public float LifeTime;
}
