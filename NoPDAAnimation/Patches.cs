using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace NoPDAAnimation;

[HarmonyPatch]
public static class Patch_Animation
{
    public static void OpenPDA(PDA pda)
    {
        Transform transform = Player.main.camRoot.mainCam.transform;

        // If the PDA remains in the hand, it will move with the hand.
        pda.transform.SetParent(transform);

        pda.transform.position = transform.position;
        pda.transform.forward = transform.forward;

        // rotate the PDA to see its front face
        pda.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

        pda.transform.localPosition = new Vector3(Plugin.ModConfig.X, Plugin.ModConfig.Y, Plugin.ModConfig.Z);
    }

    [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.SetUsingPda))]
    public static class Patch_ArmsController_SetUsingPda
    {
        static bool Prefix(ArmsController __instance, bool isUsing)
        {
            if (isUsing)
            {
                OpenPDA(__instance.pda);

                // execute animation events spawn_pda and OnToolAnimDraw
                __instance.spawn_pda();
                __instance.player.gameObject.GetComponent<GUIHand>().OnToolAnimDraw();
            }
            else
            {
                // execute animation event kill_pda
                __instance.kill_pda();
            }

            return false;
        }
    }
}

[HarmonyPatch]
public static class Patch_CameraMove
{
    [HarmonyPatch(typeof(MainCameraControl), nameof(MainCameraControl.OnUpdate))]
    public static class Patch_MainCameraControl_OnUpdate
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            /*
            IL_0259: call         bool [UnityEngine.VRModule]UnityEngine.XR.XRSettings::get_enabled()
            IL_025e: brtrue.s     IL_02b5
            // PATCH HERE

            // [249 7 - 249 71]
            IL_0260: ldarg.0      // this
            IL_0261: ldfld        class [UnityEngine.CoreModule]UnityEngine.Transform MainCameraControl::cameraOffsetTransform
            IL_0266: callvirt     instance valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 [UnityEngine.CoreModule]UnityEngine.Transform::get_localPosition()
            IL_026b: stloc.s      localPosition
            */

            CodeMatcher cm = new(instructions);

            /* Find:
            if (!XRSettings.enabled)
            {
                Vector3 localPosition = this.cameraOffsetTransform.localPosition;
                localPosition.z = Mathf.Clamp(localPosition.z + (float)((double)PDA.deltaTime * (double)num1 * 0.25), 0.0f + this.camPDAZStart, this.camPDAZOffset + this.camPDAZStart);
                this.cameraOffsetTransform.localPosition = localPosition;
            }
            */
            
            /* After patch:
            if (!XRSettings.enabled && !Player.main.GetPDA().isInUse)
            */

            cm.MatchForward(false, // false = move at the start of the match, true = move at the end of the match
               new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.XR.XRSettings), "get_enabled")),
               new CodeMatch(OpCodes.Brtrue),
               new CodeMatch(OpCodes.Ldarg_0),
               new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(MainCameraControl), "cameraOffsetTransform")),
               new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_localPosition")),
               new CodeMatch(OpCodes.Stloc_S));

            if (cm.IsValid)
            {
                cm.Advance(1);

                CodeInstruction[] codeInstructions = new[] {
                    new CodeInstruction(OpCodes.Ldsfld,AccessTools.Field(typeof(Player), nameof(Player.main))),
                    new CodeInstruction(OpCodes.Callvirt,AccessTools.Method(typeof(Player),nameof(Player.GetPDA))),
                    new CodeInstruction(OpCodes.Callvirt,AccessTools.PropertyGetter(typeof(PDA),nameof(PDA.isInUse))),
                    new CodeInstruction(OpCodes.Brtrue, cm.Instruction.operand) };

                cm.Advance(1);

                cm.Insert(codeInstructions);
            }
            else
            {
                Plugin.Logger.LogError("Unable to patch MainCameraControl.OnUpdate().");
            }

            return cm.InstructionEnumeration();
        }
    }
}

[HarmonyPatch]
public static class Patch_FOVChange
{
    [HarmonyPatch(typeof(PDACameraFOVControl), nameof(PDACameraFOVControl.Update))]
    public static class Patch_PDACameraFOVControl_Update
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}

[HarmonyPatch]
public static class Patch_ResetCameraHorizontalView
{
    [HarmonyPatch(typeof(MainCameraControl), nameof(MainCameraControl.OnUpdate))]
    public static class Patch_MainCameraControl_OnUpdate
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //
            // PATCH 1
            //

            /*
            IL_01b6: br           IL_0259

            // [242 7 - 242 45]
            IL_01bb: ldarg.0      // this
            IL_01bc: ldfld        class [UnityEngine.CoreModule]UnityEngine.Transform MainCameraControl::cameraOffsetTransform
            IL_01c1: stloc.s      transform
            */
            CodeMatcher cm = new(instructions);

