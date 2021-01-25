using System;
using System.Collections.Generic;
using System.Text;

namespace NJITSignHelper.PhyLocation
{
    public class GPSChange
    {
        private const double pi = 3.14159265358979324;
        private const double x_pi = 3.14159265358979324 * 3000.0 / 180.0;

        //克拉索天斯基椭球体参数值
        private const double a = 6378245.0;
        //第一偏心率
        private const double ee = 0.00669342162296594323;


        /// <summary>
        /// GCJ-02转换BD-09
        /// </summary>
        /// <param name="gg_lat">纬度</param>
        /// <param name="gg_lon">经度</param>
        /// <returns></returns>
        public static Location GCJ02_to_BD09(double gg_lat, double gg_lon)
        {
            double x = gg_lon, y = gg_lat;
            double z = Math.Sqrt(x * x + y * y) + 0.00002 * Math.Sin(y * x_pi);
            double theta = Math.Atan2(y, x) + 0.000003 * Math.Cos(x * x_pi);
            double bd_lon = z * Math.Cos(theta) + 0.0065;
            double bd_lat = z * Math.Sin(theta) + 0.006;
            return new Location(bd_lat,bd_lon);
        }

        /// <summary>
        /// BD-09转换GCJ-02
        /// </summary>
        /// <param name="bd_lat">纬度</param>
        /// <param name="bd_lon">经度</param>
        /// <returns></returns>
        public static Location BD09_to_GCJ02(double bd_lat, double bd_lon)
        {
            double x = bd_lon - 0.0065, y = bd_lat - 0.006;
            double z = Math.Sqrt(x * x + y * y) - 0.00002 * Math.Sin(y * x_pi);
            double theta = Math.Atan2(y, x) - 0.000003 * Math.Cos(x * x_pi);
            double gg_lon = z * Math.Cos(theta);
            double gg_lat = z * Math.Sin(theta);
            return new Location(gg_lat,gg_lon);
        }



        /// <summary>
        /// WGS-84转换GCJ-02
        /// </summary>
        /// <param name="wgLat">纬度</param>
        /// <param name="wgLon">经度</param>
        /// <returns></returns>
        public static Location WGS84_to_GCJ02(double wgLat, double wgLon)
        {
            if (OutOfChina(wgLat, wgLon))
            {
                return new Location(wgLat,wgLon);
            }
            double dLat = TransformLat(wgLon - 105.0, wgLat - 35.0);
            double dLon = TransformLon(wgLon - 105.0, wgLat - 35.0);
            double radLat = wgLat / 180.0 * pi;
            double magic = Math.Sin(radLat);
            magic = 1 - ee * magic * magic;
            double sqrtMagic = Math.Sqrt(magic);
            dLat = (dLat * 180.0) / ((a * (1 - ee)) / (magic * sqrtMagic) * pi);
            dLon = (dLon * 180.0) / (a / sqrtMagic * Math.Cos(radLat) * pi);
            double lat = wgLat + dLat;
            double lon = wgLon + dLon;
            return new Location(lat, lon);
        }


        public static void Transform(double wgLat, double wgLon, double[] latlng)
        {
            if (OutOfChina(wgLat, wgLon))
            {
                latlng[0] = wgLat;
                latlng[1] = wgLon;
                return;
            }
            double dLat = TransformLat(wgLon - 105.0, wgLat - 35.0);
            double dLon = TransformLon(wgLon - 105.0, wgLat - 35.0);
            double radLat = wgLat / 180.0 * pi;
            double magic = Math.Sin(radLat);
            magic = 1 - ee * magic * magic;
            double sqrtMagic = Math.Sqrt(magic);
            dLat = (dLat * 180.0) / ((a * (1 - ee)) / (magic * sqrtMagic) * pi);
            dLon = (dLon * 180.0) / (a / sqrtMagic * Math.Cos(radLat) * pi);
            latlng[0] = wgLat + dLat;
            latlng[1] = wgLon + dLon;
        }

        private static bool OutOfChina(double lat, double lon)
        {
            if (lon < 72.004 || lon > 137.8347)
                return true;
            if (lat < 0.8293 || lat > 55.8271)
                return true;
            return false;
        }

        private static double TransformLat(double x, double y)
        {
            double ret = -100.0 + 2.0 * x + 3.0 * y + 0.2 * y * y + 0.1 * x * y + 0.2 * Math.Sqrt(Math.Abs(x));
            ret += (20.0 * Math.Sin(6.0 * x * pi) + 20.0 * Math.Sin(2.0 * x * pi)) * 2.0 / 3.0;
            ret += (20.0 * Math.Sin(y * pi) + 40.0 * Math.Sin(y / 3.0 * pi)) * 2.0 / 3.0;
            ret += (160.0 * Math.Sin(y / 12.0 * pi) + 320 * Math.Sin(y * pi / 30.0)) * 2.0 / 3.0;
            return ret;
        }

        private static double TransformLon(double x, double y)
        {
            double ret = 300.0 + x + 2.0 * y + 0.1 * x * x + 0.1 * x * y + 0.1 * Math.Sqrt(Math.Abs(x));
            ret += (20.0 * Math.Sin(6.0 * x * pi) + 20.0 * Math.Sin(2.0 * x * pi)) * 2.0 / 3.0;
            ret += (20.0 * Math.Sin(x * pi) + 40.0 * Math.Sin(x / 3.0 * pi)) * 2.0 / 3.0;
            ret += (150.0 * Math.Sin(x / 12.0 * pi) + 300.0 * Math.Sin(x / 30.0 * pi)) * 2.0 / 3.0;
            return ret;
        }

    }
}
