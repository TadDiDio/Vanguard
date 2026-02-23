using System;
using System.Collections.Generic;
using Steamworks;

namespace LobbyService.Example.Steam
{
    public class SteamBrowserFilterProvider : IBrowserFilterProvider
    {
        private Dictionary<string, LobbyNumberFilter> _numberFilters = new();
        private Dictionary<string, string> _stringFilters = new();
        private ELobbyDistanceFilter? _distanceFilter;
        private int? _slotsAvailableFilter;
        private int? _limitResponsesFilter;

        public BrowserFilterCapabilities FilterCapabilities { get; } = new BrowserFilterCapabilities
        {
            SupportsDistanceFiltering = true,
            SupportsResponseLimit = true,
            SupportsNumberFiltering = true,
            SupportsStringFiltering = true,
            SupportsSlotsAvailable = true,
        };
        
        public void ApplyFilters()
        {
            foreach (var kvp in _numberFilters)
            {
                ELobbyComparison comparison = kvp.Value.ComparisonType switch
                {
                    ComparisonType.NotEqual           => ELobbyComparison.k_ELobbyComparisonNotEqual,
                    ComparisonType.LessThan           => ELobbyComparison.k_ELobbyComparisonLessThan,
                    ComparisonType.LessThanOrEqual    => ELobbyComparison.k_ELobbyComparisonEqualToOrLessThan,
                    ComparisonType.Equal              => ELobbyComparison.k_ELobbyComparisonEqual,
                    ComparisonType.GreaterThan        => ELobbyComparison.k_ELobbyComparisonGreaterThan,
                    ComparisonType.GreaterThanOrEqual => ELobbyComparison.k_ELobbyComparisonEqualToOrGreaterThan,
                    _                                 => throw new ArgumentOutOfRangeException()
                };

                SteamMatchmaking.AddRequestLobbyListNumericalFilter(kvp.Key, kvp.Value.Value, comparison);
            }

            foreach (var kvp in _stringFilters)
            {
                SteamMatchmaking.AddRequestLobbyListStringFilter(kvp.Key, kvp.Value, ELobbyComparison.k_ELobbyComparisonEqual);
            }

            if (_distanceFilter.HasValue)
            {
                SteamMatchmaking.AddRequestLobbyListDistanceFilter(_distanceFilter.Value);
            }

            if (_slotsAvailableFilter.HasValue)
            {
                SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(_slotsAvailableFilter.Value);
            }

            if (_limitResponsesFilter.HasValue)
            {
                SteamMatchmaking.AddRequestLobbyListResultCountFilter(_limitResponsesFilter.Value);
            }
        }
        
        public void AddNumberFilter(LobbyNumberFilter filter)
        {
            _numberFilters[filter.Key] = filter;
        }

        public void AddStringFilter(LobbyStringFilter filter)
        {
            _stringFilters[filter.Key] = filter.Value;
        }

        public void RemoveNumberFilter(string key)
        {
            _numberFilters.Remove(key);
        }

        public void RemoveStringFilter(string key)
        {
            _stringFilters.Remove(key);
        }

        public void RemoveFilter(string key)
        {
            _numberFilters.Remove(key);
            _stringFilters.Remove(key);
        }

        public void SetDistanceFilter(LobbyDistance value)
        {
            _distanceFilter = value switch
            {
                LobbyDistance.Default   => ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault,
                LobbyDistance.Near      => ELobbyDistanceFilter.k_ELobbyDistanceFilterClose,
                LobbyDistance.Far       => ELobbyDistanceFilter.k_ELobbyDistanceFilterFar,
                LobbyDistance.WorldWide => ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide,
                _                       => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }

        public void ClearDistanceFilter()
        {
            _distanceFilter = null;
        }

        public void SetSlotsAvailableFilter(int slots)
        {
            _slotsAvailableFilter = slots;
        }

        public void ClearSlotsAvailableFilter()
        {
            _slotsAvailableFilter = null;
        }

        public void SetLimitResponsesFilter(int limit)
        {
            _limitResponsesFilter = limit;
        }

        public void ClearLimitResponsesFilter()
        {
            _limitResponsesFilter = null;
        }

        public void ClearAllFilters()
        {
            _numberFilters.Clear();
            _stringFilters.Clear();

            _distanceFilter = null;
            _limitResponsesFilter = null;
            _slotsAvailableFilter = null;
        }
    }
}