using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static AssertNet.Assertions;

namespace Player.Real.Tests
{
    [TestClass()]
    public class RealGameTest
    {
        string rememberMeCookie = "1510662d387f857308cc7bac141570d682125d8";
        int userID = 1510662;

        static HttpClient client = new HttpClient(new LoggingHandler(new HttpClientHandler()));


        [TestMethod()]
        public void PlayScenario()
        {
            List<string> gamesToCheck = new List<string>()
            {
                "seed=557895426\n",  /// merke geschickte einheiten nicht (ist vllt aber auch besser für andere szenarien direkt zu schicken)
                "seed=119001932\n",   /// total katastrophe
                "seed=293889482\n", // (y)
                "seed=542656354\n", // verlust weil zu weit gespreaded
                "seed=419837463\n"  
            };


            List<string> lostGames = new List<string>();
            foreach (string gameSeed in gamesToCheck)
            {
                string gameResult = GameJsonFromServer(gameSeed);
                if (!GameIsWon(gameResult))
                    lostGames.Add(gameSeed);
            }

            AssertThat(lostGames).IsEmpty();
        }

        [TestMethod()]
        public void DebugGame()
        {
            var gameInputs = parseGameInput(readGameInput(File.ReadAllText("GameResult.json")));

            Console.SetIn(new StringReader(string.Join("\n", gameInputs)));
            Player.Main(null);
        }


        [TestMethod()]
        public void DebugGameRemote()
        {
            string gameSeed = "seed=119001932\n";
            var gameInputs = parseGameInput(readGameInput(GameJsonFromServer(gameSeed)));

            Console.SetIn(new StringReader(string.Join("\n", gameInputs)));
            Player.Main(null);
        }

        private bool GameIsWon(string gameJson)
        {
            dynamic data = JObject.Parse(gameJson);
            int first = data.ranks[0];
            return first == 0;
        }

        private List<string> readGameInput(string gameResultJson)
        {
            List<string> result = new List<string>();
            dynamic data = JObject.Parse(gameResultJson);
            foreach (var frame in data.frames)
            {
                string agent = frame.agentId;
                if (agent == "0")
                {
                    string error = frame.stderr;
                    result.Add(error);
                }
            }

            return result;
        }


        private List<string> parseGameInput(List<string> errorOutputs)
        {
            List<string> result = new List<string>();
            foreach (var errorOutput in errorOutputs)
            {
                string[] errorLines = errorOutput.Split("\n");
                foreach (var line in errorLines)
                {
                    MatchCollection matches = Regex.Matches(line, @"debug:\^(.*)debug:\$");
                    foreach (Match match in matches)
                    {
                        result.Add(match.Groups[1].Value);
                    }
                }
            }
            return result;
        }




        public string GameJsonFromServer(string gameSeed)
        {
            client.DefaultRequestHeaders.Add("cookie", $"rememberMe = {rememberMeCookie};");
            string code = File.ReadAllText("Program.cs");
            dynamic multi = new JObject();
            multi.agentsIds = JToken.FromObject(new int[] { -1, -2 });
            multi.gameOptions = gameSeed;
            dynamic json = new JObject();
            json.code = code;
            json.programmingLanguageId = "C#";
            json.multi = multi;
            string payload = $"[\"775977482a81ca4fc246f300d2146c20118ed07\",{json} ]";
            

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = client.PostAsync("https://www.codingame.com/services/TestSession/play", content).Result;
            return response.Content.ReadAsStringAsync().Result;
        }


    }
}