using UnityEngine;
using TMPro;

namespace AnimalFall.Core.Goals
{
    public class GoalObject : MonoBehaviour
    {
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject tickMark;
        [SerializeField] private ParticleSystem completionEffect;

        private int count;

        public int Count
        {
            get => count;
            set
            {
                if (value < count)
                    completionEffect?.Play();

                count = Mathf.Max(0, value);

                if (count == 0)
                {
                    countText.gameObject.SetActive(false);
                    tickMark.SetActive(true);
                }
                else
                {
                    countText.text = count.ToString();
                }
            }
        }

        public void ResetGoal(int newCount)
        {
            tickMark.SetActive(false);
            countText.gameObject.SetActive(true);
            Count = newCount;
        }
    }
}
