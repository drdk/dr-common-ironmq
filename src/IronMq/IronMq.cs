using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DR.Common.IronMq.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace DR.Common.IronMq
{
    public abstract class IronMq<TSystem> : IQueue<TSystem>
    {
        private readonly string _queueName;
        private readonly string _deadLetterQueueName;
        private readonly string _projectId;
        private readonly string _token;
        private readonly string _hostName;

        public TimeSpan LongPollWait { get; set; } = TimeSpan.FromSeconds(30);

        private int? LongPollWaitSecAsInt => (int) LongPollWait.TotalSeconds;
        // This will store an instance of the client to prevent creating a new HttpClient on every call to IronMq
        // In the DefaultRegistry class, the IQueue instances will be configured as singletons so each queue
        // will only be created once
        private readonly Client _client;

        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        protected IronMq(
            string queueName,
            string projectId,
            string token,
            string hostName,
            HttpClient httpClient)
        {
            if (string.IsNullOrWhiteSpace(queueName)) throw new ArgumentNullException(nameof(queueName));
            if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentNullException(nameof(projectId));
            if (string.IsNullOrWhiteSpace(token)) throw new ArgumentNullException(nameof(token));
            if (string.IsNullOrWhiteSpace(hostName)) throw new ArgumentNullException(nameof(hostName));


            _queueName = queueName;
            _deadLetterQueueName = $"{_queueName}-DEADLETTER";
            _projectId = projectId;
            _token = token;
            _hostName = hostName;


            //Since we are using a static queues, we need to handle failover scenarios at IronMq by closing connections once in a while. ServicePointManager.DnsRefreshTimeout is 2 minutes by default.
            var ironMqServicePoint = System.Net.ServicePointManager.FindServicePoint(new Uri($"https://{_hostName}"));
            ironMqServicePoint.ConnectionLeaseTimeout = (int) TimeSpan.FromMinutes(10).TotalMilliseconds;


            var baseUrl = $"https://{_hostName}/3/projects/{_projectId}/";
            _client = new Client(baseUrl, httpClient);

        }

        public abstract TSystem QueueOwner { get; }
        public int DeadletterCount => DeadletterQueueInfo().Result?.Size.GetValueOrDefault() ?? 0;
        public int MessageCount => QueueInfo().Result?.Size ?? 0;

        #region Peak
        public async Task<IEnumerable<RawQueueMessage>> Peek(int maxCount)
        {

            return await Peek(maxCount, _queueName);
        }

        public async Task<IEnumerable<RawQueueMessage>> PeekDeadletter(int maxCount)
        {
            return await Peek(maxCount, _deadLetterQueueName);
        }

        private async Task<IEnumerable<RawQueueMessage>> Peek(int maxCount, string queueName)
        {
            if (maxCount > 100) 
                throw new ArgumentOutOfRangeException(nameof(maxCount), "Maximum number of 100 messages can be peeked");

            if (queueName == null) 
                throw new ArgumentNullException(nameof(queueName));

            await SemaphoreSlim.WaitAsync();

            try
            {
                var collection =  await _client.Messages2Async(queueName, _token, new PeekMessagesRequest() { N = maxCount });
                
                if (collection == null)
                    throw new InvalidOperationException($"Call to {queueName}.Peek(maxCount: {maxCount}) returned null");

                return collection.Messages.Select(MapToRawQueueMessage);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
        #endregion

        #region Retrieve

        private async Task<IEnumerable<RawQueueMessage>> RetrieveMessages(
            int timeout,
            int maxCount,
            string queueName,
            bool longPoll = false)
        {
            if (timeout < 0)
                throw new ArgumentOutOfRangeException(nameof(timeout), "A negative timeout is not supported.");

            if (timeout > 86400)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout can at max be 24 hours (86400 seconds).");

            if (queueName == null) 
                throw new ArgumentNullException(nameof(queueName));

            if (maxCount > 100)
                throw new ArgumentOutOfRangeException(
                    nameof(maxCount),
                    "Maximum number of 100 messages can be retrieved");

            var longPollWait = longPoll ? LongPollWaitSecAsInt : null;
            
            Response4 collection = null;

            await SemaphoreSlim.WaitAsync();

            try
            {
                collection = await _client.ReservationsAsync(
                    queueName, 
                    _token,
                    new ReservationRequest() { N = maxCount, Timeout = timeout, Wait = longPollWait });

            }
            catch (AggregateException e)
            {
                if (e.InnerException is ApiException exception && exception.StatusCode == 404)
                {
                    //No queue is an empty queue
                    collection = new Response4
                    {
                        Messages = new System.Collections.ObjectModel.ObservableCollection<Message>()
                    };
                }
            }
            finally
            {
                SemaphoreSlim.Release();
            }

            if (collection == null)
                throw new InvalidOperationException($"Call to {_queueName}.Reserve(timeout: {timeout}) returned null");

            return collection.Messages.Select(MapToRawQueueMessage);
        }

        public async Task<IEnumerable<RawQueueMessage>> RetrieveMessages(int timeout, int maxCount, bool longPoll = false)
        {
            return await RetrieveMessages(timeout, maxCount, _queueName, longPoll);
        }


        public async Task<IEnumerable<RawQueueMessage>> RetrieveDeadletterMessages(int timeout, int maxCount, bool longPoll = false)
        {
            return await RetrieveMessages(timeout, maxCount, _deadLetterQueueName, longPoll);
        }

        public async Task<RawQueueMessage> RetrieveMessage(int timeout, bool longPoll = false)
        {
            var res = await RetrieveMessages(timeout, 1, _queueName, longPoll);
            return res.FirstOrDefault();
        }

        public async Task<RawQueueMessage> RetrieveDeadletterMessage(int timeout, bool longPoll = false)
        {
            var res = await RetrieveMessages(timeout, 1, _deadLetterQueueName, longPoll);
            return res.FirstOrDefault();
        }
        #endregion

        #region Delete
        public async Task DeleteMessage(RawQueueMessage rawQueueMessage)
        {
            if (rawQueueMessage == null) 
                throw new ArgumentNullException(nameof(rawQueueMessage));

            await SemaphoreSlim.WaitAsync();

            try
            {
                await _client.Messages4Async(
                    _queueName, 
                    rawQueueMessage.Id, 
                    _token,
                    new DeleteMessageRequest
                    {
                        Reservation_id = rawQueueMessage.ReservationId
                    });
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
        #endregion

        #region Release
        public async Task ReleaseMessage(RawQueueMessage rawQueueMessage)
        {
            if (rawQueueMessage == null) 
                throw new ArgumentNullException(nameof(rawQueueMessage));

            await SemaphoreSlim.WaitAsync();

            try
            {
                await _client.ReleaseAsync(
                    _queueName, 
                    rawQueueMessage.Id, 
                    _token,
                    new ReleaseMessageRequest
                    {
                        Reservation_id = rawQueueMessage.ReservationId
                    });
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
        #endregion

        #region Post
        public async Task PostMessage(object data)
        {
            await PostMessage(data, _queueName);
        }

        private async Task PostMessage(object data, string queueName)
        {
            if (data == null) 
                throw new ArgumentNullException(nameof(data));

            if (queueName == null) 
                throw new ArgumentNullException(nameof(queueName));

            await SemaphoreSlim.WaitAsync();

            try
            {
                var message = (data is string dataStr) ? dataStr : Serialize(data);

                await _client.MessagesAsync(
                    queueName, 
                    _token,
                    new EnqueueMessages()
                    {
                        Messages = new System.Collections.ObjectModel.ObservableCollection<Messages>
                        {
                            new Messages() {Body = message}
                        }
                    });

            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
        #endregion

        #region Move
        public async Task MoveToDeadletterQueue(RawQueueMessage rawQueueMessage)
        {
            if (rawQueueMessage == null) 
                throw new ArgumentNullException(nameof(rawQueueMessage));

            await SemaphoreSlim.WaitAsync();

            try
            {
                await PostMessage(rawQueueMessage.Body, _deadLetterQueueName);

                await _client.Messages4Async(
                    _queueName, 
                    rawQueueMessage.Id, 
                    _token,
                    new DeleteMessageRequest
                    {
                        Reservation_id = rawQueueMessage.ReservationId
                    });
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public async Task MoveToQueue(RawQueueMessage rawQueueMessage)
        {
            if (rawQueueMessage == null) 
                throw new ArgumentNullException(nameof(rawQueueMessage));

            await SemaphoreSlim.WaitAsync();

            try
            {
                await PostMessage(rawQueueMessage.Body, _queueName);
                
                await _client.Messages4Async(
                    _deadLetterQueueName, 
                    rawQueueMessage.Id, 
                    _token,
                    new DeleteMessageRequest
                    {
                        Reservation_id = rawQueueMessage.ReservationId
                    });
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
        #endregion

        #region Queue Info
        public async Task<IronQueueInfo> QueueInfo()
        {
            await SemaphoreSlim.WaitAsync();

            try
            {
                var q = await _client.QueuesAsync(_queueName, _token);
                return MapToIronQueueInfo(q);
            }
            catch (AggregateException e)
            {
                if (e.InnerException is ApiException exception && exception.StatusCode == 404)
                {
                    return new IronQueueInfo() { Name = _queueName };
                }

                throw;
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public async Task<IronQueueInfo> DeadletterQueueInfo()
        {
            await SemaphoreSlim.WaitAsync();

            try
            {
                var q = await _client.QueuesAsync(_deadLetterQueueName, _token);
                return MapToIronQueueInfo(q);
            }
            catch (AggregateException e)
            {
                if (e.InnerException is ApiException exception && exception.StatusCode == 404)
                {
                    return new IronQueueInfo() { Name = _deadLetterQueueName };
                }

                throw;
            }
            finally
            {
                SemaphoreSlim.Release();
            }

        }
        #endregion

        #region Mappers
        private static RawQueueMessage MapToRawQueueMessage(Message message)
        {
            return new RawQueueMessage()
            {
                Id = message.Id,
                ReservationId = message.Reservation_id,
                Body = message.Body,
                ReservedCount = message.Reserved_count.Value
            };
        }

        private static IronQueueInfo MapToIronQueueInfo(Response7 response)
        {
            if (response == null)
                return null;

            return new IronQueueInfo()
            {
                Name = response.Queue.Name,
                Size = response.Queue.Size,
                //Alerts = x.Alerts,
                //QueueDeadletterInfo = x.QueueDeadletterInfo,
                //Id = x.Queue.Name,
                ProjectId = response.Queue.Project_id,
                MessageExpiration = response.Queue.Message_expiration,
                MessageTimeout = response.Queue.Message_timeout,
                //PushInfo = x.PushInfo,
                //PushType = x.PushType,
                TotalMessages = response.Queue.Total_messages
            };
        }
        #endregion

        private static string Serialize(object o)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));

            return JsonConvert.SerializeObject(o, new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new IsoDateTimeConverter(),
                    new StringEnumConverter()
                },
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
        }
    }
}