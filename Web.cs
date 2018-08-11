using HtmlAgilityPack;
using Test.Common.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Test.Common
{
    public sealed class Web
    {
        private int proxyCounter = 0;
        public static Random rng = new Random();

        private List<Model.Proxy> _proxies;

        private object RetailCloudExceptionLock = new object();

        
        

        public Web()
        {
            proxyCounter = 0;
            _proxies = new List<Model.Proxy>();
            
            
            
        }

        public List<Model.Proxy> Proxies
        {
            get
            {
                return _proxies;
            }
            set
            {
                _proxies = value;
            }
        }


        public HtmlDocument GetDocument(string SourceUrl, SharedData sharedData, out string UpdatedSourceUrl, string ValidationCondition = "", CookieContainer CContainer = null)
        {

            System.Net.ServicePointManager.DefaultConnectionLimit = 150;

            HttpWebRequest htpReq = null;
            HttpWebResponse htpRes;


            HtmlAgilityPack.HtmlDocument htmaDoc;

            int a = 0;

            htmaDoc = new HtmlAgilityPack.HtmlDocument();

            UpdatedSourceUrl = SourceUrl;

            string Host = SourceUrl.Substring(0, SourceUrl.IndexOf(":") + 3) + new Uri(SourceUrl).Host;

            string CurrentProxy = "";

            proxyCounter = sharedData.SelectProxiesCounter(Proxies.Count);
            CurrentProxy = Proxies[proxyCounter].ProxyValue;




            for (; a < 3; a++)
            {
                try
                {
                    htpReq = GetRequest(UpdatedSourceUrl, true);

                    if (CurrentProxy != "")
                    {
                        htpReq.Proxy = new WebProxy(CurrentProxy.Split(':')[0], int.Parse(CurrentProxy.Split(':')[1]));
                        htpReq.Proxy.Credentials = new NetworkCredential(CurrentProxy.Split(':')[2], CurrentProxy.Split(':')[3]);
                    }


                    htpRes = (HttpWebResponse)htpReq.GetResponse();

                    switch (htpRes.StatusCode)
                    {
                        case HttpStatusCode.MovedPermanently:
                        case HttpStatusCode.Found:
                            if (htpRes.Headers.GetValues("location")[0].Contains(";"))
                                UpdatedSourceUrl = htpRes.Headers.GetValues("location")[0].SubstringLastCharacter("", ";").StartsWith("http") == true ? htpRes.Headers.GetValues("location")[0].SubstringLastCharacter("", ";") : Host + htpRes.Headers.GetValues("location")[0].SubstringLastCharacter("", ";");
                            else
                                UpdatedSourceUrl = htpRes.Headers.GetValues("location")[0].StartsWith("http") == true ? htpRes.Headers.GetValues("location")[0] : Host + htpRes.Headers.GetValues("location")[0];

                            continue;

                        case HttpStatusCode.OK:
                            htmaDoc.Load(htpRes.GetResponseStream());
                            break;

                        default:
                            break;
                    }

                    htpRes.Close();

                    if (htmaDoc != null && ValidationCondition == "")
                        break;
                    else if (htmaDoc != null && ValidationCondition != "" && htmaDoc.DocumentNode.SelectSingleNode(ValidationCondition) != null)
                        break;
                    else if (htmaDoc != null && ValidationCondition != "" && htmaDoc.DocumentNode.SelectSingleNode(ValidationCondition) == null)
                        a = 3;
                }
                catch (Exception exp)
                {
                    System.Threading.Thread.Sleep(5000);
                    if (exp.Message == "The operation has timed out")
                    {
                        //Just in case it is because of Proxy over utilization
                        //Generate a random number for proxy, preferrably from upper half 
                        //Random random = new Random();
                        //proxyCounter = random.Next(Proxies.Count / 2, Proxies.Count);
                        proxyCounter = sharedData.SelectProxiesCounter(Proxies.Count);
                        CurrentProxy = Proxies[proxyCounter].ProxyValue;

                        
                    }
                }
            }

            if (a >= 3)
                return null;
            else
                return htmaDoc;
        }

        
        public HttpWebRequest GetRequest(string Url, bool AllowRedirect)
        {
            Uri FixedUrl = new Uri(Url);

            MethodInfo getSyntax = typeof(UriParser).GetMethod("GetSyntax", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            FieldInfo flagsField = typeof(UriParser).GetField("m_Flags", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (getSyntax != null && flagsField != null)
            {
                foreach (string scheme in new[] { "http", "https" })
                {
                    UriParser parser = (UriParser)getSyntax.Invoke(null, new object[] { scheme });

                    if (parser != null)
                    {
                        int flagsValue = (int)flagsField.GetValue(parser);

                        if ((flagsValue & 0x1000000) != 0)
                            flagsField.SetValue(parser, flagsValue & ~0x1000000);
                    }
                }
            }

            FixedUrl = new Uri(Url);

            HttpWebRequest HWRequest = (HttpWebRequest)HttpWebRequest.Create(FixedUrl);

            HWRequest.Host = FixedUrl.Host;

            HWRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36 Edge/16.16299";
            HWRequest.AllowAutoRedirect = true;
            HWRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            HWRequest.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
            HWRequest.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9,ru;q=0.8,uk;q=0.7");
            HWRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            HWRequest.KeepAlive = true;

            HWRequest.Headers.Set(HttpRequestHeader.Cookie, @"_abck=586D6AE37E1589323777D5C9CCBAE42D17357F86E826000007ED795AB6EA6F7E~0~8WMHPMDaQO8Aj2gAyXBQbKyZfbVxYJgUXS1VLXd0BAk=~-1~-1; THD_CACHE_NAV_PERSIST=; RES_TRACKINGID=475074506207556; _pxvid=7e948600-0b67-11e8-b80a-4b70fac8a702; THD_GUESTLIST=NwsKZgTE3jsZ6VHxi4AfWAa4oEh6B61+7wCHU6Xzhl2jaloaEVQGSPEILhoZCpubpM0dl2E55oO2ua4TivHR3UorQokfyVZorTb51tRPzHM=; _ga=GA1.2.1163237080.1517939977; _mibhv=anon-1517939977055-5279787870_4577; cto_lwid=fd3dc391-e1d6-4191-8463-b6f6610a97af; thda.u=caf5059a-e614-aa3c-3ce8-68d17756b8e5; LPVID=QzNjcwZDU4OTZlY2RhNjg1; aam_uuid=42276662487023126432224992379268001160; ftr_ncd=6; WRUID15e=1762803022627084; THD_SESSION=; _gid=GA1.2.1904637263.1528540173; bm_sz=13EB4A1AC69F6EB40C53BBBC870EE1AE~QAAQCxccuMrHd+BjAQAAcImh6EYgGeOad8We9dnFJTxOC08HlBYhmPBnWoFwTTGyvka7ZD6eUUvYqZOa6D6lc8UZoP0F1Dneza+PJam7V+F/C3mRaVBFZrDU9NjAhNRBDzrNhaPYoxacur5ce8LW98DMqiLgAAm0F+0Xz/OfQiMOgh5wiah9KOTVaemKCLhPR8M=; THD_FORCE_LOC=1; ak_bmsc=20801A7DBB7C9DED42FF7EF02681F483B81C170B4633000062D51C5B737DAE21~pliNOPXQ2FGFw9Tve2HJp9Wn9BzenoXvrkDWYs/etopz7/PnaoTCbgK4x71/wlRth+UKtxjBIRdud3XqwgVVk5oORUH/neME+bMRgQFn5xbWwaR1qhevcXxfUgXYQAxc+y8z8KB4oMKY5PBO1Ju9XdGwojel1lKad2Rl0MSloQWOAO3VDGhsms5tYgh0bAdIHGDVSIFrhzXeSdP9unEU+OIovQ98JJKW4QZN4BnDP3Nr535h9FoW6WJtfOrLTfWVp0; THD_CACHE_NAV_SESSION=; RES_SESSIONID=508665685797642; THD-LOC-STORE=; WRIgnore=true; _px=QrzFx77sY7/sJhZ2BY5+LzpfUONsDsfJeQdfp1QFf3o5oujDA37LjlD4MzNQ5cctOsXq5KvQcuRpcsMrR+edKg==:1000:b24vxInwrb9Z+GKdlmKqN5KCAjhMUJmPSoRM8sNWZsKad9V8MU6SG+uPLV+51GM7xxzuoSG7AsWlTnpKXWpyeOu80R0Fj+ugkAP74bYe5LMAZ//B4E5ox4JM1mvnm/9Lpey07b/KVC0PfjEn47ntOfbm1rMF+pqZX01imQzNm7AJeaGrxQdx2aFcEkgJ6g+iEbrdALRjZLDCraqysYjnZNd6+qQoQrLiSe/Uh1AD6ax4dgzPsmwu+e88qcy5aDzZBCTaF1DKu/DMnSpf9FGMGg==; mbox=PC#41536487f3b3497c8cf3ccffe71b517e.28_53#1581454627|session#cee6362061a74e1a83b31384ee7a3096#1528618340; bm_sv=EC3492C8981514216B32CA77C4B74E1F~5t+YsJBeuWF6cLSB6vS4Qs7IDu2T3Cc06/WosxupCeQavSGPF9fBqBg8u2Z7Ape0r2Hj495qUC/NhGo1lJtd4QQUw6OfR4Cdk1sc6VHFLmsNw4YUIFbkWoPpwnnfwoObrhlmOpmfipZK7RwOUObQ8R+KFZbH/u+ltVEN3wNIC5I=; s_pers=%20s_nr%3D1528616480304-Repeat%7C1560152480304%3B%20s_dslv%3D1528616480305%7C1623224480305%3B%20s_dslv_s%3DLess%2520than%25201%2520day%7C1528618280305%3B; _uetsid=_ueta7aca90b; _br_uid_2=uid%3D9502649385856%3Av%3D12.0%3Ats%3D1517939977303%3Ahc%3D11; forterToken=8ad97c7b9dc341f39cf91e500882f3bb_1528616480332__UDF43_6; __CT_Data=gpv=7&ckp=tld&dm=homedepot.com&apv_4_www23=7&cpv_4_www23=7&rpv_4_www23=7; AMCV_F6421253512D2C100A490D45%40AdobeOrg=-894706358%7CMCIDTS%7C17692%7CMCMID%7C42370218233061004062199900856274109437%7CMCAAMLH-1529144972%7C9%7CMCAAMB-1529221282%7CRKhpRz8krg2tLO6pguXWp5olkAcUniQYPHaMWWgdJ3xzPWQmdj0y%7CMCOPTOUT-1528623491s%7CNONE%7CMCAID%7CNONE%7CMCCIDH%7C1520615674%7CvVersion%7C2.3.0; ctm={'pgv':4083469809756386|'vst':1617727720318439|'vstr':5109552701292877|'intr':1528616974160|'v':1|'lvst':1266}; _4c_=hVNdTxsxEPwrlR94yuX8%2FREJVYHSikqFQqj6iO58e%2BTEJRfZTtMK5b%2BzDiRtAbWRLvLas%2Fbs7OwD2cxhSSZMcauZYcZQa0bkHn5FMnkgwef%2FH2RCUlgDGZGfGSqUFE5ITY3ZjkhIz5i26iO8gDCuEOKXvv8v6C60%2B5ue2GhpuTPyTXDomj0zUxtPgTpfW8Glr7RuQECjm6puHdTqN2vNDdVcMZcprZ7zH8g6IDkyT2kVJ2W52WzG82EBDayGNPbDolyV05CqmKC4rrplcX5eMF587cBD8XEId9AUzBaMFrOExz3EiCuAvphVfdVkyH0xg1QorSgrBWWKSmss0vJDA%2Fgyc2M55hi3677t%2Bn6WhgDnH%2FAE91br4OdVzLgLDGtIFS6H0N11S4xP%2B87f31Q9fNslMKO5pYJynmu1MmeEYRMh4OHpPGBd77TOT2H5hDEtuEAcHTPFjBPOGYOHA%2FaefO%2BWDWZiGKCFEHZXYBS7lMn8JRFuJwiLnIbLyy8317cnZ9PTy4s%2FdI2bKvoXytZljOVhaxWGpmS0%2FDwr%2BFiMaRnRjtpSxbGvXOn306uTY3a06JpjmWlzZrkQVDNKJfaVOecotbnLklEnhTmaXp0dsyxi9kk2Qj941AoD9PyIfJrePsv2phDokp1xuMW3rOVMO6wzoVmsljT%2FEJGv3vlIvULb1%2BinZvwj560X9qPDDmiNVTvOHc7GDs3kAY3q7Aejtk1r8%2BeUp641WigOBsA0igpZ14fB0CJfxqVh2%2B32EQ%3D%3D");

            


            return HWRequest;
        }

        

        public string GetResponseString(string SourceUrl, SharedData sharedData, out string UpdatedSourceUrl, CookieContainer CContainer = null)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 150;
            

            HttpWebRequest htpReq = null;
            HttpWebResponse htpRes;

            string docString = "";

            


            int a = 0;            

            UpdatedSourceUrl = SourceUrl;

            string Host = SourceUrl.Substring(0, SourceUrl.IndexOf(":") + 3) + new Uri(SourceUrl).Host;

            string CurrentProxy = "";

            proxyCounter = sharedData.SelectProxiesCounter(Proxies.Count);
            CurrentProxy = Proxies[proxyCounter].ProxyValue;

            

            for (; a < 3; a++)
            {
                try
                {
                    docString = "";

                    htpReq = GetRequest(UpdatedSourceUrl, true);

                    if (CurrentProxy != "")
                    {
                        htpReq.Proxy = new WebProxy(CurrentProxy.Split(':')[0], int.Parse(CurrentProxy.Split(':')[1]));
                        htpReq.Proxy.Credentials = new NetworkCredential(CurrentProxy.Split(':')[2], CurrentProxy.Split(':')[3]);
                    }

                    htpRes = (HttpWebResponse)htpReq.GetResponse();

                    switch (htpRes.StatusCode)
                    {
                        case HttpStatusCode.MovedPermanently:
                        case HttpStatusCode.Found:
                            if (htpRes.Headers.GetValues("location")[0].Contains(";"))
                                UpdatedSourceUrl = htpRes.Headers.GetValues("location")[0].SubstringLastCharacter("", ";").StartsWith("http") == true ? htpRes.Headers.GetValues("location")[0].SubstringLastCharacter("", ";") : Host + htpRes.Headers.GetValues("location")[0].SubstringLastCharacter("", ";");
                            else
                                UpdatedSourceUrl = htpRes.Headers.GetValues("location")[0].StartsWith("http") == true ? htpRes.Headers.GetValues("location")[0] : Host + htpRes.Headers.GetValues("location")[0];

                            continue;

                        case HttpStatusCode.OK:

                            using (StreamReader streamReader = new StreamReader(htpRes.GetResponseStream(), Encoding.UTF8))
                            {
                                docString = streamReader.ReadToEnd();                                
                            }
                            
                            break;

                        default:
                            break;
                    }

                    htpRes.Close();

                    if (docString != null && docString != "")
                        break;                    
                    else 
                        a = 3;
                }
                catch (Exception exp)
                {
                    System.Threading.Thread.Sleep(5000);
                    proxyCounter = sharedData.SelectProxiesCounter(Proxies.Count);
                    CurrentProxy = Proxies[proxyCounter].ProxyValue;
                 }
                
            }

            if (a >= 3)
                return null;
            else
                return docString;
        }

        
    }
}
