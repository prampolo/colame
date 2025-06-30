using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class triggerScript : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (UImanager.instance.lifeState == 5 || UImanager.instance.lifeState == 7)
        {
            if (other.CompareTag("CrystalBlue") || other.CompareTag("CrystalRed") || other.CompareTag("CrystalGreen")
             || other.CompareTag("BorderGreen") || other.CompareTag("BorderRed") || other.CompareTag("BorderBlue"))
            {
                Destroy(other.gameObject);
                Debug.Log("[TRIGGER] Destroyed: " + other.gameObject.name);
            }
        }
    }
}
