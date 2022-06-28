using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace D2Map.Core.Models
{
    public class Rect
    {
        public Rect(int x0, int y0, int x1, int y1)
        {
            this.x0 = x0;
            this.x1 = x1;
            this.y0 = y0;
            this.y1 = y1;
        }

        public int x0 { get; set; }

        public int y0 { get; set; }

        public int x1 { get; set; }

        public int y1 { get; set; }
    };
    public class Size
    {
        public Size(uint x, uint y)
        {
            width = x;
            height = y;
        }

        public uint width { get; set; }

        public uint height { get; set; }
    }

    public class Exit
    {
        public List<Point> offsets;
        public bool isPortal = false;
    };
    public class CollisionMap
    {
        public Point LevelOrigin { get; set; }
        [JsonPropertyName("mapRows")]
        public List<List<int>> Map { get; set; }
        public Dictionary<string, AdjacentLevel> AdjacentLevels { get; set; } = new Dictionary<string, AdjacentLevel>();
        public Dictionary<string, List<Point>> Npcs { get; set; } = new Dictionary<string, List<Point>>();
        public Dictionary<string, List<Point>> Objects { get; set; } = new Dictionary<string, List<Point>>();
        public Area? TombArea { get; set; }

        public bool built = true;
        public Point offset = new Point(0, 0);
        /* Collission maps are cropped in rect [crop.x0, crop.y0] to [crop.x1, crop.y1] relative to [offset.x, offset.y] */
        public Rect crop = new Rect(-1, -1, -1, -1);
        public Size size = new Size(0, 0);
        public Dictionary<string, Exit> exits { get; set; } = new Dictionary<string, Exit>();
        public List<int> mapData { get; set; }
    };
}
