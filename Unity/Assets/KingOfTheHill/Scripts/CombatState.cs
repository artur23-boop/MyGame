using System;

namespace WinterYard
{
    // Pure timing/state rules, independent of rendering and Unity physics.
    public sealed class CombatState
    {
        public const float ReloadSeconds=2.5f;
        public const float ComboWindowSeconds=6;
        public const int ComboTarget=3;
        public const float KnockdownSeconds=5;
        public float ReloadRemaining {get;private set;}
        public float ComboRemaining {get;private set;}
        public int ComboHits {get;private set;}
        public bool SuperReady {get;private set;}
        public float KnockdownRemaining {get;private set;}
        public bool HasSnow => ReloadRemaining<=0;

        public void Tick(float delta)
        {
            if(delta<0)throw new ArgumentOutOfRangeException(nameof(delta));
            ReloadRemaining=Math.Max(0,ReloadRemaining-delta);
            KnockdownRemaining=Math.Max(0,KnockdownRemaining-delta);
            ComboRemaining=Math.Max(0,ComboRemaining-delta);
            if(ComboRemaining<=0)ComboHits=0;
        }
        public bool TryThrow(bool super)
        {
            if(!HasSnow||KnockdownRemaining>0||(super&&!SuperReady))return false;
            ReloadRemaining=ReloadSeconds;
            if(super)SuperReady=false;
            return true;
        }
        public void RegisterHit()
        {
            if(SuperReady)return;
            ComboHits++;ComboRemaining=ComboWindowSeconds;
            if(ComboHits>=ComboTarget){SuperReady=true;ComboHits=0;ComboRemaining=0;}
        }
        public void BreakCombo(){ComboHits=0;ComboRemaining=0;}
        public void KnockDown(){KnockdownRemaining=KnockdownSeconds;BreakCombo();}
    }
}
