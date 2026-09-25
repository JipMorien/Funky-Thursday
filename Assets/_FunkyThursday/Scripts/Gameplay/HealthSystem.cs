using System;
using FunkyThursday.Core;
using UnityEngine;

namespace FunkyThursday.Gameplay
{
    /// <summary>
    /// Tug-of-war health: 0 = player perished, 1 = player owns the whole bar. Pure model with
    /// events; HealthBar draws it and GameplayController decides what hits feed it.
    /// </summary>
    public sealed class HealthSystem : MonoBehaviour
    {
        [SerializeField, Range(0.05f, 1f)] float startHealth = 0.5f;

        [Header("Per judgement")]
        [SerializeField] float perfectGain = 0.015f;
        [SerializeField] float goodGain = 0.0075f;
        [SerializeField] float missLoss = 0.035f;
        [SerializeField] float ghostTapLoss = 0.02f;
        [SerializeField] float holdDropLoss = 0.025f;

        [Header("Sustains")]
        [SerializeField] float holdGainPerSecond = 0.02f;

        [Header("Debug")]
        [SerializeField] bool invincible;

        /// <summary>Multiplies every health loss (misses, dropped holds, ghost taps). 1 = normal.</summary>
        public float LossScale { get; set; } = 1f;

        /// <summary>Multiplies opponent drain. 0 turns it off.</summary>
        public float DrainScale { get; set; } = 1f;

        public float Value { get; private set; }
        public bool IsDead { get; private set; }

        public event Action<float> Changed;
        public event Action Died;

        void Awake() => ResetHealth();

        public void ResetHealth()
        {
            IsDead = false;
            Set(startHealth, force: true);
        }

        public void Apply(in HitResult result)
        {
            switch (result.Judgement)
            {
                case Judgement.Perfect:
                    Add(perfectGain);
                    break;
                case Judgement.Good:
                    Add(goodGain);
                    break;
                default:
                    Add(-LossScale * (result.Kind == HitKind.GhostTap ? ghostTapLoss
                        : result.Kind == HitKind.HoldDropped ? holdDropLoss
                        : missLoss));
                    break;
            }
        }

        public void AddHold(float seconds) => Add(seconds * holdGainPerSecond);

        /// <summary>Opponent drain: removes up to <paramref name="amount"/> but never below <paramref name="floor"/>.</summary>
        public void Drain(float amount, float floor)
        {
            amount *= DrainScale;
            if (amount <= 0f || Value <= floor) return;
            Set(Mathf.Max(floor, Value - amount));
        }

        void Add(float delta)
        {
            if (IsDead) return;

            float next = Mathf.Clamp01(Value + delta);
            if (invincible) next = Mathf.Max(next, 0.01f);
            Set(next);

            if (next <= 0f)
            {
                IsDead = true;
                Died?.Invoke();
            }
        }

        void Set(float value, bool force = false)
        {
            if (!force && Mathf.Approximately(value, Value)) return;
            Value = value;
            Changed?.Invoke(Value);
        }
    }
}