            /* Find:
            { 
                this.cameraOffsetTransform.localEulerAngles = UWE.Utils.LerpEuler(this.cameraOffsetTransform.localEulerAngles, Vector3.zero, deltaTime * 5f);
                // IL_01b6: br           IL_0259
            }
            else
            {
                transform = this.cameraOffsetTransform;
                this.rotationY = Mathf.LerpAngle(this.rotationY, 0.0f, PDA.deltaTime* 15f);
                this.transform.localEulerAngles = new Vector3(Mathf.LerpAngle(this.transform.localEulerAngles.x, 0.0f, PDA.deltaTime* 15f), this.transform.localEulerAngles.y, 0.0f);
                this.cameraUPTransform.localEulerAngles = UWE.Utils.LerpEuler(this.cameraUPTransform.localEulerAngles, Vector3.zero, PDA.deltaTime* 15f);
            }
            */

            cm.MatchForward(false, // false = move at the start of the match, true = move at the end of the match
                   new CodeMatch(OpCodes.Br), // IL_01b6: br           IL_0259
                   new CodeMatch(OpCodes.Ldarg_0),
                   new CodeMatch(OpCodes.Ldfld),
                   new CodeMatch(OpCodes.Stloc_S),
                   new CodeMatch(OpCodes.Ldarg_0),
                   new CodeMatch(OpCodes.Ldarg_0));

            if (cm.IsValid)
            {
                CodeInstruction br = new CodeInstruction(cm.Opcode, cm.Operand);
                cm.Advance(1); // IL_01bb: ldarg.0      // this
                br.MoveLabelsFrom(cm.Instruction);
                cm.Insert(br);
            }
            else
            {
                Plugin.Logger.LogError("Patch_ResetCameraHorizontalView (patch 1) -- Unable to patch MainCameraControl.OnUpdate().");
            }

            //
            // PATCH 2
            //

            /*
            IL_0464: ldloc.s      flag6
            IL_0466: brfalse.s    IL_0489

            // PATCH HERE

            // [286 11 - 286 92]
            IL_0468: ldarg.0      // this
            IL_0469: ldarg.0      // this
            IL_046a: ldfld        float32 MainCameraControl::camRotationY
            IL_046f: ldc.r4       0.0
            IL_0474: call         float32 PDA::get_deltaTime()
            IL_0479: ldc.r4       10
            IL_047e: mul
            IL_047f: call         float32 [UnityEngine.CoreModule]UnityEngine.Mathf::LerpAngle(float32, float32, float32)
            IL_0484: stfld        float32 MainCameraControl::camRotationY
            */

            /* Find:
            if (flag6)
			{
			  this.camRotationY = Mathf.LerpAngle(this.camRotationY, 0f, PDA.deltaTime * 10f);
			}
            */

            /* After patch:
            if (flag6 && !Player.main.GetPDA().isInUse)
            {
              this.camRotationY = Mathf.LerpAngle(this.camRotationY, 0f, PDA.deltaTime * 10f);
            }
            */

            /*
            cm.Start();
            cm.MatchForward(false, // false = move at the start of the match, true = move at the end of the match
                new CodeMatch(OpCodes.Ldloc_S));
            */
            
            cm.MatchForward(false, // false = move at the start of the match, true = move at the end of the match
                new CodeMatch(OpCodes.Ldloc_S), // IL_0464: ldloc.s      flag6
                new CodeMatch(OpCodes.Brfalse),
                new CodeMatch(OpCodes.Ldarg_0), // this
                new CodeMatch(OpCodes.Ldarg_0), // this
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(MainCameraControl), nameof(MainCameraControl.camRotationY))),
                new CodeMatch(OpCodes.Ldc_R4),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(PDA), nameof(PDA.deltaTime))),
                new CodeMatch(OpCodes.Ldc_R4),
                new CodeMatch(OpCodes.Mul));
            
            if (cm.IsValid)
            {
                cm.Advance(1); // IL_0466: brfalse.s    IL_0489

                CodeInstruction[] codeInstructions = new[] {
                    new CodeInstruction(OpCodes.Ldsfld,AccessTools.Field(typeof(Player), nameof(Player.main))),
                    new CodeInstruction(OpCodes.Callvirt,AccessTools.Method(typeof(Player),nameof(Player.GetPDA))),
                    new CodeInstruction(OpCodes.Callvirt,AccessTools.PropertyGetter(typeof(PDA),nameof(PDA.isInUse))),
                    new CodeInstruction(OpCodes.Brtrue, cm.Instruction.operand) };

                cm.Advance(1); // IL_0468: ldarg.0      // this

                cm.Insert(codeInstructions);
            }
            else
            {
                Plugin.Logger.LogError("Patch_ResetCameraHorizontalView (patch 2) -- Unable to patch MainCameraControl.OnUpdate().");
            }

            return cm.InstructionEnumeration();
        }
    }
}
