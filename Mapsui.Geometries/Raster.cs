using System;
using System.IO;
using Mapsui.Geometries.WellKnownBinary;
using Mapsui.Geometries.WellKnownText;

namespace Mapsui.Geometries
{
    public class Raster : Geometry, IRaster
    {
        private readonly BoundingBox _boundingBox;

        public Raster(MemoryStream data, BoundingBox box)
        {
            Data = data;
            _boundingBox = box;
            TickFetched = DateTime.Now.Ticks;
        }

        public MemoryStream Data { get; }
        public long TickFetched { get; }

        public override BoundingBox GetBoundingBox()
        {
            return _boundingBox;
        }

        public new string AsText()
        {
            return GeometryToWKT.Write(Envelope());
        }

        public new byte[] AsBinary()
        {
            return GeometryToWKB.Write(Envelope());
        }

        public override bool IsEmpty()
        {
            return _boundingBox.Width*_boundingBox.Height <= 0;
        }

        public new Geometry Clone()
        {
            var copy = new MemoryStream();
            Data.Position = 0;
            Data.CopyTo(copy);
            return new Raster(copy, _boundingBox.Clone());
        }
        
        public override double Distance(Point point)
        {
            throw new NotImplementedException();
        }
    }
}