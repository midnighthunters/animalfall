// AnimalMovement — distinct fall patterns with polish (tilt, spin, bounce)
using UnityEngine;
using AnimalFall.Data;
using AnimalFall.Effects;
using AnimalFall.Managers;

namespace AnimalFall.Core.Animals
{
    [RequireComponent(typeof(Animal))]
    public class AnimalMovement : MonoBehaviour
    {
        private const float FallSpeedMultiplier = 0.65f;
        private const float SeparationDistance = 1.9f;
        private const float SeparationStrength = 6f;

        private float _screenLeft, _screenRight, _screenBottom, _screenTop;
        private int   _cachedScreenWidth, _cachedScreenHeight;

        private MovementPattern _pattern;
        private float _speed;
        private float _zigzagAmp;
        private float _zigzagFreq;
        private float _spawnTime;
        private float _startX;
        private int   _moveDirX = 1;
        private bool  _hasBounced;
        private bool  _fallSuspended;
        private float _phase;
        private float _spinSpeed;
        private float _bobAmp;
        private Vector3 _baseScale;
        private Vector2 _externalVelocity;
        private object _attachmentOwner;
        private bool _forceExit;
        private float _releaseProtectionUntil;
        private int _separationPhase;
        private float _separationSpeed;

        private Animal _animal;

        private void Awake()
        {
            _animal = GetComponent<Animal>();
            _separationPhase = Mathf.Abs(
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)) % 3;
            RecalcBounds();
            _cachedScreenWidth  = Screen.width;
            _cachedScreenHeight = Screen.height;
        }

        public void Configure(AnimalData data, LevelData level)
        {
            // Pool/test creation can configure the movement in the same frame that
            // its Animal component is attached. Resolve it lazily so a valid tap
            // target is never left half-initialized by component Awake order.
            if (_animal == null) _animal = GetComponent<Animal>();
            if (_animal == null)
            {
                enabled = false;
                return;
            }

            RecalcBounds();

            _pattern     = data.movementPattern;
            _speed       = Random.Range(data.speedMin, data.speedMax) * FallSpeedMultiplier;
            _zigzagAmp   = data.zigzagAmplitude;
            _zigzagFreq  = data.zigzagFrequency;
            _spawnTime   = Time.time;
            _startX      = transform.position.x;
            _moveDirX    = Random.value > 0.5f ? 1 : -1;
            _hasBounced  = false;
            _fallSuspended = false;
            _phase       = Random.Range(0f, Mathf.PI * 2f);
            _spinSpeed   = Random.Range(80f, 160f) * (Random.value > 0.5f ? 1f : -1f);
            _bobAmp      = Random.Range(0.08f, 0.18f);
            _baseScale   = Vector3.one * _animal.CurrentScale;
            _externalVelocity = Vector2.zero;
            _attachmentOwner = null;
            _forceExit = false;
            _releaseProtectionUntil = 0f;
            _separationSpeed = 0f;
            transform.localScale = _baseScale;
            transform.rotation = Quaternion.identity;
            enabled = true;
        }

        public void ResumeFall() => _fallSuspended = false;

        public bool TryAttach(object owner)
        {
            if (owner == null || (_attachmentOwner != null && !ReferenceEquals(_attachmentOwner, owner))) return false;
            _attachmentOwner = owner;
            _fallSuspended = true;
            return true;
        }

        public void ReleaseAttachment(object owner, Vector2 releaseVelocity, float protectionSeconds = 0.35f)
        {
            if (!ReferenceEquals(_attachmentOwner, owner)) return;
            _attachmentOwner = null;
            _fallSuspended = false;
            _externalVelocity = Vector2.ClampMagnitude(releaseVelocity, 4.5f);
            _releaseProtectionUntil = Time.time + Mathf.Max(0f, protectionSeconds);
        }

        public void AddImpulse(Vector2 impulse)
        {
            _externalVelocity = Vector2.ClampMagnitude(_externalVelocity + impulse, 5f);
        }

