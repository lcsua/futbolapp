using System;
using System.Collections.Generic;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class Division : Entity
    {
        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; }

        public string Name { get; private set; }
        public string Slug { get; private set; }
        public string Description { get; private set; }

        /// <summary>
        /// When enabled, generated fixtures must not use a kickoff time inside
        /// [<see cref="KickoffRestrictionStart"/>, <see cref="KickoffRestrictionEnd"/>) (e.g. heat hours for senior divisions).
        /// </summary>
        public bool KickoffRestrictionEnabled { get; private set; }

        public TimeOnly? KickoffRestrictionStart { get; private set; }
        public TimeOnly? KickoffRestrictionEnd { get; private set; }

        protected Division() { }

        public Division(
            League league,
            string name,
            string slug,
            string description = null,
            bool kickoffRestrictionEnabled = false,
            TimeOnly? kickoffRestrictionStart = null,
            TimeOnly? kickoffRestrictionEnd = null)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Division name cannot be empty.", nameof(name));
            Slug = !string.IsNullOrWhiteSpace(slug) ? slug.ToLowerInvariant() : throw new ArgumentException("Division slug cannot be empty.", nameof(slug));
            Description = description;
            ApplyKickoffRestriction(kickoffRestrictionEnabled, kickoffRestrictionStart, kickoffRestrictionEnd);
        }

        public void UpdateDetails(string name, string slug, string description = null)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Division name cannot be empty.", nameof(name));
            Slug = !string.IsNullOrWhiteSpace(slug) ? slug.ToLowerInvariant() : throw new ArgumentException("Division slug cannot be empty.", nameof(slug));
            Description = description;
            UpdateTimestamp();
        }

        public void SetKickoffRestriction(bool enabled, TimeOnly? start, TimeOnly? end)
        {
            ApplyKickoffRestriction(enabled, start, end);
            UpdateTimestamp();
        }

        /// <summary>
        /// Returns true if <paramref name="kickoff"/> may not be used for this division (falls in the blocked window).
        /// </summary>
        public bool IsKickoffInBlockedWindow(TimeOnly kickoff)
        {
            if (!KickoffRestrictionEnabled || KickoffRestrictionStart == null || KickoffRestrictionEnd == null)
                return false;
            var s = KickoffRestrictionStart.Value;
            var e = KickoffRestrictionEnd.Value;
            return kickoff >= s && kickoff < e;
        }

        private void ApplyKickoffRestriction(bool enabled, TimeOnly? start, TimeOnly? end)
        {
            if (!enabled)
            {
                KickoffRestrictionEnabled = false;
                KickoffRestrictionStart = null;
                KickoffRestrictionEnd = null;
                return;
            }

            if (start == null || end == null)
                throw new ArgumentException("Kickoff restriction start and end are required when the restriction is enabled.");

            if (start.Value >= end.Value)
                throw new ArgumentException("Kickoff restriction end must be after start (same day).");

            KickoffRestrictionEnabled = true;
            KickoffRestrictionStart = start;
            KickoffRestrictionEnd = end;
        }
    }
}
