using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace NorkonTest
{
    class Program
    {
        private static int Counter = 0;
        private static string cookieData = string.Empty;
        private static string[] cookiekeys;
        private static List<string> cookies;
        private static HttpClient client = new HttpClient();

        private static NorkonServerInfo norkonServerInfo;
        public static void Main(string[] args)
        {
            try
            {
                /*Test Code*/

                // string s = @"{'server':'17cf5910c7b22ce5426b4142b3486f89fac148ae0051ded962b5d0febddea8b4','serverName':'dninvestornorway','process':{'cpu':15144332.03,'uptime':151660888,'totalMemory':5354264976,'workingSet':8836395008,'peakWorkingSet':12241920000},'stats':{'dnApiCalls':7290,'iotCalls':0},'liveCenter':{'connected':49,'requests':7941},'quantHubs':{'connected':2458,'reconnected':0,'totalConnected':441180,'categories':[['overview',1230],['valuta',68],['ticker',909],['aksjer',118],['importance',4],['portfolio',51],[null,14],['2020corona',4],['aksjonaer',23],['allTrades',4],['analyze',20],['tegningsretter',0],['ek',2],['indekser',1],['ravarer',1],['intl-aksjer',0],['etner',0],['sub',1],['signals',2],['favtickers',3],['fond',3],['renter',0],['etfer',0],['warranter',0]]},'rt':{'users':248,'connected':369},'fallbackMgr':{'primaryReady':true,'fallbackReady':true},'reqCount':{'http':2720505,'normal':687652,'rt':269709},'updates':{'frag':176185735,'channel':445116,'area':33730,'rtFrag':178244115,'rtChannel':956616,'rtArea':175319},'mitoRequests':0}";

                // string s2 = @"{ 'uptime': 100, 'serverName': 'vinz1'}";


                /*End of test code*/

                var appsettings = ConfigurationManager.AppSettings;
                cookiekeys = appsettings.AllKeys;

                cookies = new List<string>();

                foreach (string cookieKey in cookiekeys)
                {
                    cookies.Add(appsettings[cookieKey]);
                }

                // Get Stats of All Servers
                LoadCumulativeServerstats();
                Timer t = new Timer(TimerCallback, null, 0, 1000);

                var keyStroke = Console.ReadKey(false);
                HandleToggleKeys(keyStroke.Key);
            }
            catch (Exception)
            {
                throw;
            }
        }

        //Retriving and populating the cummulative server data
        private static async void LoadCumulativeServerstats(CancellationToken cancellationToken = default)
        {
            Console.WriteLine("cumulative server metric loading... ");
            int frag = 0, uptime = 0, totalConnections = 0, httpRecCount = 0;
            bool fallbackready = true;

            for (int i = 0; i < cookiekeys.Length; i++)
            {
                cookieData = await GetServerStats(cookies[i], cancellationToken);
                norkonServerInfo = PopulateServerInfo(cookieData);

                frag += norkonServerInfo.frag;
                uptime += norkonServerInfo.uptime;
                totalConnections += norkonServerInfo.totalConnected;
                httpRecCount += norkonServerInfo.httpRecCount;
                fallbackready = fallbackready & norkonServerInfo.fallbackReady;
            }

            norkonServerInfo.frag = frag;
            norkonServerInfo.uptime = uptime;
            norkonServerInfo.totalConnected = totalConnections;
            norkonServerInfo.httpRecCount = httpRecCount;

            norkonServerInfo.fallbackReady = fallbackready;
            norkonServerInfo.serverName = "ALL";

            DisplayServerStats(norkonServerInfo);
        }

        //Displaying the Server info on the console
        private static void DisplayServerStats(NorkonServerInfo serverInfo)
        {
            Console.Clear();
            if (serverInfo.serverName.ToUpper() == "ALL")
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.White;
                Console.Write(serverInfo.serverName + " ");
                Console.ResetColor();
            }
            else
            {
                Console.Write("ALL ");
            }
            foreach (string cookieKey in cookiekeys)
            {
                if (cookieKey.Substring(6, 4).ToUpper() == serverInfo.serverName.ToUpper())
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.Write(cookieKey.Substring(6, 4) + " ", ConsoleColor.Green);
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(cookieKey.Substring(6, 4) + " ");
                }
            }

            Console.WriteLine("");
            Console.WriteLine("Server Name: " + serverInfo.serverName);
            Console.WriteLine("Connection Count: " + serverInfo.totalConnected.ToString());
            TimeSpan timespan = TimeSpan.FromSeconds(serverInfo.uptime);
            string Uptime = string.Format("{0:D2}:{1:D2}:{2:D2}:{2:D2}",
                            timespan.Hours, timespan.Minutes, timespan.Seconds, timespan.Milliseconds);
            Console.WriteLine("Uptime: " + Uptime);
            Console.WriteLine("Fragment Updates : " + serverInfo.frag);
            Console.WriteLine("Http Request Count : " + serverInfo.httpRecCount.ToString());
            Console.WriteLine("Fallback ready  : " + serverInfo.fallbackReady.ToString());

            Console.WriteLine("");
            Console.WriteLine("use only <- or -> to toggle between servers or ESC to exit ");

        }

        private static async void LoadNextOrPreviousServerStats(CancellationToken cancellationToken = default)
        {
            if (Counter == 0)
            {
                Console.Clear();
                LoadCumulativeServerstats(cancellationToken);
            }
            else
            {
                var s = await GetServerStats(cookies[Counter - 1], cancellationToken);
                norkonServerInfo = PopulateServerInfo(s);
                Console.Clear();
                DisplayServerStats(norkonServerInfo);
            }
            var keyStroke = Console.ReadKey(false);
            HandleToggleKeys(keyStroke.Key);
        }

        //Populating the Server info in Serverinfo object
        private static NorkonServerInfo PopulateServerInfo(string cookieValue)
        {
            JObject jObject = JObject.Parse(cookieValue);

            NorkonServerInfo serverInfo = new NorkonServerInfo();
            serverInfo.serverName = jObject["result"]["server"].ToString().Substring(0, 4);
            serverInfo.frag = (int)jObject["result"]["updates"]["frag"];
            serverInfo.fallbackReady = (bool)jObject["result"]["fallbackMgr"]["fallbackReady"];
            serverInfo.httpRecCount = (int)jObject["result"]["reqCount"]["http"];
            serverInfo.uptime = (int)jObject["result"]["process"]["uptime"];
            serverInfo.totalConnected = (int)jObject["result"]["quantHubs"]["connected"];

            return serverInfo;

        }

        //Fetching the string from the provided particular server
        private static async Task<string> GetServerStats(string cookie, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                client.CancelPendingRequests();
            client.DefaultRequestHeaders.Remove("Cookie");
            client.DefaultRequestHeaders.Add("Cookie", cookie);
            var response = client.GetAsync("https://investor.dn.no/JsonServer/GetStats").Result;
            var jsonString = response.Content.ReadAsStringAsync().Result;
            return jsonString;

        }

        //Navigating from the servers by clicking left and right arrow Keys 
        private static void HandleToggleKeys(ConsoleKey key)
        {
            CancellationToken cancellationToken = new CancellationToken(true);
            switch (key)
            {
                case ConsoleKey.RightArrow:
                    Console.Clear();
                    Console.WriteLine("next server info loading...");

                    if (Counter < cookiekeys.Length)
                    {
                        Counter++;
                    }
                    else
                    {
                        Counter = 0;
                    }
                    LoadNextOrPreviousServerStats(cancellationToken);
                    break;
                case ConsoleKey.LeftArrow:

                    Console.Clear();
                    Console.WriteLine("previous server info loading...");
                    if (Counter > 0)
                        Counter--;
                    else if (Counter == 0)
                    {
                        Counter = cookiekeys.Length;
                    }
                    LoadNextOrPreviousServerStats(cancellationToken);
                    break;
                case ConsoleKey.Escape:
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("use only <- or -> to toggle between servers or ESC to exit ");
                    var keyHit = Console.ReadKey(false);
                    HandleToggleKeys(keyHit.Key);
                    break;
            }
        }

        //Timer call back call at the interval of 1 second
        private static async void TimerCallback(Object o)
        {
            Console.WriteLine("Refreshing...");

            if (Counter == 0)
            {
                LoadCumulativeServerstats();
            }
            else
            {
                cookieData = await GetServerStats(cookies[Counter - 1]);
                norkonServerInfo = PopulateServerInfo(cookieData);
                DisplayServerStats(norkonServerInfo);
            }
            GC.Collect();
        }

    }
}
