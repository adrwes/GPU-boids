using System.Collections.Generic;
using System.Linq;
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
	    foods = SpawnFoods().ToList();
	}

    IEnumerable<GameObject> SpawnFoods()
    {
        for (int i = 0; i < FoodCount; i++)
        {
            var go = Instantiate(Food, Random.insideUnitSphere * spawnVolume.radius, Quaternion.identity);
            go.AddComponent<Food>();
            go.GetComponent<Food>().LifeTime = Random.Range(0.0f, LifeTime);
            go.transform.parent = transform;
            yield return go;
        }
    }

	void Update ()
    {
        foreach (var food in foods)
        {
            food.GetComponent<Food>().LifeTime -= Time.deltaTime;
            if (food.GetComponent<Food>().LifeTime > 0)
                continue;

            RespawnFood(food);
        }
	}

    void RespawnFood(GameObject food)
    {
        food.GetComponent<Food>().LifeTime = LifeTime;
        food.transform.position = Random.insideUnitSphere * spawnVolume.radius;
    }
}
