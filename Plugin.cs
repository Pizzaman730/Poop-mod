using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using BoplFixedMath;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Poop
{
    [BepInPlugin("com.PizzaMan730.Poop", "Poop", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {

        internal static ManualLogSource Log;
        private static Harmony harmony;

        private void Awake()
        {
            Log = base.Logger;

            Plugin.config = base.Config;
			Plugin.chance = Plugin.config.Bind<float>("Poop", "Poop chance", 50f, "Percent chance that you poop when jumping");

            Log.LogInfo("Poop has loaded!");

            harmony = new("com.PizzaMan730.Poop");

            harmony.PatchAll(typeof(Patch));
        }
        public static ConfigFile config;

		public static ConfigEntry<float> chance;
    }

    [HarmonyPatch]
    public class Patch
    {
        [HarmonyPatch(typeof(SlimeController), "Jump")]
        [HarmonyPrefix]

        private static bool Jump(ref bool ___wasUpsideDown, ref PlayerPhysics ___playerPhysics, ref PlayerBody ___body 
                                /*ref BoplBody ___grenadeBody, ref BoplBody ___ItemPrefab, ref Player ___player, 
                                ref PlayerInfo ___playerInfo, ref Grenade ___grenade, ref GameObject ___dummy, 
                                ref ThrowItem2 __instance*/)
        {
            Fix prevRotation = ___body.fixtrans.rotation;

            bool flag = ___playerPhysics.getAttachedGround().currentNormal(___body).y < 0L;
	        ___wasUpsideDown = flag;
	        ___playerPhysics.Jump();

            if (!(Updater.RandomFix((Fix)1, (Fix)2) <= (Fix)(1f + Plugin.chance.Value / 100))) {return false;}

            BoplBody grenade = new BoplBody();
            ThrowItem2[] allThrowItem2 = Resources.FindObjectsOfTypeAll(typeof(ThrowItem2)) as ThrowItem2[];
                foreach (ThrowItem2 obj in allThrowItem2)
                {
                    if (obj.name == "Grenade")
                    {
                        grenade = obj.ItemPrefab;
                    }
                }

            Vec2 spawnpos = new Vec2();
            if ((double)prevRotation < 0.75 || (double)prevRotation >= 5.5)
            {
                spawnpos = new Vec2(___body.fixtrans.position.x,  ___body.fixtrans.position.y - (Fix)2);
            }
            else if ((double)prevRotation < 5.5 && (double)prevRotation >= 4)
            {
                spawnpos = new Vec2(___body.fixtrans.position.x - (Fix)2,  ___body.fixtrans.position.y);
            }
            else if ((double)prevRotation < 4 && (double)prevRotation >= 2.5)
            {
                spawnpos = new Vec2(___body.fixtrans.position.x,  ___body.fixtrans.position.y + (Fix)3);
            }
            else if ((double)prevRotation < 2.5 && (double)prevRotation >= 0.75)
            {
                spawnpos = new Vec2(___body.fixtrans.position.x + (Fix)2,  ___body.fixtrans.position.y);
            }
            //6.25 max
            Debug.Log(prevRotation);


            BoplBody boplBody = FixTransform.InstantiateFixed<BoplBody>(grenade, spawnpos/*new Vec2(___body.fixtrans.position.x, ___body.fixtrans.position.y-(Fix)1)*/, ___body.fixtrans.rotation);
            boplBody.Scale = ___body.fixtrans.Scale;
            boplBody.StartVelocity = new Vec2(-___body.Velocity.x/(Fix)5, -___body.Velocity.y/(Fix)5);
            boplBody.rotation = ___body.fixtrans.rotation;
            boplBody.StartAngularVelocity = Fix.Zero;
            Item component = boplBody.GetComponent<Item>();
            component.OwnerId = (int)Traverse.Create(___body).Field("idHolder").GetValue();
            var Grenade = component.GetComponent<Grenade>();

            Grenade.hasBeenThrown = true;
            DPhysicsCircle dphysicsCircle = (boplBody != null) ? boplBody.GetComponent<DPhysicsCircle>() : null;
            if (dphysicsCircle != null && !dphysicsCircle.IsDestroyed)
            {
                if (!dphysicsCircle.initHasBeenCalled)
                {
                    dphysicsCircle.ManualInit();
                }
            }
            //Traverse.Create(Grenade).Field("hurtOwnerDelay").SetValue(-(Fix)50);
            Grenade.DetonatesOnOwner = false;
            //Grenade.hurtOwnerDelay = Fix.One;

            return false;
        }
    }
}


//      dotnet build "C:\Users\ajarc\BoplMods\Poop\Poop.csproj"