using System.Collections.Generic;
using LobbyService;
using PurrNet;
using PurrNet.Packing;
using Unity.VisualScripting;
using UnityEngine;

namespace Vanguard
{
    public enum Team
    {
        Marines,
        Aliens
    }
    
    public class TeamHelper
    {
        private const string Delimiter = ":";

        private static string GetTeamKey(Team team) => team == Team.Marines ? ApplicationController.MarineTeamKey : ApplicationController.AlienTeamKey;
        
        public static HashSet<string> GetTeamIds(Team team)
        {
            var key = GetTeamKey(team);
            
            var teamRaw = Lobby.GetLobbyDataOrDefault(key, "");
            return teamRaw.Split(Delimiter).ToHashSet();
        }

        public static void AddMemberToTeam(LobbyMember member, Team team)
        {
            var key = GetTeamKey(team);
            
            string list = Lobby.GetLobbyDataOrDefault(key, "");

            var newList = $"{list}{member.Id}{Delimiter}";
            Lobby.SetLobbyData(key, newList);
        }

        public static void RemoveMemberFromTeam(LobbyMember member, Team team)
        {
            var key = GetTeamKey(team);
            
            string list = Lobby.GetLobbyDataOrDefault(key, "");

            list = list.Replace(member.Id.ToString(), "");
            list = list.Replace($"{Delimiter}{Delimiter}", Delimiter);
            if (list == Delimiter) list = "";

            Lobby.SetLobbyData(key, list);
        }

        public static int GetTeamCount(Team team)
        {
            return GetTeamIds(team).Count;
        }

        public static Team GetMemberTeam(LobbyMember member)
        {
            return GetTeamIds(Team.Marines).Contains(member.Id.ToString()) ? Team.Marines : Team.Aliens;
        }
    }
}