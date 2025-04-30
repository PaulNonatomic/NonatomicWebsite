using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DustEffectScript
{
    public class DustParticleManager : MonoBehaviour
    {
        public GameObject[] stylesArray; // Array to hold your GameObjects
        private int currentNumber = 0; // To keep track of the current GameObject
        public Text textCounting;
        void Update()
        {
            //if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (currentNumber < stylesArray.Length)
                //if (currentIndex < 7)
                {
                    if (currentNumber > 0)
                    {
                        stylesArray[currentNumber - 1].SetActive(false); // Deactivate the previous GameObject
                    }
                    stylesArray[currentNumber].SetActive(true); // Activate the current GameObject
                    textCounting.text = currentNumber.ToString();
                    currentNumber++; // Move to the next GameObject
                }
            }
        }
    }
}