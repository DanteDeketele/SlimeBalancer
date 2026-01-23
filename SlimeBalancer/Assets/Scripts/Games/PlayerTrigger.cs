using System.Collections.Generic;
using UnityEngine;

public class PlayerTrigger : MonoBehaviour
{


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public SlimeCollider.SlimeColor slimeColor;
    private TiltGame tiltGame;

    

    public ParticleSystem ParticleEffect;

    private List<GameObject> collidedSlimes = new List<GameObject>();

    void Start()
    {
        tiltGame = GameObject.FindAnyObjectByType<TiltGame>();
    }

    void Update()
    {
        foreach (var slime in collidedSlimes)
        {
            if (slime == null)
            {
                collidedSlimes.Remove(slime);
                break;
            }
        }
    }

    //on trigger when slime hits the player play particle effect and check color
    void OnCollisionEnter(Collision other)
    {
        // Check if the collider has a SlimeCollider component
        SlimeCollider slime = other.transform.GetComponent<SlimeCollider>();
        if (slime != null && collidedSlimes.Contains(other.gameObject) == false)
        {
            // Instantiate the particle system
            ParticleSystem particles = Instantiate(ParticleEffect, other.contacts[0].point + Vector3.up, Quaternion.identity);
            Color color = slime.GetComponent<Renderer>().material.GetColor("_MainColor");
            Renderer particleRenderer = particles.GetComponent<Renderer>();
            particleRenderer.material.SetColor("_MainColor", color);
            collidedSlimes.Add(other.gameObject);
            Destroy(particles.gameObject, 3f);
         
        }
    }

 


}
