﻿using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

// ReSharper disable UnusedMember.Local
namespace BetterChat.Patches
{
    [HarmonyPatch(typeof(DevConsole),"Update")]
    class Patch_Update_dev
    {
        private static bool Prefix()
        {
            if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.Return) && (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)))
            {
                return true;
            }
            if (Input.GetKeyDown(KeyCode.Return) && PhotonNetwork.IsConnected && BetterChat.chatHidden)
            {
                BetterChat.OpenChatForGroup("ALL", BetterChat.chatGroupsDict["ALL"]);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(DevConsole), "Send")]
    class Patch_send_dev
    {
        private static bool Prefix(DevConsole __instance, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            if (Application.isEditor || (GM_Test.instance && GM_Test.instance.gameObject.activeSelf))
            {
                typeof(DevConsole).InvokeMember("SpawnCard", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, MenuControllerHandler.instance.GetComponent<DevConsole>(), new object[]{message});
            }
            if (Application.isEditor)
            {
                typeof(DevConsole).InvokeMember("SpawnMap", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, MenuControllerHandler.instance.GetComponent<DevConsole>(), new object[]{message});
            }
            
            var viewID = ((Player) typeof(PlayerManager).InvokeMember("GetPlayerWithActorID",
                BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null,
                PlayerManager.instance, new object[] {PhotonNetwork.LocalPlayer.ActorNumber})).data.view.ViewID;
            
            __instance.GetComponent<PhotonView>().RPC("RPCA_SendChat", RpcTarget.Others, new object[]
            {
                "‌" + message,
                viewID
            });
            return false;
        }
    }
    
    [HarmonyPatch(typeof(DevConsole), "RPCA_SendChat")]
    class Patch_rpca_dev
    {
        private static bool Prefix(string message, int playerViewID)
        {
            if (message.Contains("‌")) return false;
            
            var player = PhotonNetwork.GetPhotonView(playerViewID);
            if (player.IsMine) return false;
            MenuControllerHandler.instance.GetComponent<PhotonView>().RPC(nameof(ChatMonoGameManager.Instance.RPCA_CreateMessage), RpcTarget.All,player.Owner.NickName, player.GetComponent<Player>().teamID, message, "ALL", player.GetComponent<Player>().playerID);
            return false;
        }
    }
}