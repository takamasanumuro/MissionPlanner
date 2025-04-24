using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;


namespace MissionPlanner.GCSViews
{
    public class FlightDataRemoteUpdate
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly string _token = Environment.GetEnvironmentVariable("INFLUX_TOKEN", EnvironmentVariableTarget.User);
        private const string Org = "Innomaker";
        private const string Bucket = "Innoboat";
        private const string Url = "http://144.22.131.217:8086";

        public FlightDataRemoteUpdate()
        {
            HttpClient.DefaultRequestHeaders.Clear(); 
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _token);
            HttpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/csv"));;
        }

        public async Task FetchUpdateForMavAsync(CurrentState cs)
        {
            try
            {
                var fluxQuery = $@"
                                      from(bucket:""{Bucket}"")
                                      |> range(start: -120m)
                                      |> filter(fn: (r) => r._measurement == ""Innoboat"")
                                      |> last()";


                var content = new StringContent(fluxQuery);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.flux");

                var requestUrl = $"{Url}/api/v2/query?org={Org}";
                var response = await HttpClient.PostAsync(requestUrl, content);

                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();

                void SetIfPresent(string field, Action<float> setter)
                {
                    var value = ParseValueFromResponse(responseBody, field);
                    if (value.HasValue)
                    {
                        setter(value.Value);
                    }
                }
                
                SetIfPresent("[BB]Voltage", value => cs.TensaoBateriaBombordo = value);
                SetIfPresent("[BO]Voltage", value => cs.TensaoBateriaBoreste = value);
                SetIfPresent("[BB]Current", value => cs.CorrenteBateriaBombordo = value);
                SetIfPresent("[BO]Current", value => cs.CorrenteBateriaBoreste = value);
                SetIfPresent("tensao-barramento", value => cs.TensaoBarramento = value);
                SetIfPresent("tensao-aux", value =>cs.TensaoBateriaAuxiliar = value);
                SetIfPresent("corrente-motor-bombordo", value => cs.CorrenteMotorBombordo = value); 
                SetIfPresent("corrente-motor-boreste", value => cs.CorrenteMotorBoreste = value);
                
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

        private readonly SemaphoreSlim _fetchLock = new SemaphoreSlim(1, 1);

        public void UpdateMavStateAsync(CurrentState cs)
        {
            
            if (!_fetchLock.Wait(0)) return;
            
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
                finally
                {
                    _fetchLock.Release();
                }
            });
        }
    }
}
