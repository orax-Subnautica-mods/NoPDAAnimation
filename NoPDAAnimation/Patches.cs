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
            [PATCH]: br           IL_02b5   // add a jump to IL_02b5

            IL_0259: call         bool [UnityEngine.VRModule]UnityEngine.XR.XRSettings::get_enabled()
            IL_025e: brtrue.s     IL_02b5

            // [249 7 - 249 71]
            IL_0260: ldarg.0      // this
            IL_0261: ldfld        class [UnityEngine.CoreModule]UnityEngine.Transform MainCameraControl::cameraOffsetTransform
            IL_0266: callvirt     instance valuetype [UnityEngine.CoreModule]UnityEngine.Vector3 [UnityEngine.CoreModule]UnityEngine.Transform::get_localPosition()
            IL_026b: stloc.s      localPosition
            */

            CodeMatcher cm = new(instructions);

            // Find:
            // if (!XRSettings.enabled)
            // {
            //     Vector3 localPosition = this.cameraOffsetTransform.localPosition;
            //     localPosition.z = Mathf.Clamp(localPosition.z + (float)((double)PDA.deltaTime * (double)num1 * 0.25), 0.0f + this.camPDAZStart, this.camPDAZOffset + this.camPDAZStart);
            //     this.cameraOffsetTransform.localPosition = localPosition;
            // }
            cm.MatchForward(false, // false = move at the start of the match, true = move at the end of the match
               new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.XR.XRSettings), "get_enabled")),
               new CodeMatch(OpCodes.Brtrue),
               new CodeMatch(OpCodes.Ldarg_0),
               new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(MainCameraControl), "cameraOffsetTransform")),
               new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_localPosition")),
               new CodeMatch(OpCodes.Stloc_S));

            if (cm.IsValid)
            {
                CodeInstruction br = new CodeInstruction(OpCodes.Br, cm.InstructionAt(1).operand);
                br.MoveLabelsFrom(cm.Instruction);
                cm.Insert(br);
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
            /*
            IL_01b6: br           IL_0259

            // [242 7 - 242 45]
            IL_01bb: ldarg.0      // this
            IL_01bc: ldfld        class [UnityEngine.CoreModule]UnityEngine.Transform MainCameraControl::cameraOffsetTransform
            IL_01c1: stloc.s      transform
            */
            CodeMatcher cm = new(instructions);

            // Find:
            // { 
            //     this.cameraOffsetTransform.localEulerAngles = UWE.Utils.LerpEuler(this.cameraOffsetTransform.localEulerAngles, Vector3.zero, deltaTime * 5f);
            //     // IL_01b6: br           IL_0259
            // }
            // else
            // {
            //     transform = this.cameraOffsetTransform;
            //     this.rotationY = Mathf.LerpAngle(this.rotationY, 0.0f, PDA.deltaTime* 15f);
            //     this.transform.localEulerAngles = new Vector3(Mathf.LerpAngle(this.transform.localEulerAngles.x, 0.0f, PDA.deltaTime* 15f), this.transform.localEulerAngles.y, 0.0f);
            //     this.cameraUPTransform.localEulerAngles = UWE.Utils.LerpEuler(this.cameraUPTransform.localEulerAngles, Vector3.zero, PDA.deltaTime* 15f);
            // }
            cm.MatchForward(false,
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
                Plugin.Logger.LogError("Patch_ResetCameraHorizontalView -- Unable to patch MainCameraControl.OnUpdate().");
            }

            return cm.InstructionEnumeration();
        }
    }
}
