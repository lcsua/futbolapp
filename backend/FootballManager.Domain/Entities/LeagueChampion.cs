using System;
using System.Collections.Generic;
using System.Linq;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    /// <summary>
    /// A confirmed league title. Historical rows do not require a season, fixtures or statistics.
    /// The champion is always chosen explicitly; standings are never consulted.
    /// </summary>
    public class LeagueChampion : Entity
    {
        public const int MinYear = 1900;
        public const int MaxYear = 2100;
        public const int MaxChampionshipNameLength = 120;
        public const int MaxDivisionNameLength = 120;
        public const int MaxNotesLength = 4000;
        public const int MaxPhotos = 40;

        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; } = null!;

        public Guid TeamId { get; private set; }
        public virtual Team Team { get; private set; } = null!;

        public int Year { get; private set; }
        public string ChampionshipName { get; private set; } = string.Empty;
        public string DivisionName { get; private set; } = string.Empty;

        /// <summary>
        /// Stable duplicate key: year + normalized championship + normalized division.
        /// </summary>
        public string IdentityKey { get; private set; } = string.Empty;

        public Guid? SeasonId { get; private set; }
        public virtual Season? Season { get; private set; }

        public Guid? DivisionId { get; private set; }
        public virtual Division? Division { get; private set; }

        public DateOnly? WonAt { get; private set; }
        public string? Notes { get; private set; }

        private readonly List<LeagueChampionPhoto> _photos = new();
        public virtual IReadOnlyCollection<LeagueChampionPhoto> Photos => _photos.AsReadOnly();

        protected LeagueChampion() { }

        public LeagueChampion(
            League league,
            Team team,
            int year,
            string championshipName,
            string divisionName,
            Guid? seasonId = null,
            Guid? divisionId = null,
            DateOnly? wonAt = null,
            string? notes = null)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            Apply(team, year, championshipName, divisionName, seasonId, divisionId, wonAt, notes);
        }

        public void Update(
            Team team,
            int year,
            string championshipName,
            string divisionName,
            Guid? seasonId,
            Guid? divisionId,
            DateOnly? wonAt,
            string? notes)
        {
            Apply(team, year, championshipName, divisionName, seasonId, divisionId, wonAt, notes);
            UpdateTimestamp();
        }

        public IReadOnlyList<LeagueChampionPhoto> AddPhotos(IReadOnlyList<string> imageUrls)
        {
            if (imageUrls == null || imageUrls.Count == 0)
                throw new ArgumentException("At least one image is required.", nameof(imageUrls));

            var active = ActivePhotos().ToList();
            if (active.Count + imageUrls.Count > MaxPhotos)
                throw new ArgumentException($"A champion can have at most {MaxPhotos} photos.", nameof(imageUrls));

            var hasCover = active.Any(p => p.IsCover);
            var order = active.Count == 0 ? -1 : active.Max(p => p.DisplayOrder);
            var created = new List<LeagueChampionPhoto>(imageUrls.Count);

            foreach (var imageUrl in imageUrls)
            {
                order++;
                var isCover = !hasCover;
                hasCover = true;
                var photo = new LeagueChampionPhoto(this, imageUrl, caption: null, order, isCover);
                _photos.Add(photo);
                created.Add(photo);
            }

            UpdateTimestamp();
            return created;
        }

        public void EnsureGallery(IReadOnlyList<LeagueChampionPhotoUpdate> updates)
        {
            var active = ActivePhotos().ToList();
            if (updates == null)
                throw new ArgumentNullException(nameof(updates));
            if (updates.Count != active.Count)
                throw new ArgumentException("The gallery must include every current photo.", nameof(updates));

            var ids = updates.Select(u => u.PhotoId).ToList();
            if (ids.Distinct().Count() != ids.Count)
                throw new ArgumentException("Each photo can appear only once.", nameof(updates));

            if (ids.Any(id => active.All(p => p.Id != id)))
                throw new ArgumentException("A photo does not belong to this champion.", nameof(updates));

            if (updates.Count(u => u.IsCover) > 1)
                throw new ArgumentException("Only one cover photo is allowed.", nameof(updates));
        }

        public void ApplyGallery(IReadOnlyList<LeagueChampionPhotoUpdate> updates)
        {
            EnsureGallery(updates);
            var active = ActivePhotos().ToList();
            var byId = active.ToDictionary(p => p.Id);

            var ordered = updates
                .OrderBy(u => u.DisplayOrder)
                .ThenBy(u => u.PhotoId)
                .ToList();

            for (var index = 0; index < ordered.Count; index++)
            {
                var update = ordered[index];
                byId[update.PhotoId].ApplyMetadata(update.Caption, index, update.IsCover);
            }

            UpdateTimestamp();
        }

        public string RemovePhoto(Guid photoId)
        {
            var photo = ActivePhotos().FirstOrDefault(p => p.Id == photoId)
                ?? throw new KeyNotFoundException("Photo not found.");

            var imageUrl = photo.ImageUrl;
            photo.ApplyMetadata(photo.Caption, photo.DisplayOrder, isCover: false);
            photo.SoftDelete();
            UpdateTimestamp();
            return imageUrl;
        }

        /// <summary>
        /// Drops the optional season/division link and keeps the stored year and names.
        /// Used when a linked division is removed, so the title itself is preserved.
        /// </summary>
        public void ClearStructuralLink()
        {
            if (SeasonId == null && DivisionId == null)
                return;

            SeasonId = null;
            DivisionId = null;
            UpdateTimestamp();
        }

        public IReadOnlyList<string> SoftDeleteWithPhotos()
        {
            var urls = ActivePhotos().Select(p => p.ImageUrl).ToList();
            foreach (var photo in ActivePhotos().ToList())
            {
                photo.ApplyMetadata(photo.Caption, photo.DisplayOrder, isCover: false);
                photo.SoftDelete();
            }

            SoftDelete();
            return urls;
        }

        public static string NormalizeLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var parts = value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', parts);
        }

        public static string BuildIdentityKey(int year, string championshipName, string divisionName)
            => $"{year}|{NormalizeLabel(championshipName).ToLowerInvariant()}|{NormalizeLabel(divisionName).ToLowerInvariant()}";

        private void Apply(
            Team team,
            int year,
            string championshipName,
            string divisionName,
            Guid? seasonId,
            Guid? divisionId,
            DateOnly? wonAt,
            string? notes)
        {
            if (team == null)
                throw new ArgumentNullException(nameof(team));
            if (team.LeagueId != LeagueId)
                throw new ArgumentException("Team does not belong to this league.", nameof(team));

            if (year < MinYear || year > MaxYear)
                throw new ArgumentOutOfRangeException(nameof(year), $"Year must be between {MinYear} and {MaxYear}.");

            var championship = NormalizeLabel(championshipName);
            if (string.IsNullOrEmpty(championship))
                throw new ArgumentException("Championship name cannot be empty.", nameof(championshipName));
            if (championship.Length > MaxChampionshipNameLength)
                throw new ArgumentException($"Championship name cannot exceed {MaxChampionshipNameLength} characters.", nameof(championshipName));

            var division = NormalizeLabel(divisionName);
            if (string.IsNullOrEmpty(division))
                throw new ArgumentException("Division name cannot be empty.", nameof(divisionName));
            if (division.Length > MaxDivisionNameLength)
                throw new ArgumentException($"Division name cannot exceed {MaxDivisionNameLength} characters.", nameof(divisionName));

            if (wonAt.HasValue && (wonAt.Value.Year < MinYear || wonAt.Value.Year > MaxYear))
                throw new ArgumentOutOfRangeException(nameof(wonAt), $"WonAt year must be between {MinYear} and {MaxYear}.");

            var normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            if (normalizedNotes != null && normalizedNotes.Length > MaxNotesLength)
                throw new ArgumentException($"Notes cannot exceed {MaxNotesLength} characters.", nameof(notes));

            if (seasonId.HasValue != divisionId.HasValue && seasonId.HasValue)
                throw new ArgumentException("A linked championship requires both a season and a division.");

            Team = team;
            TeamId = team.Id;
            Year = year;
            ChampionshipName = championship;
            DivisionName = division;
            IdentityKey = BuildIdentityKey(year, championship, division);
            SeasonId = seasonId;
            DivisionId = divisionId;
            WonAt = wonAt;
            Notes = normalizedNotes;
        }

        private IEnumerable<LeagueChampionPhoto> ActivePhotos()
            => _photos.Where(p => p.DeletedAt == null);
    }

    public sealed class LeagueChampionPhotoUpdate
    {
        public LeagueChampionPhotoUpdate(Guid photoId, string? caption, int displayOrder, bool isCover)
        {
            PhotoId = photoId;
            Caption = caption;
            DisplayOrder = displayOrder;
            IsCover = isCover;
        }

        public Guid PhotoId { get; }
        public string? Caption { get; }
        public int DisplayOrder { get; }
        public bool IsCover { get; }
    }
}
