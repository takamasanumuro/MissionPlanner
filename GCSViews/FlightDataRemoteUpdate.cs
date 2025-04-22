using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Globalization;


namespace MissionPlanner.GCSViews
{
    public class FlightDataRemoteUpdate
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private string _token = Environment.GetEnvironmentVariable("INFLUX_TOKEN", EnvironmentVariableTarget.User);
        private const string Org = "Innomaker";
        private const string Bucket = "Teste";
        private const string Field = "tensao-barramento";
        private const string Url = "http://144.22.131.217:8086";

        public FlightDataRemoteUpdate()
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _token);
            HttpClient.DefaultRequestHeaders.ConnectionClose = false;
            HttpClient.DefaultRequestHeaders.Connection.Add("keep-alive");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/csv"));;
        }

        public async Task FetchUpdateForMavAsync(CurrentState cs)
        {
            try
            {
                var fluxQuery = $@"
                                      from(bucket:""{Bucket}"")
                                      |> range(start: -120m)
                                      |> filter(fn: (r) => r._measurement == ""Mavproxy"")
                                      |> last()";


                var content = new StringContent(fluxQuery);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.flux");

                var requestUrl = $"{Url}/api/v2/query?org={Org}";
                var response = await HttpClient.PostAsync(requestUrl, content);

                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();

                var tensaoBarramento = ParseValueFromResponse(responseBody, "tensao-barramento");
                var tensaoBombordo = ParseValueFromResponse(responseBody, "[BB]Voltage");
                var tensaoBoreste = ParseValueFromResponse(responseBody, "[BE]Voltage");
                
                if (tensaoBarramento.HasValue)
                {
                    cs.TensaoBarramento = tensaoBarramento.Value;
                    
                }

                if (tensaoBombordo.HasValue)
                {
                    cs.TensaoBateriaBombordo = tensaoBombordo.Value;
                }
                
                if (tensaoBoreste.HasValue)
                {
                    cs.TensaoBateriaBoreste = tensaoBoreste.Value;
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InfluxDB query failed: {ex.Message}");
            }
        }

        private static float? ParseValueFromResponse(string response, string field)
        {
            // The response is CSV format. Find the line that starts with a value.
            var lines = response.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                    continue;

                var columns = line.Split(',');
                if (columns.Length < 8 || columns[7] != field)
                {
                    continue;
                }

                if (Single.TryParse(columns[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    return (float)Math.Round(value, 2);
                }
            }
            return null;
        }

        public void UpdateMavStateAsync(CurrentState cs)
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await FetchUpdateForMavAsync(cs);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"FetchUpdateForMAVAsync failed {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        
        
    }
}
