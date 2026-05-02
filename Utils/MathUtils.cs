using UnityEngine;

namespace AnimalFall.Utils
{
    public static class MathUtils
    {
        public static float SineWave(float time, float frequency, float amplitude)
        {
            return Mathf.Sin(time * frequency * Mathf.PI * 2f) * amplitude;
        }

        public static Vector2 RandomPointOnScreen(Camera cam, float margin = 0.1f)
        {
            if (cam == null) return Vector2.zero;
            float x = Random.Range(margin, 1f - margin);
            float y = Random.Range(margin, 1f - margin);
            Vector3 world = cam.ViewportToWorldPoint(new Vector3(x, y, 10f));
            return new Vector2(world.x, world.y);
        }

        public static Vector2 RandomScreenTopPosition(Camera cam, float margin = 0.1f)
        {
            if (cam == null) return Vector2.zero;
            float x = Random.Range(margin, 1f - margin);
            Vector3 world = cam.ViewportToWorldPoint(new Vector3(x, 1.05f, 10f));
            return new Vector2(world.x, world.y);
        }

        public static Vector2 ScreenCenter(Camera cam)
        {
            if (cam == null) return Vector2.zero;
            Vector3 world = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
            return new Vector2(world.x, world.y);
        }

        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float t = Mathf.InverseLerp(fromMin, fromMax, value);
            return Mathf.Lerp(toMin, toMax, t);
        }

        public static Vector2 RandomDirection()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
