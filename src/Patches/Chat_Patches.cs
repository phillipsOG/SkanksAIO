using System.Threading.Tasks;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using SkanksAIO;
using Unity.Collections;
using Unity.Entities;

[HarmonyPatch]
public static class Chat_Pathces
{
    [HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
    [HarmonyPrefix]
    public static bool ChatUpdatePatch(ChatMessageSystem __instance)
    {
        try
        {
            var em = __instance.EntityManager;
            var query = __instance._ChatMessageQuery.ToEntityArray(Allocator.Temp);

            foreach (var entity in query)
            {
                if (!App.Instance!.Chat.Handle(entity))
                {
                    // Hide the chat messages
                    // Handle removing of message from query

                    // return false; <-- Add this once the message is removed from the query.
                    // Otherwise it will spam the command output in the chat.

                    // Avii's note: You *should not* return from a foreach loop, there might be more entries left, what happens with those?
                    // It's a performance hit, if it needs to run through this entire thing again for the remaining entries.
                    // Although I don't know how this stuff works under the hood, it'd be better to find a way without a return inside the loop.
                }
                
                SendMessageToDiscord(em, entity);
            }
            return true;
        }
        catch
        {
            return true;
        }
    }

    private static void SendMessageToDiscord(EntityManager em, Entity entity)
    {
        var chatMessageEvent = em.GetComponentData<ChatMessageEvent>(entity);
        if (chatMessageEvent.MessageType == ChatMessageType.Global)
        {
            var fromCharacter = em.GetComponentData<FromCharacter>(entity);
            var user = em.GetComponentData<User>(fromCharacter.User);
            Plugin.Logger?.LogDebug($"[Chat] {user.CharacterName}: {chatMessageEvent.MessageText}");
            var _ = App.Instance!.Discord.SendMessageAsync($"[GLOBAL] {user.CharacterName}: {chatMessageEvent.MessageText}");
        }
    }
}