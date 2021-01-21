using System;
using System.Collections.Generic;
using System.Text;

namespace NJITSignHelper.PhyLocation
{
    class Location
    {
        public double lat, lon;
        public string locName;

        public Location(double lat,double lon)
        {
            this.lat = lat;
            this.lon = lon;
        }

        public static double operator -(Location a, Location b)
        {
            return a.DistanceTo(b);
        }

        /// <summary>
        /// 返回两点间距离 米
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public double DistanceTo(Location target)
        {
            double dLat1InRad = lat * (Math.PI / 180);
            double dLong1InRad = lon * (Math.PI / 180);
            double dLat2InRad = target.lat * (Math.PI / 180);
            double dLong2InRad = target.lon * (Math.PI / 180);
            double dLongitude = dLong2InRad - dLong1InRad;
            double dLatitude = dLat2InRad - dLat1InRad;
            double a = Math.Pow(Math.Sin(dLatitude / 2), 2) + Math.Cos(dLat1InRad) * Math.Cos(dLat2InRad) * Math.Pow(Math.Sin(dLongitude / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double dDistance = EarthRadiusKm * c;
            return dDistance;
        }

        public const double EarthRadiusKm = 6378.137; // WGS-84
    }
}
