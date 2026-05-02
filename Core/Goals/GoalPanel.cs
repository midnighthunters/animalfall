using System;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Core.Levels;
using AnimalFall.Managers;

namespace AnimalFall.Core.Goals
{
    public class GoalPanel : MonoBehaviour
    {
        public static GoalPanel Instance { get; private set; }

        [SerializeField] private GoalObject chickenGoal;
        [SerializeField] private GoalObject dogGoal;
        [SerializeField] private GoalObject cowGoal;
        [SerializeField] private GoalObject catGoal;
        [SerializeField] private GoalObject monkeyGoal;
        [SerializeField] private GoalObject balloonGoal;

        public static event Action AllGoalsCompleted;
        public static event Action GoalsFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            LevelManager.LevelLoadedEvent += SetupGoalObjects;
        }

        private void OnDisable()
        {
            LevelManager.LevelLoadedEvent -= SetupGoalObjects;
        }

        public void SetupGoalObjects()
        {
            var level = LevelManager.Instance.CurrentLevelData;
            if (level == null) return;

            Goal goal = level.goal;

            chickenGoal.gameObject.SetActive(false);
            dogGoal.gameObject.SetActive(false);
            cowGoal.gameObject.SetActive(false);
            catGoal.gameObject.SetActive(false);
            monkeyGoal.gameObject.SetActive(false);
            balloonGoal.gameObject.SetActive(false);

            chickenGoal.ResetGoal(goal.chickenCount);
            dogGoal.ResetGoal(goal.dogCount);
            cowGoal.ResetGoal(goal.cowCount);
            catGoal.ResetGoal(goal.catCount);
            monkeyGoal.ResetGoal(goal.monkeyCount);
            balloonGoal.ResetGoal(goal.balloonCount);

            if (goal.chickenCount > 0) chickenGoal.gameObject.SetActive(true);
            if (goal.dogCount > 0) dogGoal.gameObject.SetActive(true);
            if (goal.cowCount > 0) cowGoal.gameObject.SetActive(true);
            if (goal.catCount > 0) catGoal.gameObject.SetActive(true);
            if (goal.monkeyCount > 0) monkeyGoal.gameObject.SetActive(true);
            if (goal.balloonCount > 0) balloonGoal.gameObject.SetActive(true);
        }

        public void DecreaseGoal(AnimalSpecies species)
        {
            GoalObject goal = GetGoalForSpecies(species);
            if (goal != null)
                goal.Count--;

            if (CheckAllGoalsAchieved())
                AllGoalsCompleted?.Invoke();
        }

        public bool IsSpeciesRequired(AnimalSpecies species)
        {
            GoalObject goal = GetGoalForSpecies(species);
            return goal != null && goal.Count > 0;
        }

        public Vector3 GetGoalPosition(AnimalSpecies species)
        {
            GoalObject goal = GetGoalForSpecies(species);
            return goal != null ? goal.transform.position : Vector3.zero;
        }

        private bool CheckAllGoalsAchieved()
        {
            return !IsGoalActive(chickenGoal) &&
                   !IsGoalActive(dogGoal) &&
                   !IsGoalActive(cowGoal) &&
                   !IsGoalActive(catGoal) &&
                   !IsGoalActive(monkeyGoal) &&
                   !IsGoalActive(balloonGoal);
        }

        private bool IsGoalActive(GoalObject goal)
        {
            return goal.gameObject.activeSelf && goal.Count > 0;
        }

        private GoalObject GetGoalForSpecies(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Chicken: return chickenGoal;
                case AnimalSpecies.Dog:     return dogGoal;
                case AnimalSpecies.Cow:     return cowGoal;
                case AnimalSpecies.Cat:     return catGoal;
                case AnimalSpecies.Monkey:  return monkeyGoal;
                case AnimalSpecies.Balloon: return balloonGoal;
                default:                    return null;
            }
        }
    }
}
