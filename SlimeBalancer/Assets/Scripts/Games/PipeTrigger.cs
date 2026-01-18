using UnityEngine;

public class PipeTrigger : MonoBehaviour
{
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public SlimeCollider.SlimeColor slimeColor;
    private TiltGame tiltGame;
    
    void Start()
    {
        tiltGame = GameObject.FindAnyObjectByType<TiltGame>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        SlimeCollider.SlimeColor color = other.GetComponent<SlimeCollider>().slimeColor;
        if (color == slimeColor)
        {
            
            tiltGame.Correct();
        }
        else
        {
            tiltGame.Wrong();
        }

    }
 
}
