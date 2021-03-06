﻿using Mapsui.Geometries;
using Mapsui.Geometries.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using Mapsui.Fetcher;
using Mapsui.Logging;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia;

#if __ANDROID__
namespace Mapsui.UI.Android
#elif __IOS__
namespace Mapsui.UI.iOS
#elif __UWP__
namespace Mapsui.UI.Uwp
#elif __FORMS__
namespace Mapsui.UI
#else
namespace Mapsui.UI.Wpf
#endif
{
    public partial class MapControl
    {
        private Map _map;

        /// <summary>
        /// Saver for center before last pinch movement
        /// </summary>
        private Point _previousCenter = new Point();
        
        /// <summary>
        /// Allow map panning through touch or mouse
        /// </summary>
        public bool PanLock { get; set; }

        /// <summary>
        /// Allow a rotation with a pinch gesture
        /// </summary>
        public bool RotationLock { get; set; } = true;

        /// <summary>
        /// Allow zooming though touch or mouse
        /// </summary>
        public bool ZoomLock { get; set; }

        /// <summary>
        /// After how many degrees start rotation to take place
        /// </summary>
        public double UnSnapRotationDegrees { get; set; }

        /// <summary>
        /// With how many degrees from 0 should map snap to 0 degrees
        /// </summary>
        public double ReSnapRotationDegrees { get; set; }
        
        public IRenderer Renderer { get; set; } = new MapRenderer();

        /// <summary>
        /// Unsubscribe from map events </summary>
        public void Unsubscribe()
        {
            UnsubscribeFromMapEvents(_map);
        }
        
        /// <summary>
        /// Converting function for world to screen respecting scale
        /// </summary>
        /// <param name="viewport">Viewport</param>
        /// <param name="scale">Scale</param>
        /// <param name="worldPosition">Position in world coordinates</param>
        /// <returns>Position in screen coordinates</returns>
        public Point WorldToScreen(IViewport viewport, float scale, Point worldPosition)
        {
            var screenPosition = viewport.WorldToScreen(worldPosition);
            return new Point(screenPosition.X * scale, screenPosition.Y * scale);
        }

        /// <summary>
        /// Converting function for screen to world respecting scale
        /// </summary>
        /// <param name="viewport">Viewport</param>
        /// <param name="scale">Scale</param>
        /// <param name="screenPosition">Position in screen coordinates</param>
        /// <returns>Position in world coordinates</returns>
        public Point ScreenToWorld(IViewport viewport, float scale, Point screenPosition)
        {
            var worldPosition = viewport.ScreenToWorld(screenPosition.X * scale, screenPosition.Y * scale);
            return new Point(worldPosition.X, worldPosition.Y);
        }

        private static (Point centre, double radius, double angle) GetPinchValues(List<Point> locations)
        {
            if (locations.Count < 2)
                throw new ArgumentException();

            double centerX = 0;
            double centerY = 0;

            foreach (var location in locations)
            {
                centerX += location.X;
                centerY += location.Y;
            }

            centerX = centerX / locations.Count;
            centerY = centerY / locations.Count;

            var radius = Algorithms.Distance(centerX, centerY, locations[0].X, locations[0].Y);

            var angle = Math.Atan2(locations[1].Y - locations[0].Y, locations[1].X - locations[0].X) * 180.0 / Math.PI;

            return (new Point(centerX, centerY), radius, angle);
        }

        /// <summary>
        /// Subscribe to map events
        /// </summary>
        /// <param name="map">Map, to which events to subscribe</param>
        private void SubscribeToMapEvents(Map map)
        {
            map.DataChanged += MapDataChanged;
            map.PropertyChanged += MapPropertyChanged;
            map.RefreshGraphics += MapRefreshGraphics;
        }

        /// <summary>
        /// Unsubcribe from map events
        /// </summary>
        /// <param name="map">Map, to which events to unsubscribe</param>
        private void UnsubscribeFromMapEvents(Map map)
        {
            var temp = map;
            if (temp != null)
            {
                temp.DataChanged -= MapDataChanged;
                temp.PropertyChanged -= MapPropertyChanged;
                temp.RefreshGraphics -= MapRefreshGraphics;
                temp.AbortFetch();
            }
        }

        public void Refresh()
        {
            RefreshData();
            RefreshGraphics();
        }

        private void MapDataChanged(object sender, DataChangedEventArgs e)
        {
            RunOnUIThread(() =>
            {
                try
                {
                    if (e == null)
                    {
                        Logger.Log(LogLevel.Warning, "Unexpected error: DataChangedEventArgs can not be null");
                    }
                    else if (e.Cancelled)
                    {
                        Logger.Log(LogLevel.Warning, "Fetching data was cancelled", e.Error);
                    }
                    else if (e.Error is WebException)
                    {
                        Logger.Log(LogLevel.Warning, "A WebException occurred. Do you have internet?", e.Error);
                    }
                    else if (e.Error != null)
                    {
                        Logger.Log(LogLevel.Warning, "An error occurred while fetching data", e.Error);
                    }
                    else // no problems
                    {
                        RefreshGraphics();
                    }
                }
                catch (Exception exception)
                {
                    Logger.Log(LogLevel.Warning, $"Unexpected exception in {nameof(MapDataChanged)}", exception);
                }
            });
        }

        private void MapPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Layers.Layer.Enabled))
            {
                RefreshGraphics();
            }
            else if (e.PropertyName == nameof(Layers.Layer.Opacity))
            {
                RefreshGraphics();
            }
        }

        public Map Map
        {
            get => _map;
            set
            {
                if (_map != null)
                {
                    UnsubscribeFromMapEvents(_map);
                    _map = null;
                }

                _map = value;

                if (_map != null)
                {
                    SubscribeToMapEvents(_map);
                    _map.RefreshData(true);
                }

                RefreshGraphics();
            }
        }

        public Point ToPixels(Point positionInDeviceIndependentUnits)
        {
            return new Point(
                positionInDeviceIndependentUnits.X / PixelsPerDeviceIndepententUnit,
                positionInDeviceIndependentUnits.Y / PixelsPerDeviceIndepententUnit);
        }
    }
}
