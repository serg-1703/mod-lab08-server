using System;
using System.Threading;
using System.IO;

namespace QueueingSystemSimulation
{
    class Program
    {
        static void Main(string[] args) => RunSimulation();

        static void RunSimulation()
        {
            const double serviceRate = 1.0;
            const int channelCount = 5;
            const int totalRequests = 25;
            const double minArrivalRate = 0.2;
            const double maxArrivalRate = 10;
            const double arrivalRateStep = 0.2;

            for (double arrivalRate = minArrivalRate; 
                 arrivalRate <= maxArrivalRate; 
                 arrivalRate += arrivalRateStep)
            {
                RunSingleSimulation(
                    Math.Round(arrivalRate, 1), 
                    serviceRate, 
                    channelCount, 
                    totalRequests);
            }
        }

        static void RunSingleSimulation(
            double arrivalRate, 
            double serviceRate, 
            int channelCount, 
            int totalRequests)
        {
            Console.WriteLine($"\nArrival rate = {arrivalRate}, Service rate = {serviceRate}");

            var server = new ServiceSystem(channelCount, serviceRate);
            var client = new RequestGenerator(server);

            GenerateRequests(client, arrivalRate, totalRequests);
            WaitForSystemCompletion(server);

            CalculateAndSaveResults(arrivalRate, serviceRate, channelCount, server);
        }

        static void GenerateRequests(
            RequestGenerator client, 
            double arrivalRate, 
            int totalRequests)
        {
            for (int requestId = 1; requestId <= totalRequests; requestId++)
            {
                client.SendRequest(requestId);
                Thread.Sleep((int)(500 / arrivalRate));
            }
        }

        static void WaitForSystemCompletion(ServiceSystem server)
        {
            while (server.BusyChannelsCount > 0)
            {
                Thread.Sleep(100);
            }
        }

        static void CalculateAndSaveResults(
            double arrivalRate, 
            double serviceRate, 
            int channelCount, 
            ServiceSystem server)
        {
            double trafficIntensity = arrivalRate / serviceRate;
            double idleProbability = CalculateIdleProbability(trafficIntensity, channelCount);
            double rejectionProbability = CalculateRejectionProbability(
                trafficIntensity, 
                channelCount, 
                idleProbability);
            double relativeThroughput = 1 - rejectionProbability;
            double absoluteThroughput = arrivalRate * relativeThroughput;
            double averageBusyChannels = trafficIntensity * relativeThroughput;

            double empiricalIdleProbability = CalculateEmpiricalIdleProbability(server);
            double empiricalRejectionProbability = CalculateEmpiricalRejectionProbability(server);
            double empiricalRelativeThroughput = CalculateEmpiricalRelativeThroughput(server);
            double empiricalAbsoluteThroughput = arrivalRate * empiricalRelativeThroughput;
            double empiricalAverageBusyChannels = CalculateEmpiricalAverageBusyChannels(server, serviceRate);

            SaveResultsToFile(
                arrivalRate, 
                serviceRate, 
                idleProbability, 
                rejectionProbability, 
                relativeThroughput, 
                absoluteThroughput, 
                averageBusyChannels,
                empiricalIdleProbability,
                empiricalRejectionProbability,
                empiricalRelativeThroughput,
                empiricalAbsoluteThroughput,
                empiricalAverageBusyChannels);
        }

        static void SaveResultsToFile(
            double arrivalRate, 
            double serviceRate,
            double idleProb,
            double rejectProb,
            double relThroughput,
            double absThroughput,
            double avgBusyChannels,
            double empIdleProb,
            double empRejectProb,
            double empRelThroughput,
            double empAbsThroughput,
            double empAvgBusyChannels)
        {
            try
            {
                // 1. Путь к файлу в папке проекта
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "simulation_results.txt");
                
                // 2. Формируем строку результата
                string resultLine = string.Format("{0} {1} {2:F4} {3:F4} {4:F4} {5:F4} {6:F4} {7:F4} {8:F4} {9:F4} {10:F4} {11:F4}",
                    arrivalRate.ToString("0.0"),
                    serviceRate.ToString("0.0"),
                    Math.Round(idleProb, 4),
                    Math.Round(rejectProb, 4),
                    Math.Round(relThroughput, 4),
                    Math.Round(absThroughput, 4),
                    Math.Round(avgBusyChannels, 4),
                    Math.Round(empIdleProb, 4),
                    Math.Round(empRejectProb, 4),
                    Math.Round(empRelThroughput, 4),
                    Math.Round(empAbsThroughput, 4),
                    Math.Round(empAvgBusyChannels, 4));

                // 3. Режим записи (добавление или перезапись)
                if (IsFirstRecord())
                {
                    File.WriteAllText(filePath, resultLine + Environment.NewLine); // Перезапись
                }
                else
                {
                    File.AppendAllText(filePath, resultLine + Environment.NewLine); // Добавление
                }

                Console.WriteLine($"Данные записаны в: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи: {ex.Message}");
            }
        }

// Проверка первой записи
static bool isFirstRecord = true;
static bool IsFirstRecord()
{
    if (isFirstRecord)
    {
        isFirstRecord = false;
        return true;
    }
    return false;
}

