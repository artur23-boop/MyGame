using System;
using UnityEditor;
using UnityEngine;
using WinterYard;

namespace WinterYardEditor
{
    public static class LobThrowChecks
    {
        static void Check(bool condition,string message){if(!condition)throw new Exception("Lob throw check failed: "+message);}
        [MenuItem("Winter Yard/Run lob throw rules checks")]
        public static void Run()
        {
            var charge=new LobCharge();
            Check(!charge.Release(out _,out _),"release without charge does not fire");
            charge.Begin(false);charge.Tick(.6f);Check(Mathf.Abs(charge.Power-.5f)<.001f,"half charge after 0.6 seconds");
            charge.Tick(5);Check(charge.Power==1,"charge caps at full");
            Check(charge.Release(out var power,out var super)&&power==1&&!super&&!charge.Active,"release captures power once");
            Check(!charge.Release(out _,out _),"release cannot repeat");
            charge.Begin(true);charge.Tick(.4f);charge.Cancel();
            Check(!charge.Release(out _,out _)&&!charge.Active,"cancel prevents a delayed shot");
            charge.Begin(true);charge.Tick(1.2f);charge.Release(out power,out super);Check(super&&power==1,"super uses charge too");
            var weak=LobThrow.Velocity(Vector3.forward,0,55);var strong=LobThrow.Velocity(Vector3.forward,1,55);
            Check(strong.y>weak.y&&strong.z>weak.z,"charge increases height and range");
            var high=LobThrow.Velocity(Vector3.forward,1,75);var low=LobThrow.Velocity(Vector3.forward,1,30);
            Check(high.y>low.y&&high.z<low.z,"higher angle trades horizontal speed for height");
            Check(Mathf.Abs(high.magnitude-17)<.01f,"angle does not create extra energy");
            Check(Snowball.LaunchVelocity(Vector3.forward,1,55)==strong,"preview and projectile share launch formula");
            Check(Snowball.Lifetime>2*high.y/Physics.gravity.magnitude,"projectile survives a full same-height high arc");
            Debug.Log("Lob throw charge/cancel/ballistic rules checks passed. Check trajectory collision and camera in Play Mode.");
        }
    }
}
