﻿using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using ServiceStack.ServiceHost;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.LiveTv
{
    [Route("/LiveTv/Services", "GET")]
    [Api(Description = "Gets available live tv services.")]
    public class GetServices : IReturn<List<LiveTvServiceInfo>>
    {
        [ApiMember(Name = "ServiceName", Description = "Optional filter by service.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ServiceName { get; set; }
    }

    [Route("/LiveTv/Channels", "GET")]
    [Api(Description = "Gets available live tv channels.")]
    public class GetChannels : IReturn<List<ChannelInfoDto>>
    {
        [ApiMember(Name = "ServiceName", Description = "Optional filter by service.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ServiceName { get; set; }
    }

    [Route("/LiveTv/Recordings", "GET")]
    [Api(Description = "Gets available live tv recordings.")]
    public class GetRecordings : IReturn<List<RecordingInfo>>
    {
        [ApiMember(Name = "ServiceName", Description = "Optional filter by service.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ServiceName { get; set; }
    }

    [Route("/LiveTv/Guide", "GET")]
    [Api(Description = "Gets available live tv epgs..")]
    public class GetGuide : IReturn<List<ChannelGuide>>
    {
        [ApiMember(Name = "ServiceName", Description = "Live tv service name", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ServiceName { get; set; }

        [ApiMember(Name = "ChannelIds", Description = "The channels to return guide information for.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ChannelIds { get; set; }
    }

    public class LiveTvService : BaseApiService
    {
        private readonly ILiveTvManager _liveTvManager;

        public LiveTvService(ILiveTvManager liveTvManager)
        {
            _liveTvManager = liveTvManager;
        }

        private IEnumerable<ILiveTvService> GetServices(string serviceName)
        {
            IEnumerable<ILiveTvService> services = _liveTvManager.Services;

            if (!string.IsNullOrEmpty(serviceName))
            {
                services = services.Where(i => string.Equals(i.Name, serviceName, System.StringComparison.OrdinalIgnoreCase));
            }

            return services;
        }

        public object Get(GetServices request)
        {
            var services = GetServices(request.ServiceName)
                .Select(GetServiceInfo)
                .ToList();

            return ToOptimizedResult(services);
        }

        private LiveTvServiceInfo GetServiceInfo(ILiveTvService service)
        {
            return new LiveTvServiceInfo
            {
                Name = service.Name
            };
        }

        public object Get(GetChannels request)
        {
            var result = GetChannelsAsync(request).Result;

            return ToOptimizedResult(result.ToList());
        }

        private async Task<IEnumerable<ChannelInfoDto>> GetChannelsAsync(GetChannels request)
        {
            var services = GetServices(request.ServiceName);

            var tasks = services.Select(i => i.GetChannelsAsync(CancellationToken.None));

            var channelLists = await Task.WhenAll(tasks).ConfigureAwait(false);

            // Aggregate all channels from all services
            return channelLists.SelectMany(i => i)
                .Select(_liveTvManager.GetChannelInfoDto);
        }

        public object Get(GetRecordings request)
        {
            var result = GetRecordingsAsync(request).Result;

            return ToOptimizedResult(result.ToList());
        }

        private async Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(GetRecordings request)
        {
            var services = GetServices(request.ServiceName);

            var query = new RecordingQuery
            {

            };

            var tasks = services.Select(i => i.GetRecordingsAsync(query, CancellationToken.None));

            var recordings = await Task.WhenAll(tasks).ConfigureAwait(false);

            return recordings.SelectMany(i => i);
        }

        public object Get(GetGuide request)
        {
            var result = GetGuideAsync(request).Result;

            return ToOptimizedResult(result);
        }

        private async Task<IEnumerable<ChannelGuide>> GetGuideAsync(GetGuide request)
        {
            var service = GetServices(request.ServiceName)
                .First();

            var channels = request.ChannelIds.Split(',');

            return await service.GetChannelGuidesAsync(channels, CancellationToken.None).ConfigureAwait(false);
        }
    }
}