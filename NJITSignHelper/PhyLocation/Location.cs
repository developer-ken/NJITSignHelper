using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace NJITSignHelper.PhyLocation
{
    [Serializable]
    public class Location
    {
        public double lat, lon;
        public string locName;

        public Location(double lat, double lon)
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

        public Location ToGCJ02()
        {
            return GPSChange.WGS84_to_GCJ02(lat,lon);
        }

        public static string getLocName(Location loc)
        {
            string urlString = "http://restapi.amap.com/v3/geocode/regeo?key=8325164e247e15eea68b59e89200988b&s=rsv3&location=" + loc.lon + "," + loc.lat + "&radius=0&platform=JS&logversion=2.0&sdkversion=1.3&appname=http%3A%2F%2Flbs.amap.com%2Fconsole%2Fshow%2Fpicker&csid=49851531-2AE3-4A3B-A8C8-675A69BCA316";
            JObject jb = (JObject)JsonConvert.DeserializeObject(HTTP_GET(urlString));
            return jb["regeocode"].Value<string>("formatted_address");
        }

        public static string HTTP_GET(string url)
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.AllowAutoRedirect = true;
            req.Method = "GET";
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }

        public const double EarthRadiusKm = 6378.137; // WGS-84
    }
}
