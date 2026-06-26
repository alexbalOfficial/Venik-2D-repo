using UnityEngine;

public class FlagTester : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            GameFlags.Set("muhomor", true);
            Debug.Log("muhomor = true");
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            GameFlags.Set("muhomor", false);
            Debug.Log("muhomor = false");
        }
    }
}