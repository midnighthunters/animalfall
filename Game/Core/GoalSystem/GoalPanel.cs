using System;
using UnityEngine;

public class GoalPanel : MonoBehaviour
{
    public static GoalPanel Instance { get; private set; }

    [SerializeField] private GoalObject chickenGoal;
    [SerializeField] private GoalObject dogGoal;
    [SerializeField] private GoalObject cowGoal;
    [SerializeField] private GoalObject catGoal;
    [SerializeField] private GoalObject monkeyGoal;
    [SerializeField] private GoalObject balloonGoal;

    public static event Action allGoalsEndedEvent;
    public static event Action goalsFailedEvent;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    private void OnEnable()
    {
        LevelManager.levelLoadedEvent += SetupGoalObjects;
    }

    private void OnDisable()
    {
        LevelManager.levelLoadedEvent -= SetupGoalObjects;
    }

    public void SetupGoalObjects()
    {
        var level = LevelManager.Instance.CurrentLevelData;
        if (level == null)
        {
            Debug.LogWarning("[GoalPanel] No current level data found.");
            return;
        }

        Goal myGoal = level.goal;

        // Reset & deactivate all first
        chickenGoal.gameObject.SetActive(false);
        dogGoal.gameObject.SetActive(false);
        cowGoal.gameObject.SetActive(false);
        catGoal.gameObject.SetActive(false);
        monkeyGoal.gameObject.SetActive(false);
        balloonGoal.gameObject.SetActive(false);

        // Reset counts (ResetGoal also hides mark + shows count text)
        chickenGoal.ResetGoal(myGoal.chickenCount);
        dogGoal.ResetGoal(myGoal.dogCount);
        cowGoal.ResetGoal(myGoal.cowCount);
        catGoal.ResetGoal(myGoal.catCount);
        monkeyGoal.ResetGoal(myGoal.monkeyCount);
        balloonGoal.ResetGoal(myGoal.balloonCount);

        // Activate only non-zero goals
        if (myGoal.chickenCount > 0) chickenGoal.gameObject.SetActive(true);
        if (myGoal.dogCount > 0) dogGoal.gameObject.SetActive(true);
        if (myGoal.cowCount > 0) cowGoal.gameObject.SetActive(true);
        if (myGoal.catCount > 0) catGoal.gameObject.SetActive(true);
        if (myGoal.monkeyCount > 0) monkeyGoal.gameObject.SetActive(true);
        if (myGoal.balloonCount > 0) balloonGoal.gameObject.SetActive(true);
    }

    // Single entry to decrease goals by species
    public void DecreaseGoal(AnimalSpecies species)
    {
        switch (species)
        {
            case AnimalSpecies.Chicken: chickenGoal.Count = chickenGoal.Count - 1; break;
            case AnimalSpecies.Dog: dogGoal.Count = dogGoal.Count - 1; break;
            case AnimalSpecies.Cow: cowGoal.Count = cowGoal.Count - 1; break;
            case AnimalSpecies.Cat: catGoal.Count = catGoal.Count - 1; break;
            case AnimalSpecies.Monkey: monkeyGoal.Count = monkeyGoal.Count - 1; break;
            case AnimalSpecies.Balloon: balloonGoal.Count = balloonGoal.Count - 1; break;
            default: break;
        }

        if (CheckGoalsAchieved())
        {
            allGoalsEndedEvent?.Invoke();
        }
    }

    public bool CheckGoalsAchieved()
    {
        bool allGoalsEnded = true;
        if (chickenGoal.gameObject.activeSelf && chickenGoal.Count > 0) allGoalsEnded = false;
        if (dogGoal.gameObject.activeSelf && dogGoal.Count > 0) allGoalsEnded = false;
        if (cowGoal.gameObject.activeSelf && cowGoal.Count > 0) allGoalsEnded = false;
        if (catGoal.gameObject.activeSelf && catGoal.Count > 0) allGoalsEnded = false;
        if (monkeyGoal.gameObject.activeSelf && monkeyGoal.Count > 0) allGoalsEnded = false;
        if (balloonGoal.gameObject.activeSelf && balloonGoal.Count > 0) allGoalsEnded = false;
        return allGoalsEnded;
    }

    // Utility for other systems to query if a species is still required
    public bool IsSpeciesRequired(AnimalSpecies species)
    {
        switch (species)
        {
            case AnimalSpecies.Chicken: return chickenGoal.Count > 0;
            case AnimalSpecies.Dog: return dogGoal.Count > 0;
            case AnimalSpecies.Cow: return cowGoal.Count > 0;
            case AnimalSpecies.Cat: return catGoal.Count > 0;
            case AnimalSpecies.Monkey: return monkeyGoal.Count > 0;
            case AnimalSpecies.Balloon: return balloonGoal.Count > 0;
            default: return false;
        }
    }

    // Optional: helper to get a goal UI world position (for flying fx)
    public Vector3 GetGoalPosition(AnimalSpecies species)
    {
        switch (species)
        {
            case AnimalSpecies.Chicken: return chickenGoal.transform.position;
            case AnimalSpecies.Dog: return dogGoal.transform.position;
            case AnimalSpecies.Cow: return cowGoal.transform.position;
            case AnimalSpecies.Cat: return catGoal.transform.position;
            case AnimalSpecies.Monkey: return monkeyGoal.transform.position;
            case AnimalSpecies.Balloon: return balloonGoal.transform.position;
            default: return transform.position;
        }
    }
}