        static double CalculateEmpiricalIdleProbability(ServiceSystem server) => 
            (double)server.IdleTime / server.TotalOperationTime;

        static double CalculateEmpiricalRejectionProbability(ServiceSystem server) => 
            (double)server.RejectedRequestsCount / server.TotalRequestsCount;

        static double CalculateEmpiricalRelativeThroughput(ServiceSystem server) => 
            (double)server.ProcessedRequestsCount / server.TotalRequestsCount;

        static double CalculateEmpiricalAverageBusyChannels(ServiceSystem server, double serviceRate) => 
            server.BusyTime / (server.TotalOperationTime * serviceRate);

        static double CalculateRejectionProbability(
            double trafficIntensity, 
            int channelCount, 
            double idleProbability) => 
            Math.Pow(trafficIntensity, channelCount) / 
            Factorial(channelCount) * 
            idleProbability;

        static double CalculateIdleProbability(double trafficIntensity, int channelCount)
        {
            double sum = 0;
            for (int i = 0; i <= channelCount; i++)
            {
                sum += Math.Pow(trafficIntensity, i) / Factorial(i);
            }
            return 1 / sum;
        }

        static double Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);
    }

    struct ServiceChannel
    {
        public Thread? ProcessingThread;
        public bool IsBusy;
    }

    public class ServiceSystem
    {
        private readonly ServiceChannel[] channels;
        private readonly object statsLock = new object();
        private readonly DateTime[] channelStartTimes;
        private readonly DateTime systemStartTime;
        private readonly double serviceRate;

        public int TotalRequestsCount { get; private set; }
        public int ProcessedRequestsCount { get; private set; }
        public int RejectedRequestsCount { get; private set; }
        public double BusyTime { get; private set; }
        public double IdleTime { get; private set; }
        public double TotalOperationTime { get; private set; }

        public int BusyChannelsCount
        {
            get
            {
                int count = 0;
                foreach (var channel in channels)
                {
                    if (channel.IsBusy) count++;
                }
                return count;
            }
        }

        public ServiceSystem(int channelCount, double serviceRate)
        {
            channels = new ServiceChannel[channelCount];
            channelStartTimes = new DateTime[channelCount];
            this.serviceRate = serviceRate;
            systemStartTime = DateTime.Now;
        }

        public void ProcessRequest(object? sender, RequestEventArgs e)
        {
            if (e == null) return;

            lock (statsLock)
            {
                TotalRequestsCount++;
                TotalOperationTime = (DateTime.Now - systemStartTime).TotalSeconds;
                Console.WriteLine($"Request #{e.RequestId} arrived to the system");

                if (BusyChannelsCount == 0)
                {
                    IdleTime += (DateTime.Now - systemStartTime).TotalSeconds - TotalOperationTime;
                }
                HandleRequest(e.RequestId);
            }
        }

        private void HandleRequest(int requestId)
        {
            if (FindAvailableChannel(out int availableChannel))
            {
                StartProcessing(requestId, availableChannel);
            }
            else
            {
                RejectedRequestsCount++;
                Console.WriteLine($"Request #{requestId} was rejected");
            }
        }

        private bool FindAvailableChannel(out int availableChannelIndex)
        {
            for (int i = 0; i < channels.Length; i++)
            {
                if (!channels[i].IsBusy)
                {
                    availableChannelIndex = i;
                    return true;
                }
            }
            availableChannelIndex = -1;
            return false;
        }

        private void StartProcessing(int requestId, int channelIndex)
        {
            channels[channelIndex].IsBusy = true;
            channelStartTimes[channelIndex] = DateTime.Now;
            
            channels[channelIndex].ProcessingThread = new Thread(() => 
            {
                Console.WriteLine($"Started processing request #{requestId}");
                Thread.Sleep((int)(500 / serviceRate));
                
                lock (statsLock)
                {
                    BusyTime += (DateTime.Now - channelStartTimes[channelIndex]).TotalSeconds;
                    channels[channelIndex].IsBusy = false;
                    Console.WriteLine($"Request #{requestId} processed in channel {channelIndex + 1}");
                }
            });
            
            channels[channelIndex].ProcessingThread.Start();
            ProcessedRequestsCount++;
            Console.WriteLine($"Request #{requestId} accepted to channel {channelIndex + 1}");
        }
    }

    public class RequestGenerator
    {
        public event EventHandler<RequestEventArgs>? RequestArrived;

        public RequestGenerator(ServiceSystem system) => 
            RequestArrived += system.ProcessRequest;

        public void SendRequest(int requestId) => 
            RequestArrived?.Invoke(this, new RequestEventArgs { RequestId = requestId });
    }

    public class RequestEventArgs : EventArgs
    {
        public int RequestId { get; set; }
    }
}
