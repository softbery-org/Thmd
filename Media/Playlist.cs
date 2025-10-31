// Version: 0.1.17.16
using System;
using System.Collections.Generic;

namespace Thmd.Media
{
    /// <summary>
    /// Represents a playlist of <see cref="VideoItem"/> objects.
    /// Provides navigation between current, next, and previous media, 
    /// as well as duration tracking and display utilities.
    /// </summary>
    public class Playlist : List<VideoItem>
    {
        private VideoItem _current;
        private VideoItem _next;
        private VideoItem _previous;
        private TimeSpan _playlistDuration = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the name of the playlist.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the playlist.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets the list of media items in the playlist.
        /// </summary>
        public List<VideoItem> Media { get; private set; }

        /// <summary>
        /// Gets the current media item.
        /// </summary>
        public VideoItem Current
        {
            get => _current;
            private set => _current = value;
        }

        /// <summary>
        /// Gets the next media item.
        /// </summary>
        public VideoItem Next
        {
            get => _next;
            private set => _next = value;
        }

        /// <summary>
        /// Gets the previous media item.
        /// </summary>
        public VideoItem Previous
        {
            get => _previous;
            private set => _previous = value;
        }

        /// <summary>
        /// Gets the index of the current media item in the playlist.
        /// </summary>
        public int CurrentIndex => Media.IndexOf(Current);

        /// <summary>
        /// Gets the index of the next media item in the playlist.
        /// </summary>
        public int NextIndex
        {
            get
            {
                int index = Media.IndexOf(Current) + 1;
                return index >= Media.Count ? 0 : index;
            }
        }

        /// <summary>
        /// Gets the index of the previous media item in the playlist.
        /// </summary>
        public int PreviousIndex
        {
            get
            {
                int index = Media.IndexOf(Current) - 1;
                return index < 0 ? Media.Count - 1 : index;
            }
        }

        /// <summary>
        /// Gets the creation date of the playlist.
        /// </summary>
        public DateTime CreationDate { get; private set; }

        /// <summary>
        /// Gets or sets the title of the playlist (alias for <see cref="Name"/>).
        /// </summary>
        public string Title
        {
            get => Name;
            set => Name = value;
        }

        /// <summary>
        /// Gets the total duration of the playlist.
        /// </summary>
        public TimeSpan Duration => _playlistDuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Playlist"/> class with a name and optional description.
        /// </summary>
        /// <param name="name">The name of the playlist.</param>
        /// <param name="description">The description of the playlist.</param>
        public Playlist(string name, string description = "")
        {
            Name = name;
            Description = description;
            Media = new List<VideoItem>();
            CreationDate = DateTime.Now;
        }

        /// <summary>
        /// Adds a media item to the playlist.
        /// </summary>
        /// <param name="media">The <see cref="VideoItem"/> to add.</param>
        public new void Add(VideoItem media)
        {
            if (media != null)
            {
                Media.Add(media);
                Console.WriteLine($"Media \"{media.Name}\" has been added to the playlist \"{Name}\".");
            }
            else
            {
                Console.WriteLine("Cannot add a null media.");
            }
        }

        /// <summary>
        /// Removes a media item from the playlist.
        /// </summary>
        /// <param name="media">The <see cref="VideoItem"/> to remove.</param>
        public void RemoveMedia(VideoItem media)
        {
            if (media != null)
            {
                if (Media.Remove(media))
                {
                    Console.WriteLine($"Song \"{media.Name}\" has been removed from the playlist \"{Name}\".");
                }
                else
                {
                    Console.WriteLine($"Song \"{media.Name}\" was not found in the playlist \"{Name}\".");
                }
            }
            else
            {
                Console.WriteLine("Cannot remove a null media.");
            }
        }

        /// <summary>
        /// Displays all media items in the playlist.
        /// </summary>
        public void DisplayMedia()
        {
            if (Media.Count == 0)
            {
                Console.WriteLine($"Playlist \"{Name}\" is empty.");
                return;
            }

            Console.WriteLine($"Media in playlist \"{Name}\":");
            foreach (VideoItem media in Media)
            {
                Console.WriteLine($"- ({media})");
            }
        }

        /// <summary>
        /// Gets the number of media items in the playlist.
        /// </summary>
        /// <returns>The count of media items.</returns>
        public int GetMediaCount() => Media.Count;

        /// <summary>
        /// Calculates the total duration of all media items in the playlist.
        /// </summary>
        /// <returns>The total duration as a <see cref="TimeSpan"/>.</returns>
        public TimeSpan GetTotalDuration()
        {
            TimeSpan totalDuration = TimeSpan.Zero;
            foreach (VideoItem media in Media)
            {
                totalDuration += media.Duration;
            }
            return totalDuration;
        }
    }
}
