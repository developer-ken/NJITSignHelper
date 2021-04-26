using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NJITSignHelper.SignMsgLib
{
    public class SignObject
    {
        public struct FormItem
        {
            public string title;
            public int wid;
            public FormSelection[] selections;
        }

        public struct FormSelection
        {
            public string content;
            public int wid;
            public bool abNormal;
        }

        public int signWid, signInstanceWid;
        public FormItem[] form;
        public Client client;
        public PhyLocation.Location Center;
        public double r;
        public bool Expired;
        public DateTime DeadLine;
        public string Title;
        public bool Handled;
        public bool isFetchedMore { get; private set; }

        public SignObject(JObject jb, Client client)
        {
            this.client = client;
            string dataUrl = jb.Value<string>("mobileUrl");
            Match m = Regex.Match(dataUrl, "signWid=([0-9]*).signInstanceWid=([0-9]*)");
            signWid = int.Parse(m.Groups[1].Value);
            signInstanceWid = int.Parse(m.Groups[2].Value);
            isFetchedMore = false;
            Match mm = Regex.Match(jb.Value<string>("content"), "([0-9]{4})年([0-9]{1,})月([0-9]{1,})日 *([0-9]{1,}):([0-9]{1,})");
            DeadLine = new DateTime(int.Parse(mm.Groups[1].Value),
                int.Parse(mm.Groups[2].Value), int.Parse(mm.Groups[3].Value), int.Parse(mm.Groups[4].Value),
                int.Parse(mm.Groups[5].Value), 0);
            Expired = DeadLine <= DateTime.Now;
            Handled = jb.Value<bool>("isHandled");
        }

        public void FetchMore()
        {
            var jb = new JObject();
            jb.Add("signInstanceWid", signInstanceWid.ToString());
            jb.Add("signWid", signWid.ToString());
            var result = client.HTTP_POST("https://njit.campusphere.net/wec-counselor-sign-apps/stu/sign/detailSignInstance", jb);
            if (result == null)
            {
                throw new Exception("无法获取签到信息：认证失败");
            }
            if (result.Value<int>("code") != 0) return;
            result = (JObject)result["datas"];
            Center = new PhyLocation.Location(result["signPlaceSelected"][0].Value<double>("latitude"),
                result["signPlaceSelected"][0].Value<double>("longitude"))
            {
                locName = result["signPlaceSelected"][0].Value<string>("address")
            };
            r = result["signPlaceSelected"][0].Value<double>("radius");

            var fields = (JArray)result["extraField"];

            List<FormItem> itemList = new List<FormItem>();

            foreach (JObject jba in fields)
            {
                FormItem item = new FormItem
                {
                    title = jba.Value<string>("title"),
                    wid = jba.Value<int>("wid")
                };
                List<FormSelection> selections = new List<FormSelection>();
                foreach (JObject jbb in jba["extraFieldItems"])
                {
                    selections.Add(new FormSelection()
                    {
                        content = jbb.Value<string>("content"),
                        wid = jbb.Value<int>("wid"),
                        abNormal = jbb.Value<bool>("isAbnormal")
                    });
                }
                item.selections = selections.ToArray();
                itemList.Add(item);
            }
            form = itemList.ToArray();
            Title = result.Value<string>("taskName");
            isFetchedMore = true;
        }

        private string getSignature(PhyLocation.Location loc)
        {
            JObject jb = new JObject
            {
                { "systemName", client.Info.SystemName },
                { "systemVersion", client.Info.SystemVersion },
                { "model", client.Info.DeviceModel },
                { "deviceId", client.Info.DeviceId.ToString() },
                { "appVersion", client.Info.AppVersion },
                { "lat", loc.lat },
                { "lon", loc.lon },
                { "userId", client.Login.StudentId }
            };
            string str = jb.ToString();
            return Encrypt.Des.Encrypt(str, Client.ENCODE_KEY);
        }

        public JObject Sign(FormSelection[] selections, PhyLocation.Location location)
        {
            if (!isFetchedMore) return null;
            JObject jb = new JObject
            {
                { "longitude", location.lon },
                { "latitude", location.lat },
                { "isMalposition", location - Center > r ? 1 : 0 },
                { "abnormalReason", "" },
                { "signPhotoUrl", "" },
                { "isNeedExtra", 1 },
                { "position", location.locName },
                { "uaIsCpadaily", "true" },
                { "signInstanceWid", signInstanceWid.ToString() },
                { "signVersion", "1.0.0" }
            };

            var fieldItems = new JArray();

            foreach (FormSelection sel in selections)
            {
                var jobj = new JObject
                {
                    { "extraFieldItemValue", sel.content },
                    { "extraFieldItemWid", sel.wid }
                };
                fieldItems.Add(jobj);
            }

            jb.Add("extraFieldItems", fieldItems);
            var result = client.HTTP_POST("https://njit.campusphere.net/wec-counselor-sign-apps/stu/sign/submitSign",
                jb, getSignature(location));
            return result;
        }
    }
}
