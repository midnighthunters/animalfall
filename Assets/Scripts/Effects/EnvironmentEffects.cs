// Task 7.2 — EnvironmentEffects: singleton state holder for movement modifiers
using UnityEngine;
using System.Collections.Generic;
using AnimalFall.Core.Hindrances;

namespace AnimalFall.Effects
{
    public class EnvironmentEffects : MonoBehaviour
    {
        public static EnvironmentEffects Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Properties ────────────────────────────────────────────────────────

        public bool    IsZeroGravityActive   { get; set; }
        public Vector2 WindForce             { get; set; }
        public bool    IsBlackHoleActive     { get; set; }
        public Vector2 BlackHoleCenter       { get; set; }
        public float   BlackHolePullStrength { get; set; } = 1.5f;
        public bool    IsMirrorModeActive    { get; set; }

        /// <summary>Computed — no field needed.</summary>
        public bool IsWindActive => WindForce.sqrMagnitude > 0.01f;

        private readonly Dictionary<object, Vector2> _windOwners = new Dictionary<object, Vector2>();
        private readonly HashSet<object> _zeroGravityOwners = new HashSet<object>();
        private readonly Dictionary<object, Vector3> _blackHoleOwners = new Dictionary<object, Vector3>();
        private readonly HashSet<object> _mirrorOwners = new HashSet<object>();

        public HindranceEffectToken AddWind(object owner, Vector2 force)
        {
            _windOwners[owner] = force; RecalculateWind();
            return new HindranceEffectToken(() => { _windOwners.Remove(owner); RecalculateWind(); });
        }

        public HindranceEffectToken AddZeroGravity(object owner)
        {
            _zeroGravityOwners.Add(owner); IsZeroGravityActive = true;
            return new HindranceEffectToken(() => { _zeroGravityOwners.Remove(owner); IsZeroGravityActive = _zeroGravityOwners.Count > 0; });
        }

        public HindranceEffectToken AddBlackHole(object owner, Vector2 center, float strength)
        {
            _blackHoleOwners[owner] = new Vector3(center.x, center.y, strength); RecalculateBlackHole();
            return new HindranceEffectToken(() => { _blackHoleOwners.Remove(owner); RecalculateBlackHole(); });
        }

        public HindranceEffectToken AddMirror(object owner)
        {
            _mirrorOwners.Add(owner); IsMirrorModeActive = true;
            return new HindranceEffectToken(() => { _mirrorOwners.Remove(owner); IsMirrorModeActive = _mirrorOwners.Count > 0; });
        }

        private void RecalculateWind()
        {
            Vector2 sum = Vector2.zero;
            foreach (Vector2 force in _windOwners.Values) sum += force;
            WindForce = Vector2.ClampMagnitude(sum, 4f);
        }

        private void RecalculateBlackHole()
        {
            IsBlackHoleActive = _blackHoleOwners.Count > 0;
            foreach (Vector3 value in _blackHoleOwners.Values)
            { BlackHoleCenter = new Vector2(value.x, value.y); BlackHolePullStrength = value.z; break; }
            if (!IsBlackHoleActive) { BlackHoleCenter = Vector2.zero; BlackHolePullStrength = 1.5f; }
        }

        /// <summary>Called on level start and end.</summary>
        public void ClearAll()
        {
            IsZeroGravityActive   = false;
            WindForce             = Vector2.zero;
            IsBlackHoleActive     = false;
            BlackHoleCenter       = Vector2.zero;
            BlackHolePullStrength = 1.5f;
            IsMirrorModeActive    = false;
            _windOwners.Clear();
            _zeroGravityOwners.Clear();
            _blackHoleOwners.Clear();
            _mirrorOwners.Clear();
        }
    }
}
