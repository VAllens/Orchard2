﻿using System.Collections.Generic;
using Orchard.Autoroute.Model;

namespace Orchard.Autoroute.Services
{
    public interface IAutorouteEntries
    {
        bool TryGetContentItemId(string path, out int contentItemId);
        bool TryGetPath(int contentItemId, out string path);
        void AddEntries(IEnumerable<AutorouteEntry> entries);
        void RemoveEntries(IEnumerable<AutorouteEntry> entries);
    }

    public static class AutorouteEntriesExtensions
    {
        public static void AddEntry(this IAutorouteEntries entries, int contentItemId, string path)
        {
            entries.AddEntries(new[] { new AutorouteEntry { ContentItemId = contentItemId, Path = path } }); 
        }

        public static void RemoveEntry(this IAutorouteEntries entries, int contentItemId, string path)
        {
            entries.RemoveEntries(new[] { new AutorouteEntry { ContentItemId = contentItemId, Path = path } });
        }
    }
}
