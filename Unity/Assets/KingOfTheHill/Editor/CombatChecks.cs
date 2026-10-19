using System;
using UnityEditor;
using UnityEngine;
using WinterYard;

namespace WinterYardEditor
{
    public static class CombatChecks
    {
        static void Check(bool condition,string message)
        {
            if(!condition)throw new Exception("Combat check failed: "+message);
        }
        [MenuItem("Winter Yard/Run combat rules checks")]
        public static void Run()
        {
            var state=new CombatState();
            Check(state.HasSnow,"start loaded");
            Check(!state.TryThrow(true),"super requires combo");
            Check(state.TryThrow(false),"normal throw");
            Check(!state.TryThrow(false),"cannot bypass reload");
            state.Tick(2.49f);Check(!state.HasSnow,"reload not early");
            state.Tick(.02f);Check(state.HasSnow,"reload completes");
            state.RegisterHit();state.Tick(5);state.RegisterHit();state.Tick(5);state.RegisterHit();
            Check(state.SuperReady,"three hits refresh combo window");
            Check(state.TryThrow(true)&&!state.SuperReady,"super consumed once");
            Check(!state.TryThrow(true),"super cannot repeat");
            state.RegisterHit();state.Tick(6.01f);Check(state.ComboHits==0,"combo expires");
            state.RegisterHit();state.BreakCombo();Check(state.ComboHits==0,"miss or hit breaks combo");
            state.RegisterHit();state.RegisterHit();state.RegisterHit();state.BreakCombo();
            Check(state.SuperReady,"earned super survives combo reset");
            state.KnockDown();Check(!state.TryThrow(false),"no action while down");
            state.Tick(4.99f);Check(state.KnockdownRemaining>0,"down for full five seconds");
            state.Tick(.02f);Check(state.KnockdownRemaining==0,"can get up after five seconds");
            Debug.Log("Winter Yard: all combat rules checks passed (reload, combo, super, knockdown). Physics/visual checks must be run in Play mode.");
        }
    }
}
