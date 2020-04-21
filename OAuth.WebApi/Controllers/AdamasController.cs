using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Web;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace OAuth.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdamasController : Controller
    {
        private static readonly HttpClient client = new HttpClient();

        IWebHostEnvironment _hostingEnvironment;
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<AdamasController> _logger;

        public AdamasController(
            ILogger<AdamasController> logger,
            IWebHostEnvironment hostingEnvironment)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("token")]
        public IActionResult GetURL()
        {
            return Ok(true);
        }


        [HttpPost("get-token")]
        public async Task<IActionResult> Post()
        {
            string path = Path.Combine(_hostingEnvironment.ContentRootPath, "authorize.json");
            AuthorizeToken token;
            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                token = JsonConvert.DeserializeObject<AuthorizeToken>(json);
            }

            var dict = new Dictionary<string, string>();
            dict.Add("grant_type", "authorization_code");
            dict.Add("client_id", "9CF68B5A29B24E62A12896569FC7B3BE");
            dict.Add("code", token.Code);
            dict.Add("redirect_uri", "http://localhost:5000/adamas/code");
            dict.Add("code_verifier", "Bc6SnVn~o5.XsJWF9a9SbjEIPoZta3QE0HJdq4U92~nnDXaAE20ljbe1dEhPqR~VAE3jc~t8IfxFR6bBzk_PhcZePXNYJgWnWI3I53GXPMNO7YyBCFMxn2RdjKwnxCCo");

            var req = new HttpRequestMessage(HttpMethod.Post, "https://identity.xero.com/connect/token") { Content = new FormUrlEncodedContent(dict) };
            var res = await client.SendAsync(req);
            var bearerData = await res.Content.ReadAsStringAsync();

            Token tokenObj = JsonConvert.DeserializeObject<Token>(bearerData);
            JsonSerializer serializer = new JsonSerializer();
            string tokenPath = Path.Combine(_hostingEnvironment.ContentRootPath, "token.json");


            System.IO.File.WriteAllText(tokenPath, tokenObj.ToString());

            // write JSON directly to a file
            using (StreamWriter file = System.IO.File.CreateText(tokenPath))
            using (JsonTextWriter writer = new JsonTextWriter(file))
            {
                serializer.Serialize(writer, tokenObj);
            }

            return Ok(bearerData);
        }

        [HttpGet("authenticate")]
        public async Task<IActionResult> Authorize(){           
            OpenBrowser("https://login.xero.com/identity/connect/authorize?response_type=code&client_id=9CF68B5A29B24E62A12896569FC7B3BE&redirect_uri=http://localhost:5000/adamas/code&scope=openid%20profile%20email%20accounting.transactions&state=123&code_challenge=nFQ_9bqTJdcT6idgardzwW-hzYfBdoDsmRkubc-wHFk&code_challenge_method=S256");
            return Ok(true);
        }

        [HttpGet("code")]
        public IActionResult GetURL([FromQuery] string code, [FromQuery] string scope, [FromQuery] string state, [FromQuery] string session_state)
        {

            string path = Path.Combine(_hostingEnvironment.ContentRootPath, "authorize.json");

            JObject ac = new JObject(
                new JProperty("code", code),
                new JProperty("scope", scope),
                new JProperty("state", state),
                new JProperty("session_state", session_state));

            System.IO.File.WriteAllText(path, ac.ToString());

            // write JSON directly to a file
            using (StreamWriter file = System.IO.File.CreateText(path))
            using (JsonTextWriter writer = new JsonTextWriter(file))
            {
                ac.WriteTo(writer);
            }     
            return Ok(true);
        }



        public static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                // throw 
            }
        }
    }

    public class AuthorizeToken{
        public string Code { get; set; }
        public string Scope { get; set;  }
        public string State { get; set; }
        public string Session_State { get; set; }
    }

    public class Token{
        public string Id_Token { get; set; }
        public string Access_Token { get; set; }
        public int Expires_In { get; set; }
        public string Token_Type { get; set; }
        public string Scope { get; set; }

    }
}