        /// <summary>Launches the animal beyond the visible playfield without counting it as a missed catch.</summary>
        public void LaunchOutOfScreen(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f) direction = Vector2.right;
            _attachmentOwner = null;
            _fallSuspended = false;
            _forceExit = true;
            _externalVelocity = direction.normalized * 9f;
            _releaseProtectionUntil = Time.time + 0.15f;
        }

        private void Update()
        {
            if (_animal == null)
            {
                _animal = GetComponent<Animal>();
                if (_animal == null) { enabled = false; return; }
            }

            if (_cachedScreenWidth != Screen.width || _cachedScreenHeight != Screen.height || _screenTop == 0f)
            {
                RecalcBounds();
                _cachedScreenWidth  = Screen.width;
                _cachedScreenHeight = Screen.height;
            }

            var env = EnvironmentEffects.Instance;
            float dt = Time.deltaTime > 0f ? Time.deltaTime : (!Application.isPlaying ? 0.01667f : 0f);
            if (dt <= 0f) return;

            float dx = 0f;
            float gravityScale = env != null && env.IsZeroGravityActive ? 0.18f : 1f;
            bool reverseGravity = env != null && env.IsReverseGravityActive;
            float bubbleFallScale = _animal.IsBubble ? 0.75f : 1f;
            float dy = _fallSuspended ? 0f : (reverseGravity ? _speed : -_speed * gravityScale * bubbleFallScale) * dt;
            float elapsed = Time.time - _spawnTime;
            float tilt = 0f;
            float pendingTeleportX = float.NaN;

            if (_animal.IsBubble)
            {
                dx += Mathf.Sin(elapsed * 2.5f + _phase) * 0.45f * dt;
                tilt = Mathf.Sin(elapsed * 3f + _phase) * 8f;
            }

            if (_pattern != MovementPattern.Bounce && _pattern != MovementPattern.HeavyFall)
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, dt * 10f);

            switch (_pattern)
            {
                case MovementPattern.Static:
                    // pure fall, slight idle wobble
                    tilt = Mathf.Sin(elapsed * 3f + _phase) * 6f;
                    break;

                case MovementPattern.Drift:
                    dx = _moveDirX * _speed * 0.35f * dt;
                    tilt = _moveDirX * -18f;
                    break;

                case MovementPattern.ZigZag:
                {
                    // Classic zig-zag: horizontal velocity flips direction each half period.
                    float sign = Mathf.Sign(Mathf.Sin(elapsed * _zigzagFreq * Mathf.PI + _phase));
                    if (sign == 0f) sign = 1f;
                    dx = sign * _speed * 0.6f * dt;
                    tilt = -sign * 20f;
                    break;
                }

                case MovementPattern.SineWave:
                {
                    float targetX = _startX + Mathf.Sin(elapsed * _zigzagFreq * Mathf.PI * 2f + _phase) * _zigzagAmp;
                    float curX = transform.position.x;
                    dx = (targetX - curX);
                    // limit step
                    dx = Mathf.Clamp(dx, -_speed * dt * 2.5f, _speed * dt * 2.5f);
                    tilt = -Mathf.Cos(elapsed * _zigzagFreq * Mathf.PI * 2f + _phase) * 20f;
                    // gentle bob on top of fall
                    dy += Mathf.Sin(elapsed * 4f + _phase) * _bobAmp * dt;
                    break;
                }

                case MovementPattern.Bounce:
                    dx = _moveDirX * _speed * 0.9f * dt;
                    tilt = _moveDirX * -14f;
                    // squash pulse
                    float squash = 1f + Mathf.Sin(elapsed * 10f) * 0.06f;
                    transform.localScale = new Vector3(_baseScale.x * squash, _baseScale.y / squash, 1f);
                    break;

                case MovementPattern.Teleport:
                    // fall normally, then blink sideways
                    if (elapsed > 1.6f)
                    {
                        float newX = Random.Range(_screenLeft + 0.6f, _screenRight - 0.6f);
                        pendingTeleportX = newX;
                        _startX = newX;
                        _spawnTime = Time.time;
                        // brief scale flash
                        transform.localScale = _baseScale * 0.4f;
                    }
                    else
                    {
                        transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, dt * 8f);
                    }
                    tilt = Mathf.Sin(elapsed * 8f) * 10f;
                    break;

                case MovementPattern.FloatUp:
                    dy = _speed * 0.55f * dt;
                    dx = Mathf.Sin(elapsed * 2.2f + _phase) * _zigzagAmp * 0.4f * dt * 4f;
                    tilt = Mathf.Sin(elapsed * 2.2f + _phase) * 12f;
                    break;

                case MovementPattern.HeavyFall:
                    // accelerating fall
                    float accel = Mathf.Min(2.2f, 1f + elapsed * 0.35f);
                    dy = -_speed * 1.8f * accel * dt;
                    tilt = Mathf.Sin(elapsed * 14f) * 4f; // vibration
                    transform.localScale = new Vector3(_baseScale.x * 1.05f, _baseScale.y * 0.92f, 1f);
                    break;

                case MovementPattern.Erratic:
                    dx = Mathf.PerlinNoise(elapsed * 2.5f, _phase) * 2f - 1f;
                    dx *= _speed * 0.9f * dt;
                    dy = -_speed * (0.7f + Mathf.PerlinNoise(_phase, elapsed * 1.8f) * 0.8f) * dt;
                    // continuous spin
                    transform.Rotate(0f, 0f, _spinSpeed * dt);
                    break;
            }

            // The gravity switch takes priority over every normal falling pattern.
            // It keeps both current and newly spawned animals moving upward.
            if (reverseGravity && !_fallSuspended)
                dy = _speed * Mathf.Max(0.55f, gravityScale) * dt;

            // Wind / black hole / mirror
            if (env != null && env.IsWindActive)
            {
                dx += env.WindForce.x * dt;
                dy += env.WindForce.y * dt;
            }
            if (env != null && env.IsBlackHoleActive)
            {
                Vector2 toHole = env.BlackHoleCenter - (Vector2)transform.position;
                float dist = toHole.magnitude;
                if (dist < 0.5f)
                {
                    Vector2 eject = dist > 0.01f ? -toHole.normalized : Vector2.up;
                    _externalVelocity = eject * 3f + Vector2.up;
                }
                else if (dist < 4f)
                {
                    Vector2 pull = toHole.normalized * env.BlackHolePullStrength * dt;
                    dx += pull.x;
                    dy += pull.y;
                }
            }
            Vector3 pos = transform.position;
            if (!float.IsNaN(pendingTeleportX)) pos.x = pendingTeleportX;
            dx += _externalVelocity.x * dt;
            dy += _externalVelocity.y * dt;
            // Separation is the only O(n^2) part of normal-level movement. Spread
            // those scans across three frames, then reuse the resulting velocity.
            if ((Time.frameCount + _separationPhase) % 3 == 0)
                _separationSpeed = CalculateHorizontalSeparationSpeed();
            dx += _separationSpeed * dt;
            _externalVelocity = Vector2.MoveTowards(_externalVelocity, Vector2.zero, 3.5f * dt);
            pos.x += dx;
            pos.y += dy;

            if (_pattern == MovementPattern.Bounce)
            {
                if (pos.x <= _screenLeft + 0.3f || pos.x >= _screenRight - 0.3f)
                    _moveDirX = -_moveDirX;
            }

            if (!_forceExit) pos.x = Mathf.Clamp(pos.x, _screenLeft + 0.25f, _screenRight - 0.25f);
            transform.position = pos;

            // Tilt for non-spinning patterns
            if (_pattern != MovementPattern.Erratic)
            {
                float z = Mathf.LerpAngle(transform.eulerAngles.z, tilt, dt * 8f);
                transform.rotation = Quaternion.Euler(0f, 0f, z);
            }

            // Despawn when the animal leaves the playfield. FloatUp/Bubble exit the top;
            // everything else exits the bottom. A small margin avoids popping while visible.
            bool exitedBottom = pos.y < _screenBottom - 0.6f;
            bool isMovingUp   = _pattern == MovementPattern.FloatUp || reverseGravity || dy > 0.001f;
            bool exitedTop    = (isMovingUp || _forceExit) && pos.y > _screenTop + 0.6f;
            bool exitedSide   = _forceExit && (pos.x < _screenLeft - 0.6f || pos.x > _screenRight + 0.6f);
            if ((exitedBottom || exitedTop || exitedSide) && Time.time >= _releaseProtectionUntil)
            {
                // Only count a miss when the animal fell past the bottom (was catchable).
                if (exitedBottom) GameEvents.OnAnimalMissed?.Invoke();
                _animal.Despawn();
            }
        }

        private float CalculateHorizontalSeparationSpeed()
        {
            float push = 0f;
            Vector3 position = transform.position;
            var animals = ActiveAnimalRegistry.All;
            for (int i = 0; i < animals.Count; i++)
            {
                Animal other = animals[i];
                if (other == null || other == _animal || !other.gameObject.activeInHierarchy || other.IsCollected)
                    continue;

                Vector3 delta = position - other.transform.position;
                float absX = Mathf.Abs(delta.x);
                if (absX >= SeparationDistance || Mathf.Abs(delta.y) >= SeparationDistance) continue;

                float direction = absX > 0.001f
                    ? Mathf.Sign(delta.x)
                    : (System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this) <
                       System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(other) ? -1f : 1f);
                push += direction * (SeparationDistance - absX) * SeparationStrength;
            }

            return Mathf.Clamp(push, -3.5f, 3.5f);
        }

        private void RecalcBounds()
        {
            if (Camera.main == null) return;
            var cam = Camera.main;
            float z = Mathf.Abs(cam.transform.position.z);
            _screenLeft   = cam.ViewportToWorldPoint(new Vector3(0, 0, z)).x;
            _screenRight  = cam.ViewportToWorldPoint(new Vector3(1, 0, z)).x;
            _screenBottom = cam.ViewportToWorldPoint(new Vector3(0, 0, z)).y;
            _screenTop    = cam.ViewportToWorldPoint(new Vector3(0, 1, z)).y;
        }
    }
}
