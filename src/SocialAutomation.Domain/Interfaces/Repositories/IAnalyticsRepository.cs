using SocialAutomation.Domain.Models.Analytics;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IAnalyticsRepository
{
    Task<DashboardSummary> GetDashboardSummaryAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IEnumerable<FunnelStageMetric>> GetFunnelMetricsAsync(Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<TimeSeriesPoint>> GetTimeSeriesAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IEnumerable<SourceMetric>> GetLeadSourcesAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IEnumerable<ScoreDistributionMetric>> GetScoreDistributionAsync(Guid tenantId, CancellationToken ct = default);
}