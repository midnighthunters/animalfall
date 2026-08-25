// Task 12.1 — AppBootstrap: frame rate, battery watcher + service init
using UnityEngine;
using AnimalFall.Managers;
using AnimalFall.Services;

namespace AnimalFall
{
    public class AppBootstrap : MonoBehaviour
    {
        private float _batteryCheckTimer;

        private void Awake()
        {
            Application.targetFrameRate = 60;

            // Wire SaveService → LevelManager and LivesManager
            var save = GetComponent<SaveService>() ?? FindFirstObjectByType<SaveService>();
            if (save != null)
            {
                LevelManager.Instance?.Init(save);
                LivesManager.Instance?.Init(save);
            }
        }

        private void Update()
        {
            // Check battery every 10 seconds to avoid per-frame cost
            _batteryCheckTimer -= Time.deltaTime;
            if (_batteryCheckTimer > 0f) return;
            _batteryCheckTimer = 10f;

            if (SystemInfo.batteryLevel > 0 &&
                SystemInfo.batteryLevel <= 0.2f &&
                SystemInfo.batteryStatus == BatteryStatus.Discharging)
            {
                Application.targetFrameRate = 30;
            }
            else
            {
                Application.targetFrameRate = 60;
            }
        }
    }
}
