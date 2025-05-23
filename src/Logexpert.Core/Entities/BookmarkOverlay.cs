using System.Drawing;

namespace LogExpert.Core.Entities
{
    public class BookmarkOverlay
    {
        #region Properties

        public Bookmark Bookmark { get; set; }

        public Point Position { get; set; }

        public Rectangle BubbleRect { get; set; }

        #endregion
    }
}