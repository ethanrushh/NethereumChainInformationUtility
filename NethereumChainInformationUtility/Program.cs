using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

namespace ChainInformationUtility
{
    internal class Program
    {
        static HttpClient client = new();
        const string chainListRpc = @"https://raw.githubusercontent.com/DefiLlama/chainlist/844277a44d6c8c9bff5d2e75e7de20ee010317f0/constants/extraRpcs.json";
        static bool disableOutput = false;
        const bool DEBUG_MODE = false;

        static async Task Main(string[] args)
        {
            disableOutput = !DEBUG_MODE;
            dynamic rpcList = new{};

            client.Timeout = TimeSpan.FromSeconds(5);

            try
            {
                HttpClient rpcListClient = new();
                var rawJson = await rpcListClient.GetStringAsync(chainListRpc);

                try { rpcList = JsonConvert.DeserializeObject(rawJson)!; }
                catch (Exception ex) { throw new Exception("Failed to deserialize json from downloaded rpc list: ", ex); }
            }
            catch (Exception ex)
            {
                Output($"FAIL: Failed to get RPC URLs from {chainListRpc}: " + ex);
                Environment.Exit(-1);
            }

            var result = ScanRpcs(rpcList, 100);

            disableOutput = false;

            Output(JsonConvert.SerializeObject(result, Formatting.Indented), false);

            Environment.Exit(0); // Just to be 100% sure all the threads are gone
        }

        static IEnumerable<RpcInfo> ScanRpcs(dynamic chainRpcList, uint maxThreads)
        {
            List<RpcInfo> result = new();
            List<Rpc> toScan = new();

            foreach (var rpc in chainRpcList)
            {
                try
                {
                    ulong chainId = ulong.Parse(rpc.Name); // Get the Chain ID
                    List<string> rpcList = rpc.Value.rpcs.ToObject<List<string>>(); // Get the known public RPCs for that chain

                    if (rpcList.Count == 0) continue; // Skip if there are no working RPCs
                    if (rpcList.Contains("rpcWorking:false")) continue; // Skip if it has been noted as legacy retired

                    try { if (rpc.Value.rpcWorking.ToObject<bool>()) continue; Output("Skipped " + chainId); } catch { }

                    toScan.Add(new Rpc(chainId, rpcList));
                }
                catch { Output("Failed to scan a chain"); }
            }

            Parallel.ForEach(toScan, new ParallelOptions { MaxDegreeOfParallelism = (int)maxThreads }, rpcDto =>
            {
                Parallel.ForEach(rpcDto.RpcList, rpc =>
                {
                    try
                    {
                        var isValid = ValidateRpcExists(rpc).Result;

                        if (!isValid) return;

                        result.Add(new RpcInfo(rpcDto.ChainId, rpc, ChainHasEip1559Support(rpc).Result));
                    }
                    catch { }
                });
            });
            
            result.Sort((x, y) => x.ChainId.CompareTo(y.ChainId));

            return result;
        }

        // Uses web3_clientVersion - tested to work on all
        public static async Task<bool> ValidateRpcExists(string url) => await MakeRpcRequest(url, "{\"jsonrpc\":\"2.0\",\"method\":\"web3_clientVersion\",\"params\":[],\"id\":67}");

        // Checks if eth_feeHistory exists
        public static async Task<bool> ChainHasEip1559Support(string url) => await MakeRpcRequest(url, "{\"jsonrpc\":\"2.0\",\"method\":\"eth_feeHistory\",\"params\":[4, \"latest\", [25, 75]],\"id\":1}");

        public static async Task<bool> MakeRpcRequest(string url, string data)
        {
            try
            {
                var httpResult = await client.PostAsync(url, new StringContent(data));

                //Console.WriteLine(await httpResult.Content.ReadAsStringAsync());

                if (!httpResult.IsSuccessStatusCode)
                    return false;

                return (await httpResult.Content.ReadAsStringAsync()).Contains("result");
            }
            catch (Exception ex)
            {
                Output($"WARN: Failed to scan {url}: " + ex.Message);
                return false;
            }
        }

        public struct RpcInfo
        {
            public ulong ChainId;
            public string Rpc;
            public bool HasEIP1559Support;

            public RpcInfo(ulong chainId, string rpc, bool rpcWorking)
            {
                ChainId = chainId;
                Rpc = rpc;
                HasEIP1559Support = rpcWorking;
            }

            public override string ToString() => $"Chain: {ChainId}, EIP1559: {(HasEIP1559Support ? "Yes" : "No")} - {Rpc}";
        }

        public class Rpc
        {
            public ulong ChainId;
            public List<string> RpcList;

            public Rpc(ulong chainId, List<string> rpcList)            {
                ChainId = chainId;
                RpcList = rpcList;
            }

        }

        static void Output(string message, bool newLine = true) => Console.Write(disableOutput ? "" : message + (newLine ? Environment.NewLine : "")); // Here to make it easier to refactor logging later
    }
}
