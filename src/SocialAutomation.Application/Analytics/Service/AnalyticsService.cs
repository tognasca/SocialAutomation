using SocialAutomation.Application.Analytics.DTOs;
using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Application.Analytics.Services;

public class AnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AnalyticsService(
        IAnalyticsRepository analyticsRepository,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _analyticsRepository = analyticsRepository;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<DashboardResponse>> GetDashboardAsync(
        AnalyticsFilterRequest request, CancellationToken ct = default)
    {
        var (from, to, period) = ResolvePeriod(request);

        var summary = await _analyticsRepository.GetDashboardSummaryAsync(_tenantContext.TenantId, from, to, ct);
        var funnel = await _analyticsRepository.GetFunnelMetricsAsync(_tenantContext.TenantId, ct);
        var series = await _analyticsRepository.GetTimeSeriesAsync(_tenantContext.TenantId, from, to, ct);
        var sources = await _analyticsRepository.GetLeadSourcesAsync(_tenantContext.TenantId, from, to, ct);
        var scores = await _analyticsRepository.GetScoreDistributionAsync(_tenantContext.TenantId, ct);

        var response = new DashboardResponse
        {
            From = from,
            To = to,
            Period = period,
            NewLeads = summary.NewLeads,
            Conversations = summary.Conversations,
            HotLeads = summary.HotLeads,
            Opportunities = summary.Opportunities,
            Clients = summary.Clients,
            FollowUpsPending = summary.FollowUpsPending,
            MessagesReceived = summary.MessagesReceived,
            MessagesSent = summary.MessagesSent,
            AiReplies = summary.AiReplies,
            HumanHandoffs = summary.HumanHandoffs,
            CampaignsSent = summary.CampaignsSent,
            ContentPublished = summary.ContentPublished,
            ConversionRate = summary.ConversionRate,
            TotalPipelineValue = summary.TotalPipelineValue,
            Funnel = funnel.Select(f => new FunnelStageDto
            {
                Stage = f.Stage,
                Count = f.Count,
                TotalValue = f.TotalValue
            }).ToList(),
            TimeSeries = series.Select(s => new TimeSeriesDto
            {
                Date = s.Date.ToString("yyyy-MM-dd"),
                Leads = s.Leads,
                Conversations = s.Conversations,
                Messages = s.Messages,
                Clients = s.Clients
            }).ToList(),
            Sources = sources.Select(s => new SourceDto
            {
                Source = s.Source,
                Count = s.Count
            }).ToList(),
            ScoreDistribution = scores.Select(s => new ScoreDto
            {
                Classification = s.Classification,
                Count = s.Count
            }).ToList()
        };

        return Result.Ok(response);
    }

    private (DateTime from, DateTime to, string period) ResolvePeriod(AnalyticsFilterRequest request)
    {
        var now = _dateTimeProvider.UtcNow;
        var to = now.Date.AddDays(1);
        var period = (request.Period ?? "30d").Trim().ToLowerInvariant();

        if (period == "custom" && request.From.HasValue && request.To.HasValue)
        {
            var fromCustom = request.From.Value.Date;
            var toCustom = request.To.Value.Date.AddDays(1);
            if (toCustom <= fromCustom) toCustom = fromCustom.AddDays(1);
            return (fromCustom, toCustom, "custom");
        }

        var from = period switch
        {
            "today" => now.Date,
            "7d" => now.Date.AddDays(-6),
            "90d" => now.Date.AddDays(-89),
            _ => now.Date.AddDays(-29) // 30d default
        };

        if (period is not ("today" or "7d" or "30d" or "90d"))
            period = "30d";

        return (from, to, period);
    }
}